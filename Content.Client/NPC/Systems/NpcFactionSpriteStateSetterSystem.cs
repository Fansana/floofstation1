
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Events;
using Robust.Client.GameObjects;

namespace Content.Client.NPC.Systems;
public sealed partial class NpcFactionSpriteStateSetterSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<NpcFactionAddedEvent>(OnFactionAdded);
    }

    private void OnFactionAdded(NpcFactionAddedEvent ev)
    {
        if (_entityManager.TryGetEntity(ev.EntityUid, out var entity))
        {
            if (!_entityManager.HasComponent(entity.Value, typeof(NpcFactionSpriteStateSetterComponent)))
                return;

            SpriteComponent spriteComponent = _entityManager.GetComponent<SpriteComponent>(entity.Value);
            spriteComponent.LayerSetState(0, new Robust.Client.Graphics.RSI.StateId(ev.FactionID));
        }
    }
}
