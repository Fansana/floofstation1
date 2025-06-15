// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 SolsticeOfTheWinter <solsticeofthewinter@gmail.com>
// SPDX-FileCopyrightText: 2025 TheBorzoiMustConsume <197824988+TheBorzoiMustConsume@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Physics.Events;

namespace Content.Shared._Goobstation.Religion;

public sealed partial class WeakToHolySystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    private readonly Dictionary<EntityUid, FixedPoint2> _originalDamageCaps = new();

    public const string ContainerId = "Biological";
    public const string TransformedContainerId = "BiologicalMetaphysical";
    public const string PassiveDamageType = "Holy";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WeakToHolyComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<WeakToHolyComponent> ent, ref ComponentStartup args)
    {
        // Only change to "BiologicalMetaphysical" if the original damage container was "Biological"
        if (TryComp<DamageableComponent>(ent, out var damageable) && damageable.DamageContainerID == ContainerId)
            _damageableSystem.ChangeDamageContainer(ent, TransformedContainerId);
    }




}
