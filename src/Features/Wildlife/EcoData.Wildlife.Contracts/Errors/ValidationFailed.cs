namespace EcoData.Wildlife.Contracts.Errors;

public sealed record ValidationFailed(IReadOnlyDictionary<string, string[]> Errors)
{
    public string[] AllMessages => Errors.Values.SelectMany(messages => messages).ToArray();
}
