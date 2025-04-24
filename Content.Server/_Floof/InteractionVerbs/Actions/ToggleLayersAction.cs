using Content.Server.Humanoid;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.InteractionVerbs;

namespace Content.Server._Floof.InteractionVerbs.Actions;

/// <summary>
///     Toggles a humanoid visual layer.
/// </summary>
[Serializable]
public sealed partial class ToggleLayersAction : InteractionAction
{
    [DataField]
    public HumanoidVisualLayers? NeededMarkingCategory;

    [DataField]
    public HashSet<HumanoidVisualLayers> ToggleLayers;

    public override bool CanPerform(InteractionArgs args, InteractionVerbPrototype proto, bool isBefore, VerbDependencies deps)
    {
        if (NeededMarkingCategory == null)
            return true;

        var markingCategory = MarkingCategoriesConversion.FromHumanoidVisualLayers(NeededMarkingCategory.Value);

        return deps.EntMan.TryGetComponent(args.Target, out HumanoidAppearanceComponent? bodyAppearance)
           && bodyAppearance.MarkingSet.Markings.TryGetValue(markingCategory, out var markingList)
           && markingList.Count > 0; // Check if at least one entry exists
    }

    public override bool Perform(InteractionArgs args, InteractionVerbPrototype proto, VerbDependencies deps)
    {
        if (deps.EntMan.TryGetComponent<HumanoidAppearanceComponent>(args.Target, out var humanoidAppearance))
        {
            foreach (HumanoidVisualLayers layer in ToggleLayers)
            {
                deps.EntMan.System<HumanoidAppearanceSystem>().SetLayerVisibility(
                    args.Target,
                    layer, humanoidAppearance.HiddenLayers.Contains(layer)
                );
            }
        }

        return true;
    }
}
