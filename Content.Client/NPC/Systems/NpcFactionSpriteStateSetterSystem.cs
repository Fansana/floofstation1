
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Events;
using Robust.Client.GameObjects;

namespace Content.Client.NPC.Systems;
public sealed partial class NpcFactionSpriteStateSetterSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<NpcFactionAddedEvent>(OnFactionAdded);
    }

    private void OnFactionAdded(NpcFactionAddedEvent ev)
    {
        if (!TryGetEntity(ev.EntityUid, out var entity) || !TryComp<SpriteComponent>(entity.Value, out var sprite) || !TryComp<NpcFactionSpriteStateSetterComponent>(entity.Value, out var _)|| !TryComp<NpcFactionSelectorComponent>(entity.Value, out var factionSelector))
            return;

        if(factionSelector.SelectableFactions.Contains(ev.FactionID))
            sprite.LayerSetState(0, new (ev.FactionID));
    }
}
