using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Body.Components;

namespace Content.Server.Traits.Assorted;

public sealed class SuccubusSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _bodySystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SuccubusComponent, ComponentInit>(OnSpawn);
    }

    private void OnSpawn(Entity<SuccubusComponent> entity, ref ComponentInit args)
    {
        if (!TryComp<BodyComponent>(entity, out var body))
            return;

        if (!_bodySystem.TryGetBodyOrganComponents<MetabolizerComponent>(entity, out var metabolizers, body))
            return;

        foreach (var (metabolizer, _) in metabolizers)
        {
            if (metabolizer.MetabolizerTypes is null
                || metabolizer.MetabolismGroups is null)
                continue;

            foreach (var metabolismGroup in metabolizer.MetabolismGroups)
            {
                // Add the Succubus metabolizer type to the liver and equivalent organs.
                if (metabolismGroup.Id == "NaturalSauce")
                    metabolizer.MetabolizerTypes.Add("Succubus");
            }
        }
    }
}
