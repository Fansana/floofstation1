using Content.Server.Chat.Systems;
using Content.Shared._Goobstation.DelayedDeath;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Medical;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
namespace Content.Server._Shitmed.DelayedDeath;

public partial class DelayedDeathSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!; // Goobstation
    [Dependency] private readonly ChatSystem _chat = default!; // Goobstation
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DelayedDeathComponent, TargetBeforeDefibrillatorZapsEvent>(OnDefibZap);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        using var query = EntityQueryEnumerator<DelayedDeathComponent>();
        while (query.MoveNext(out var ent, out var component))
        {
            component.DeathTimer += frameTime;

            if (component.DeathTimer >= component.DeathTime && !_mobState.IsDead(ent))
            {
                var damage = new DamageSpecifier(_prototypes.Index<DamageTypePrototype>("Bloodloss"), 150);
                _damageable.TryChangeDamage(ent, damage, partMultiplier: 0f);

                var ev = new DelayedDeathEvent(ent);
                RaiseLocalEvent(ent, ref ev);

                if (ev.Cancelled)
                {
                    RemComp<DelayedDeathComponent>(ent);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(component.DeathMessageId)) // Goobstation
                    _popupSystem.PopupEntity(Loc.GetString(component.DeathMessageId), ent, PopupType.LargeCaution);
            }
        }
    }

    private void OnDefibZap(Entity<DelayedDeathComponent> ent, ref TargetBeforeDefibrillatorZapsEvent args)
    {
        // can't defib someone without a heart or brain pal
        args.Cancel();

        _chat.TrySendInGameICMessage(args.Defib, Loc.GetString("defibrillator-missing-organs"),
            InGameICChatType.Speak, true);
    }
}
