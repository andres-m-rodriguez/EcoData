namespace EcoData.AquaTrack.WebApp.Client.Services;

public sealed class BackButtonService
{
    public string? Path { get; private set; }
    public bool IsVisible => Path is not null;

    public event Action? OnStateChanged;

    public void SetPath(string path)
    {
        Path = path;
        OnStateChanged?.Invoke();
    }

    public void ResetPath()
    {
        Path = null;
        OnStateChanged?.Invoke();
    }
}
