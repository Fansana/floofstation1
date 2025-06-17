using Content.Server.Body.Components;
using Content.Shared.Contests;
using Content.Shared.HeightAdjust;

namespace Content.Server._Floof.HeightAdjust;

public sealed class ThermalRegulatorAdjustSystem : EntitySystem
{
    [Dependency] private readonly ContestsSystem _contests = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ThermalRegulatorAffectedByMassComponent, MapInitEvent>((uid, comp, _) => TryAdjust((uid, comp)));
        SubscribeLocalEvent<ThermalRegulatorAffectedByMassComponent, HeightAdjustedEvent>((uid, comp, _) => TryAdjust((uid, comp)));
    }

    /// <summary>
    ///     Adjusts the bloodstream of the specified entity based on the settings provided by the component.
    /// </summary>
    public bool TryAdjust(Entity<ThermalRegulatorAffectedByMassComponent> ent)
    {
        if (!TryComp<ThermalRegulatorComponent>(ent, out var thermals))
            return false;

        // We raise to the power of 2/3 due to the square-cube law:
        // a mob 2x the size loses/gains 4x more heat through their surface area, but generates 8x the heat throughout their volume
        // this is merely an approximation to keep things more fair and prevent us from punishing smaller people too much
        var factor = MathF.Pow(_contests.MassContest(ent, bypassClamp: true, rangeFactor: 10), 1.5f) / ent.Comp.OldFactor;
        thermals.MetabolismHeat *= (float) factor;
        thermals.RadiatedHeat *= (float) factor;
        thermals.ImplicitHeatRegulation *= (float) factor;
        thermals.ShiveringHeatRegulation *= (float) factor;
        thermals.SweatHeatRegulation *= (float) factor;

        ent.Comp.OldFactor = factor;

        return true;
    }
}
