using System.Linq;
using Content.Shared.Clothing.Components;
using Content.Shared.DoAfter;
using Content.Shared.Floofstation.Leash.Components;
using Content.Shared.Floofstation.Leash.Events;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Floofstation.Leash;

public sealed class LeashSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfters = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedJointSystem _joints = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;

    public override void Initialize()
    {
        UpdatesBefore.Add(typeof(SharedPhysicsSystem));

        SubscribeLocalEvent<LeashAnchorComponent, BeingUnequippedAttemptEvent>(OnAnchorUnequipping);
        SubscribeLocalEvent<LeashComponent, JointRemovedEvent>(OnJointRemoved);
        SubscribeLocalEvent<LeashAnchorComponent, GetVerbsEvent<EquipmentVerb>>(OnGetEquipmentVerbs);
        SubscribeLocalEvent<LeashedComponent, GetVerbsEvent<InteractionVerb>>(OnGetLeashedVerbs);

        SubscribeLocalEvent<LeashAnchorComponent, LeashAttachDoAfterEvent>(OnAttachDoAfter);
        SubscribeLocalEvent<LeashedComponent, LeashDetachDoAfterEvent>(OnDetachDoAfter);
    }

    public override void Update(float frameTime)
    {
        var leashQuery = EntityQueryEnumerator<LeashComponent>();

        while (leashQuery.MoveNext(out var leashEnt, out var leash))
        {
            var sourceXForm = Transform(leashEnt);

            foreach (var data in leash.Leashed)
            {
                // Break each leash joint whose entities are on different maps or are too far apart
                var target = GetEntity(data.Pulled);
                var targetXForm = Transform(target);

                if (targetXForm.MapUid != sourceXForm.MapUid
                    || !sourceXForm.Coordinates.TryDistance(EntityManager, targetXForm.Coordinates, out var dst)
                    || dst > leash.MaxDistance)
                    RemoveLeash(target, (leashEnt, leash));

                // Calculate joint damage
                if (_timing.CurTime < data.NextDamage
                    || !TryComp<JointComponent>(target, out var jointComp)
                    || !jointComp.GetJoints.TryGetValue(data.JointId, out var joint))
                    continue;

                // TODO reaction force always returns 0 and thus damage doesn't work
                // TODO find another way to calculate how much force is being excerted to hold the two entities together
                var damage = joint.GetReactionForce(1 / (float) leash.DamageInterval.TotalSeconds).Length() - leash.JointRepairDamage;
                data.Damage = Math.Max(0f, data.Damage + damage);
                data.NextDamage = _timing.CurTime + leash.DamageInterval;

                if (damage >= leash.BreakDamage && !_net.IsClient)
                {
                    _popups.PopupPredicted(Loc.GetString("leash-snap-popup", ("leash", leashEnt)), target, null, PopupType.SmallCaution);
                    RemoveLeash(target, (leashEnt, leash), true);
                }
            }
        }

        leashQuery.Dispose();
    }

    #region event handling

    private void OnAnchorUnequipping(Entity<LeashAnchorComponent> ent, ref BeingUnequippedAttemptEvent args)
    {
        // Prevent unequipping the anchor clothing until the leash is removed
        if (TryComp<LeashedComponent>(args.Equipment, out var leashed) && leashed.Puller is not null)
            args.Cancel();
    }

    private void OnJointRemoved(Entity<LeashComponent> ent, ref JointRemovedEvent args)
    {
        var id = args.Joint.ID;
        if (!ent.Comp.Leashed.TryFirstOrDefault(it => it.JointId == id, out var data)
            || !TryComp<LeashedComponent>(GetEntity(data.Pulled), out var leashed))
            return;

        RemoveLeash((leashed.Owner, leashed), ent!, false);
    }

    private void OnGetEquipmentVerbs(Entity<LeashAnchorComponent> ent, ref GetVerbsEvent<EquipmentVerb> args)
    {
        if (!args.CanAccess
            || !args.CanInteract
            || args.Using is not { } leash
            || !TryComp<LeashComponent>(leash, out var leashComp))
            return;

        var user = args.User;
        var leashVerb = new EquipmentVerb { Text = Loc.GetString("verb-leash-text") };

        if (CanLeash(ent, (leash, leashComp)))
            leashVerb.Act = () => TryLeash(ent, (leash, leashComp), user);
        else
        {
            leashVerb.Message = Loc.GetString("verb-leash-error-message");
            leashVerb.Disabled = true;
        }
        args.Verbs.Add(leashVerb);


        if (!TryGetLeashTarget(ent, out var leashTarget)
            || !TryComp<LeashedComponent>(leashTarget, out var leashedComp)
            || leashedComp.Puller != leash
            || HasComp<LeashedComponent>(leashTarget)) // This one means that OnGetLeashedVerbs will add a verb to remove it
            return;

        var unleashVerb = new EquipmentVerb
        {
            Text = Loc.GetString("verb-unleash-text"),
            Act = () => TryUnleash((leashTarget, leashedComp), (leash, leashComp), user)
        };
        args.Verbs.Add(unleashVerb);
    }

    private void OnGetLeashedVerbs(Entity<LeashedComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess
            || !args.CanInteract
            || ent.Comp.Puller is not { } leash
            || !TryComp<LeashComponent>(leash, out var leashComp))
            return;

        var user = args.User;
        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("verb-unleash-text"),
            Act = () => TryUnleash(ent!, (leash, leashComp), user)
        });
    }

    private void OnAttachDoAfter(Entity<LeashAnchorComponent> ent, ref LeashAttachDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled
            || !TryComp<LeashComponent>(args.Used, out var leash)
            || !CanLeash(ent, (args.Used.Value, leash)))
            return;

        DoLeash(ent, (args.Used.Value, leash), args.Target!.Value);
    }

    private void OnDetachDoAfter(Entity<LeashedComponent> ent, ref LeashDetachDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        RemoveLeash(args.Target!.Value, args.Used!.Value, true);
    }

    #endregion

    #region private api

    /// <summary>
    ///     Tries to find the entity that gets leashed for the given anchor entity.
    /// </summary>
    private bool TryGetLeashTarget(Entity<LeashAnchorComponent> ent, out EntityUid leashTarget)
    {
        leashTarget = default;
        if (TryComp<ClothingComponent>(ent, out var clothing))
        {
            if (clothing.InSlot == null || !_container.TryGetContainingContainer(ent, out var container))
                return false;

            leashTarget = container.Owner;
            return true;
        }

        leashTarget = ent.Owner;
        return true;
    }

    #endregion

    #region public api

    public bool CanLeash(Entity<LeashAnchorComponent> anchor, Entity<LeashComponent> leash)
    {
        return leash.Comp.Leashed.Count < leash.Comp.MaxJoints
            && TryGetLeashTarget(anchor, out var leashTarget)
            && CompOrNull<LeashedComponent>(leashTarget)?.JointId == null
            && Transform(anchor).Coordinates.TryDistance(EntityManager, Transform(leash).Coordinates, out var dst)
            && dst <= leash.Comp.Length;
    }

    public bool TryLeash(Entity<LeashAnchorComponent> anchor, Entity<LeashComponent> leash, EntityUid user)
    {
        if (!CanLeash(anchor, leash) || !TryGetLeashTarget(anchor, out var leashTarget))
            return false;

        // We reuse pulling attempt here because eugh it already exists
        var attempt = new PullAttemptEvent(leash, anchor);
        RaiseLocalEvent(anchor, attempt);
        RaiseLocalEvent(leash, attempt);

        if (attempt.Cancelled)
            return false;

        var doAfter = new DoAfterArgs(EntityManager, user, leash.Comp.AttachDelay, new LeashAttachDoAfterEvent(), anchor, leashTarget, leash)
        {
            BreakOnDamage = true,
            BreakOnUserMove = true,
            BreakOnTargetMove = true,
            BreakOnWeightlessMove = true,
            NeedHand = true
        };
        return _doAfters.TryStartDoAfter(doAfter);
    }

    public bool TryUnleash(Entity<LeashedComponent?> leashed, Entity<LeashComponent?> leash, EntityUid user)
    {
        if (!Resolve(leashed, ref leashed.Comp, false) || !Resolve(leash, ref leash.Comp) || leashed.Comp.Puller != leash)
            return false;

        var delay = user == leashed.Owner ? leash.Comp.SelfDetachDelay : leash.Comp.DetachDelay;
        var doAfter = new DoAfterArgs(EntityManager, user, delay, new LeashDetachDoAfterEvent(), leashed.Owner, leashed)
        {
            BreakOnDamage = true,
            BreakOnUserMove = true,
            BreakOnTargetMove = true,
            BreakOnWeightlessMove = true,
            NeedHand = true
        };

        return _doAfters.TryStartDoAfter(doAfter);
    }

    public void DoLeash(Entity<LeashAnchorComponent> anchor, Entity<LeashComponent> leash, EntityUid leashTarget)
    {
        if (_net.IsClient || leashTarget is { Valid: false } && !TryGetLeashTarget(anchor, out leashTarget))
            return;

        var leashedComp = EnsureComp<LeashedComponent>(leashTarget);
        var netLeashTarget = GetNetEntity(leashTarget);
        leashedComp.JointId = $"leash-joint-{netLeashTarget}";
        leashedComp.Puller = leash;

        // I'd like to use a chain joint or smth, but it's too hard and oftentimes buggy - lamia is a good bad example of that.
        var joint = _joints.CreateDistanceJoint(leash, leashTarget, id: leashedComp.JointId);
        joint.CollideConnected = false;
        joint.Length = leash.Comp.Length;
        joint.MinLength = 0f;
        joint.MaxLength = leash.Comp.Length;
        joint.Stiffness = 0f;
        joint.Damping = 0f;

        var data = new LeashComponent.LeashData(leashedComp.JointId, netLeashTarget)
        {
            NextDamage = _timing.CurTime + leash.Comp.DamageInterval
        };

        if (leash.Comp.LeashSprite is { } sprite)
        {
            var visualEntity = EntityManager.SpawnAttachedTo(null, Transform(leashTarget).Coordinates);
            var visualComp = EnsureComp<JointVisualsComponent>(visualEntity);

            visualComp.Sprite = sprite;
            visualComp.Target = leash;

            data.LeashVisuals = GetNetEntity(visualEntity);
        }

        leash.Comp.Leashed.Add(data);
        Dirty(leash);
        Dirty(leashTarget, leashedComp);
    }

    public void RemoveLeash(Entity<LeashedComponent?> leashed, Entity<LeashComponent?> leash, bool breakJoint = true)
    {
        if (_net.IsClient || !Resolve(leashed, ref leashed.Comp))
            return;

        var jointId = leashed.Comp.JointId;
        RemComp<LeashedComponent>(leashed);

        if (breakJoint && jointId is not null)
            _joints.RemoveJoint(leash, jointId);

        if (Resolve(leash, ref leash.Comp, false))
            foreach (var data in leash.Comp.Leashed.Where(it => it.JointId == jointId).ToList())
            {
                if (data.LeashVisuals is {} visualsEntity)
                    QueueDel(GetEntity(visualsEntity));

                leash.Comp.Leashed.Remove(data);
            }

        Dirty(leash);
    }

    #endregion
}
