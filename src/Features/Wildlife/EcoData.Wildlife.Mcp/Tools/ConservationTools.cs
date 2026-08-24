using System.ComponentModel;
using EcoData.Wildlife.DataAccess.Interfaces;
using ModelContextProtocol.Server;

namespace EcoData.Wildlife.Mcp.Tools;

/// <summary>
/// The conservation half of the connector: the NRCS practices landowners can
/// carry out, and the Fish &amp; Wildlife Service recovery actions those
/// practices deliver.
/// </summary>
// Sealed rather than static, for the same reason as SpeciesTools.
[McpServerToolType]
public sealed class ConservationTools
{
    [McpServerTool(Name = "list_conservation_practices")]
    [Description("""
        List the NRCS conservation practices recorded for the Caribbean area, by code
        and name. These are the on-the-ground practices a landowner can carry
        out; list_recovery_actions covers the outcomes they serve.
        """)]
    public static async Task<IReadOnlyList<ConservationPractice>> ListConservationPractices(
        INrcsPracticeRepository repository,
        CancellationToken cancellationToken
    )
    {
        var practices = await repository.GetAsync(cancellationToken);

        return practices
            .Select(practice => new ConservationPractice(
                practice.Code,
                WildlifeMcpMapping.ResolveName(practice.Name, practice.Code)
            ))
            .ToList();
    }

    [McpServerTool(Name = "list_recovery_actions")]
    [Description("""
        List the Fish & Wildlife Service recovery actions recorded for Puerto
        Rico, by code and name. These are the conservation outcomes that NRCS
        practices are meant to deliver.
        """)]
    public static async Task<IReadOnlyList<RecoveryAction>> ListRecoveryActions(
        IFwsActionRepository repository,
        CancellationToken cancellationToken
    )
    {
        var actions = await repository.GetAsync(cancellationToken);

        return actions
            .Select(action => new RecoveryAction(
                action.Code,
                WildlifeMcpMapping.ResolveName(action.Name, action.Code)
            ))
            .ToList();
    }
}
