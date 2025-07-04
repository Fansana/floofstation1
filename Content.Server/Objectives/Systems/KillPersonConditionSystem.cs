using Content.Server._Floof.Traits.Components;
using Content.Server.Administration.Toolshed;
using Content.Server.Objectives.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.CCVar;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Configuration;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles kill person condition logic and picking random kill targets.
/// </summary>
public sealed class KillPersonConditionSystem : EntitySystem
{
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KillPersonConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);

        SubscribeLocalEvent<PickRandomPersonComponent, ObjectiveAssignedEvent>(OnPersonAssigned);

        SubscribeLocalEvent<PickRandomHeadComponent, ObjectiveAssignedEvent>(OnHeadAssigned);
    }

    private void OnGetProgress(EntityUid uid, KillPersonConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!_target.GetTarget(uid, out var target))
            return;

        args.Progress = GetProgress(target.Value, comp.RequireDead);
    }

    private void OnPersonAssigned(EntityUid uid, PickRandomPersonComponent comp, ref ObjectiveAssignedEvent args)
    {
        // invalid objective prototype
        if (!TryComp<TargetObjectiveComponent>(uid, out var target))
        {
            args.Cancelled = true;
            return;
        }

        // target already assigned
        if (target.Target != null)
            return;

        // no other humans to kill
        var allHumans = _mind.GetAliveHumans(exclude: args.MindId);
        if (allHumans.Count == 0)
        {
            args.Cancelled = true;
            return;
        }

        // Floofstation Edit Start
        if (comp.ObjectiveType > 0)
        {
            allHumans = MarkedList(allHumans, comp.ObjectiveType);
        }
        else
        {
            //Cancel the objective if no target type was declared.
            //Prevent potential edge cases of people that didn't opt in getting assigned.
            args.Cancelled = true;
            return;
            //Legacy code below
            /*          foreach (var mind in allHumans)
                                        if (_job.MindTryGetJob(mind, out _, out var prototype) && !prototype.CanBeAntagTarget)
                                           allHumans.Remove(mind); */
        }
        //If the culled list is now empty
        if (allHumans.Count == 0)
        {
            args.Cancelled = true;
            return;
        }
        // Floofstation Edit End

        _target.SetTarget(uid, _random.Pick(allHumans), target);
    }
    // Floofstation - added this entire function
    public HashSet<Entity<MindComponent>> MarkedList(HashSet<Entity<MindComponent>> markedList, ObjectiveTypes objType)
    {
        if (objType.HasFlag(ObjectiveTypes.TraitorKill))//Culls from the list of all alive minds anyone that hasn't opted into kill targetting.
        {
            foreach (var mind in markedList)
            {
                if (!TryComp<MarkedComponent>(mind.Comp.CurrentEntity, out var mComp) || !mComp.TargetType.HasFlag(ObjectiveTypes.TraitorKill))
                {
                    markedList.Remove(mind);
                }
            }
        }
        if (objType.HasFlag(ObjectiveTypes.TraitorTeach))//Culls from the list of all alive minds anyone that hasn't opted into teach targetting.
        {
            foreach (var mind in markedList)
            {
                if (!TryComp<MarkedComponent>(mind.Comp.CurrentEntity, out var mComp) || !mComp.TargetType.HasFlag(ObjectiveTypes.TraitorTeach))
                {
                    markedList.Remove(mind);
                }
            }
        }
        return markedList;
    }
    private void OnHeadAssigned(EntityUid uid, PickRandomHeadComponent comp, ref ObjectiveAssignedEvent args)
    {
        // invalid prototype
        if (!TryComp<TargetObjectiveComponent>(uid, out var target))
        {
            args.Cancelled = true;
            return;
        }

        // target already assigned
        if (target.Target != null)
            return;

        // no other humans to kill
        var allHumans = _mind.GetAliveHumans(exclude: args.MindId);
        if (allHumans.Count == 0)
        {
            args.Cancelled = true;
            return;
        }

        var allHeads = new List<EntityUid>();
        foreach (var mind in allHumans)
        {
            // RequireAdminNotify used as a cheap way to check for command department
            if (_job.MindTryGetJob(mind, out _, out var prototype) && prototype.RequireAdminNotify)
                allHeads.Add(mind);
        }

        var pickCatch = new PickRandomPersonComponent();//Floofstation: Prepares for an edge case where there are no command members.
        if (allHeads.Count == 0)
        {
            pickCatch.ObjectiveType = ObjectiveTypes.TraitorTeach;//Floofstation: Sets the objective type
            OnPersonAssigned(uid, pickCatch, ref args);//Floofstation: Assigns a target from the Marked if possible
            return;//Floofstation: Escapes
            // allHeads = allHumans.Select(x => x.Owner).ToList(); // fallback to non-head target //Floofstation: Disabled to prevent non-consenting targets.
        }

        _target.SetTarget(uid, _random.Pick(allHeads), target);
    }

    private float GetProgress(EntityUid target, bool requireDead)
    {
        // deleted or gibbed or something, counts as dead
        if (!TryComp<MindComponent>(target, out var mind) || mind.OwnedEntity == null)
            return 1f;

        // dead is success
        if (_mind.IsCharacterDeadIc(mind))
            return 1f;

        // if the target has to be dead dead then don't check evac stuff
        if (requireDead)
            return 0f;

        // if evac is disabled then they really do have to be dead
        if (!_config.GetCVar(CCVars.EmergencyShuttleEnabled))
            return 0f;

        // target is escaping so you fail
        if (_emergencyShuttle.IsTargetEscaping(mind.OwnedEntity.Value))
            return 0f;

        // evac has left without the target, greentext since the target is afk in space with a full oxygen tank and coordinates off.
        if (_emergencyShuttle.ShuttlesLeft)
            return 1f;

        // if evac is still here and target hasn't boarded, show 50% to give you an indicator that you are doing good
        return _emergencyShuttle.EmergencyShuttleArrived ? 0.5f : 0f;
    }
}
