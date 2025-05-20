using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Chat;
using Content.Server.Chat.Systems;
using Content.Shared.DoAfter;
using Content.Server.Power.EntitySystems;
using Content.Shared._Shitmed.Autodoc.Components;
using Content.Shared._Shitmed.Autodoc.Systems;
using Content.Shared._Shitmed.Medical.Surgery;
using Content.Shared.Bed.Sleep;
using Content.Server.Construction;

namespace Content.Server._Shitmed.Autodoc.Systems;

public sealed class AutodocSystem : SharedAutodocSystem
{
    [Dependency] private readonly InternalsSystem _internals = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly SleepingSystem _sleepingSystem = default!;
    private const float UpgradeAddsPercent = 0.50f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AutodocComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<AutodocComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveAutodocComponent, AutodocComponent>();
        var now = Timing.CurTime;
        while (query.MoveNext(out var uid, out var active, out var comp))
        {
            if (now < active.NextUpdate)
                continue;

            active.NextUpdate = now + comp.UpdateDelay;
            if (HasComp<ActiveDoAfterComponent>(uid) || !_power.IsPowered(uid))
                continue;

            if (Proceed((uid, comp, active)))
                RemCompDeferred<ActiveAutodocComponent>(uid);
        }
    }

    protected override void WakePatient(EntityUid patient)
    {
        // incase they are using nitrous, disconnect it so they can get woken up later on
        if (TryComp<InternalsComponent>(patient, out var internals) && _internals.AreInternalsWorking(patient, internals))
            _internals.DisconnectTank(internals);

        _sleepingSystem.TryWaking(patient);
    }

    public override void Say(EntityUid uid, string msg)
    {
        _chat.TrySendInGameICMessage(uid, msg, InGameICChatType.Speak, hideChat: false, hideLog: true, checkRadioPrefix: false);
    }

    private void OnRefreshParts(EntityUid uid, AutodocComponent component, RefreshPartsEvent args)
    {
        var ratingSurgerySpeed = args.PartRatings[component.MachinePartSurgerySpeed];

        component.SurgerySpeed = component.BaseSurgerySpeed * (1 + (ratingSurgerySpeed - 1) * UpgradeAddsPercent);

        if (TryComp<SurgerySpeedModifierComponent>(uid, out var surgerySpeedModifier))
            surgerySpeedModifier.SpeedModifier = component.SurgerySpeed;
    }

    private void OnUpgradeExamine(EntityUid uid, AutodocComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("autodoc-upgrade-surgery-speed", component.SurgerySpeed / component.BaseSurgerySpeed);
    }
}
