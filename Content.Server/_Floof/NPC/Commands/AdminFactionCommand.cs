using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Server.NPC.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using Content.Server.NPC.Components;
using Content.Shared.NPC.Systems;


namespace Content.Server._Floof.NPC.Commands;

[ToolshedCommand(Name = "faction"), AdminCommand(AdminFlags.Admin)]
public sealed class AdminFactionCommand : ToolshedCommand
{
    private NpcFactionSystem? _factionField;
    private NpcFactionSystem Factions => _factionField ??= GetSys<NpcFactionSystem>();

    [CommandImplementation("add")]
    public EntityUid AddFaction(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid input,
        [CommandArgument] string faction
    )
    {
        Factions.AddFaction(input, faction);

        return input;
    }

    [CommandImplementation("rm")]
    public EntityUid RmFaction(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid input,
        [CommandArgument] string faction
    )
    {
        Factions.RemoveFaction(input, faction);

        return input;
    }

    [CommandImplementation("clear")]
    public EntityUid ClearFaction(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid input
    )
    {
        Factions.ClearFactions(input);

        return input;
    }

}
