
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
        if (!TryGetEntity(ev.EntityUid, out var entity) || !TryComp<SpriteComponent>(entity.Value, out var sprite))
            return;

        sprite.LayerSetState(0, new Robust.Client.Graphics.RSI.State(0, new Robust.Client.Graphics.RSI.StateId(ev.FactionID));
    }
}
