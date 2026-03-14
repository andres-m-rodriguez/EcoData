using System.Diagnostics;
using EcoData.Sensors.Application.Client;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Requests;

namespace EcoData.IntegrationTests;

/// <summary>
/// Runs the actual C++ ecodata executable for integration tests.
/// </summary>
public sealed class CppEsp32Device : IEsp32Device
{
    private readonly string _exePath;
    private readonly string _apiUrl;
    private readonly ISensorHttpClient _sensors;

    private Guid? _sensorId;
    private string? _token;

    public CppEsp32Device(string exePath, string apiUrl, HttpClient httpClient)
    {
        _exePath = exePath;
        _apiUrl = apiUrl;
        _sensors = new SensorHttpClient(httpClient);
    }

    public Guid SensorId =>
        _sensorId ?? throw new InvalidOperationException("Sensor not registered.");

    public bool IsRegistered => _sensorId.HasValue;
    public bool IsAuthenticated => _token is not null;

    public async Task<SensorRegistrationResultDto?> RegisterAsync(
        RegisterSensorRequest request,
        CancellationToken ct = default)
    {
        var result = await _sensors.RegisterAsync(request, ct);

        if (result.IsT0)
        {
            var credentials = result.AsT0;
            _sensorId = credentials.SensorId;
            _token = credentials.AccessToken;
            return credentials;
        }

        return null;
    }

    public async Task SendSensorDataAsync(SensorReadingDto reading, CancellationToken ct = default)
    {
        if (_sensorId is null || _token is null)
            throw new InvalidOperationException("Sensor must be registered.");

        var args = BuildArgs(reading);
        var result = await RunExeAsync(args, ct);

        if (result.ExitCode != 0)
            throw new InvalidOperationException($"C++ exe failed: {result.StdErr}");

        if (!result.StdOut.Contains("OK:PUSH"))
            throw new InvalidOperationException($"Push failed: {result.StdOut}");
    }

    private string BuildArgs(SensorReadingDto reading)
    {
        var args = $"--url \"{_apiUrl}\" --sensor-id \"{_sensorId}\" --token \"{_token}\"";

        if (reading.Temperature.HasValue)
            args += $" -r \"temperature,{reading.Temperature.Value},C\"";
        if (reading.Ph.HasValue)
            args += $" -r \"ph,{reading.Ph.Value},pH\"";
        if (reading.DissolvedOxygen.HasValue)
            args += $" -r \"dissolvedOxygen,{reading.DissolvedOxygen.Value},mg/L\"";
        if (reading.Turbidity.HasValue)
            args += $" -r \"turbidity,{reading.Turbidity.Value},NTU\"";
        if (reading.Conductivity.HasValue)
            args += $" -r \"conductivity,{reading.Conductivity.Value},uS/cm\"";

        return args;
    }

    private async Task<(int ExitCode, string StdOut, string StdErr)> RunExeAsync(
        string args, CancellationToken ct)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _exePath,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        var stdout = await process.StandardOutput.ReadToEndAsync(ct);
        var stderr = await process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        return (process.ExitCode, stdout, stderr);
    }
}
