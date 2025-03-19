using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Other.Components;
using Content.Shared.Atmos;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.Atmos.Piping.Other.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasTesterSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasTesterComponent, AtmosDeviceUpdateEvent>(OnTesterUpdated);
        }

        private void OnTesterUpdated(Entity<GasTesterComponent> ent, ref AtmosDeviceUpdateEvent args)
        {
            var (uid, tester) = ent;
            GasMixture? containingMixture = _atmosphereSystem.GetContainingMixture(uid);
            if (containingMixture == null)
                return;

            if (tester.MixLocal == null)
            {
                Vector2i tilePos = _transformSystem.GetGridTilePositionOrDefault(uid);
                tester.MixLocal = tester.MixZero.Clone();
                GasMixture mixXComp = tester.MixX.Clone(),
                    mixYComp = tester.MixY.Clone();
                mixXComp.Multiply(tilePos.X);
                mixYComp.Multiply(tilePos.Y);
                _atmosphereSystem.Merge(tester.MixLocal, mixXComp);
                _atmosphereSystem.Merge(tester.MixLocal, mixYComp);
            }

            containingMixture.Clear();
            _atmosphereSystem.Merge(containingMixture, tester.MixLocal);
        }
    }
}
