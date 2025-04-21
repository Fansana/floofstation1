using System.Linq;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FloofStation.Lock.Events;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;
using static Content.Shared.FloofStation.Lock.IdLockComponent.LockState;


namespace Content.Shared.FloofStation.Lock;


public sealed class IdLockSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly LockSystem _locks = default!;
    [Dependency] private readonly SharedIdCardSystem _idCards = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<IdLockComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<IdLockComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<IdLockComponent, LockToggleAttemptEvent>(OnToggleNormalLock);
        SubscribeLocalEvent<IdLockComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<LockComponent, IdLockSetEvent>(OnSetIdLock);

        SubscribeLocalEvent<IdLockComponent, IdLockActivateDoAfterEvent>(OnLockDoAfter);
        SubscribeLocalEvent<IdLockComponent, IdLockDeactivateDoAfterEvent>(OnUnlockDoAfter);
    }

    #region Event Handling

    private void OnMapInit(Entity<IdLockComponent> ent, ref MapInitEvent args)
    {
        // Sanity check: as of now, ID locks require a normal lock underneath to work
        if (!HasComp<LockComponent>(ent) || !HasComp<AccessReaderComponent>(ent))
            Log.Warning($"Entity {ToPrettyString(ent)} has an IdLock, but no Lock + AccessReader. As of right now, standalone ID locks are not supported."););
    }

    private void OnExamined(Entity<IdLockComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !ent.Comp.Enabled)
            return;

        using (args.PushGroup(nameof(IdLockComponent)))
        {
            var loc =
                ent.Comp.State == Disengaged ? "id-lock-examined-unlocked" :
                ent.Comp.RevealInfo ? "id-lock-examined-locked-revealing" :
                "id-lock-examined-locked";

            args.PushMarkup(Loc.GetString(loc,
                ("ownerName", ent.Comp.Info.OwnerName ?? "unknown"),
                ("ownerJob", ent.Comp.Info.OwnerJobTitle ?? "unknown")));

            args.PushMarkup(Loc.GetString("id-lock-examined-info"));
        }
    }

    private void OnToggleNormalLock(Entity<IdLockComponent> ent, ref LockToggleAttemptEvent args)
    {
        // We allow to toggle the normal lock on even if the ID lock is enabled, as a failsafe.
        // Ideally, if an ID lock is active, the underlying regular lock should always be active as well.
        if (args.Cancelled || !ent.Comp.Enabled || TryComp<LockComponent>(ent, out var normalLock) && !normalLock.Locked)
            return;

        // Allow toggling if it's disengaged (temporarily or not)
        if (ent.Comp.State != Engaged)
            return;

        args.Cancelled = true;
        if (!args.Silent)
            _popups.PopupClient(Loc.GetString("id-lock-fail-locked", ("ent", ent.Owner)), ent, args.User);
    }

    private void OnGetVerbs(Entity<IdLockComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!ent.Comp.Enabled || !_idCards.TryFindIdCard(args.User, out var id))
            return;

        var locked = ent.Comp.State is Engaged;
        var user = args.User;
        AlternativeVerb verb = new()
        {
            Act = locked ? () => TryUnlock(ent, id, user) : () => TryLock(ent, id, user),
            Text = Loc.GetString(locked ? "id-lock-verb-unlock" : "id-lock-verb-lock"),
            Icon = new SpriteSpecifier.Texture(new(locked
                ? "/Textures/Interface/VerbIcons/unlock.svg.192dpi.png"
                : "/Textures/Interface/VerbIcons/lock.svg.192dpi.png")),
            Priority = locked ? 1 : -1 // Higher priority than "toggle lock" if unlocking, but lower if locking, so alt-click works correctly.
        };
        args.Verbs.Add(verb);

        // If temporarily disengaged, allow re-engaging the old lock
        if (ent.Comp.State == TemporarilyDisengaged)
        {
            var verb2 = new AlternativeVerb
            {
                Act = () => TryReEngage(ent, id, user),
                Text = Loc.GetString("id-lock-verb-reengage"),
                Priority = -2,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/lock.svg.192dpi.png"))
            };
            args.Verbs.Add(verb2);
        }

    }

    private void OnSetIdLock(Entity<LockComponent> ent, ref IdLockSetEvent args)
    {
        if (!HasComp<LockComponent>(ent) || !HasComp<AccessReaderComponent>(ent))
        {
            Log.Error($"Tried to add an ID lock to an entity without a lock or access reader: {ToPrettyString(ent)}.");
            return;
        }

        // We do not remove the ID lock component either way to presereve master accesses on the entity.
        var lockComp = EnsureComp<IdLockComponent>(ent);
        lockComp.Enabled = args.Enable;

        if (!args.Enable)
            lockComp.State = Disengaged;

        Dirty(ent, lockComp);
    }


    private void OnLockDoAfter(Entity<IdLockComponent> ent, ref IdLockActivateDoAfterEvent args)
    {
        if (args.Cancelled || !TryComp<IdCardComponent>(args.Used, out var id))
            return;

        DoLock(ent, (args.Used.Value, id), args.User);
    }

    private void OnUnlockDoAfter(Entity<IdLockComponent> ent, ref IdLockDeactivateDoAfterEvent args)
    {
        if (args.Cancelled || !TryComp<IdCardComponent>(args.Used, out var id))
            return;

        DoUnlock(ent, (args.Used.Value, id), args.User);
    }
    #endregion

    #region Public API

    /// <summary>
    ///     Tries to start a lock do-after.
    /// </summary>
    public bool TryLock(Entity<IdLockComponent> ent, Entity<IdCardComponent> id, EntityUid user)
    {
        if (CheckAccess(id, ent, out var reason) == AccessLevel.None)
        {
            if (reason != null)
                _popups.PopupClient(Loc.GetString(reason), user, user);
            return false;
        }

        var args = new DoAfterArgs(EntityManager, user, ent.Comp.LockTime, new IdLockActivateDoAfterEvent(), ent, ent, id)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true
        };
        return _doAfter.TryStartDoAfter(args);
    }

    /// <summary>
    ///     Tries to start an unlock do-after.
    /// </summary>
    public bool TryUnlock(Entity<IdLockComponent> ent, Entity<IdCardComponent> id, EntityUid user)
    {
        if (CheckAccess(id, ent, out var reason) is var level && level == AccessLevel.None)
        {
            if (reason != null)
                _popups.PopupClient(Loc.GetString(reason), user, user);
            return false;
        }

        var time = level == AccessLevel.Master ? ent.Comp.MasterUnlockTime : ent.Comp.UnlockTime;
        var args = new DoAfterArgs(EntityManager, user, time, new IdLockDeactivateDoAfterEvent(), ent, ent, id)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true
        };

        return _doAfter.TryStartDoAfter(args);
    }

    /// <summary>
    ///     Try to re-activate the lock that was previously active. Requires a master ID. Does not require a do-after.
    /// </summary>
    public bool TryReEngage(Entity<IdLockComponent> ent, Entity<IdCardComponent> id, EntityUid user)
    {
        var level = CheckAccess(id, ent, out var reason);
        if (!level.HasFlag(AccessLevel.CanUnlock) && !level.HasFlag(AccessLevel.CanTemporarilyUnlock))
        {
            _popups.PopupClient("id-lock-fail-requires-master", user, user);
            return false;
        }

        if (ent.Comp.Info.OwnerName is null && ent.Comp.Info.OwnerJobTitle is null)
            return false;

        ent.Comp.State = Engaged;
        _popups.PopupPredicted(Loc.GetString("id-lock-re-locked", ("ent", ent.Owner)), ent, user);
        _audio.PlayPredicted(ent.Comp.LockSound, ent, user, ent.Comp.LockSound?.Params);

        return true;
    }

    /// <summary>
    ///     Actually locks the lock.
    /// </summary>
    public void DoLock(Entity<IdLockComponent> ent, Entity<IdCardComponent> id, EntityUid user)
    {
        if (CheckAccess(id, ent, out var reason) == AccessLevel.None)
            return;

        ent.Comp.State = Engaged;
        ent.Comp.Info = new() { OwnerName = id.Comp.FullName, OwnerJobTitle = id.Comp.JobTitle };
        Dirty(ent);

        _popups.PopupPredicted(Loc.GetString("id-lock-locked", ("ent", ent.Owner)), ent, user);
        _audio.PlayPredicted(ent.Comp.LockSound, ent, user, ent.Comp.LockSound?.Params);
    }

    /// <summary>
    ///     Actually unlocks the lock.
    /// </summary>
    public void DoUnlock(Entity<IdLockComponent> ent, Entity<IdCardComponent> id, EntityUid user)
    {
        var level = CheckAccess(id, ent, out _);
        if (!level.HasFlag(AccessLevel.CanUnlock) && !level.HasFlag(AccessLevel.CanTemporarilyUnlock))
            return;

        // When unlocking via master id, set to "temporarily disengaged"
        ent.Comp.State = level == AccessLevel.Master ? TemporarilyDisengaged : Disengaged;
        ent.Comp.Info = new();
        Dirty(ent);

        _popups.PopupPredicted(Loc.GetString("id-lock-unlocked", ("ent", ent.Owner)), ent, user);
        _audio.PlayPredicted(ent.Comp.UnlockSound, ent, user, ent.Comp.UnlockSound?.Params);
    }

    /// <summary>
    ///     Checks if the lock can be locked/unlocked at the given moment using the given ID, and if so, at which level.
    ///     If not, <paramref name="reason"/> contains a locale string to be displayed to the user as a popup, if necessary.
    /// </summary>
    public AccessLevel CheckAccess(Entity<IdCardComponent> id, Entity<IdLockComponent> lockable, out string? reason)
    {
        reason = null;
        if (!TryComp<AccessReaderComponent>(lockable, out var reader) || !reader.Enabled)
        {
            // Probably emagged or something, we only allow to remove it if it's active (which shouldn't happen hopefully)
            reason = "id-lock-fail-disabled";
            return AccessLevel.CanUnlock;
        }

        var level = MatchId(id, lockable);
        if (lockable.Comp.State is Engaged && level == AccessLevel.None)
        {
            reason = "id-lock-fail-access-no-match";
            return level;
        }

        // Note: we allow to *remove* the lock if it is active but the owner somehow lost access to it, as a failsafe.
        if (!_access.IsAllowed(id, lockable, reader))
        {
            reason = "lock-comp-has-user-access-fail";
            return level & ~AccessLevel.CanLock;
        }

        // We disallow engaging the ID lock while the normal lock is unlocked.
        if (!_locks.IsLocked(lockable.Owner))
        {
            reason = "id-lock-fail-must-be-locked";
            return level & ~AccessLevel.CanLock;
        }

        return level;
    }

    #endregion

    private AccessLevel MatchId(Entity<IdCardComponent> id, Entity<IdLockComponent> lockable)
    {
        if (TryComp<AccessComponent>(id, out var access) && lockable.Comp.MasterAccesses.Any(it => access.Tags.Contains(it)))
            return AccessLevel.Master;

        // ID always matches if the lock is inactive.
        if (lockable.Comp.State is not Engaged)
            return AccessLevel.CanLock;

        if (lockable.Comp.Info.OwnerName == id.Comp.FullName && lockable.Comp.Info.OwnerJobTitle == id.Comp.JobTitle)
            return AccessLevel.CanUnlock;

        return AccessLevel.None;
    }

    [Flags]
    public enum AccessLevel : int
    {
        CanLock = 1,
        CanUnlock = 2,
        CanTemporarilyUnlock = 4,

        None = 0,
        Regular = CanLock | CanUnlock,
        Master = Regular | CanTemporarilyUnlock,
    }
}
