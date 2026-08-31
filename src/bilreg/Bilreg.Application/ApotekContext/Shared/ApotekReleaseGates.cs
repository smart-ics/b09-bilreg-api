namespace Bilreg.Application.ApotekContext.Shared;

/// <summary>
/// Production release gates for Apotek endpoints. Code-review GO does not clear these.
/// </summary>
public static class ApotekReleaseGates
{
    /// <summary>Role-command matrix not ratified; authenticated-actor policy seam only.</summary>
    public const string Bc12CommandRoleMatrix = "BC-12";
}
