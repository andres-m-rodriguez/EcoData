namespace EcoData.Wildlife.Contracts;

/// <summary>
/// Origin of a species relative to Puerto Rico. Replaces the former
/// <c>IsEndemic</c> boolean, which could not express "not yet assessed" —
/// absent data was indistinguishable from a confident "not endemic".
/// </summary>
public enum EndemicStatus
{
    /// <summary>
    /// No endemism assessment recorded. The default for un-sourced rows.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Native and restricted to Puerto Rico — occurs naturally nowhere else.
    /// </summary>
    Endemic = 1,

    /// <summary>
    /// Occurs naturally in Puerto Rico, but also elsewhere.
    /// </summary>
    Native = 2,

    /// <summary>
    /// Present in Puerto Rico but not naturally occurring.
    /// </summary>
    Introduced = 3,
}
