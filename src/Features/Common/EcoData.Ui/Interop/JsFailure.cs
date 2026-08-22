namespace EcoData.Ui.Interop;

public enum JsFailureKind
{
    ScriptError,
    Disconnected,
    Unavailable,
    Cancelled,
}

public sealed record JsFailure(JsFailureKind Kind, string Message);
