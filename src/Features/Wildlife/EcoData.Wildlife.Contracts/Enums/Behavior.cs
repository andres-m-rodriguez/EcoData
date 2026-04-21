namespace EcoData.Wildlife.Contracts.Enums;

[Flags]
public enum Behavior
{
    None = 0,
    Feeding = 1,
    Resting = 2,
    Moving = 4,
    Calling = 8,
}
