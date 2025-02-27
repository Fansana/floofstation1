using Content.Shared.Atmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.Atmos.Piping.Other.Components
{
    [RegisterComponent]
    public sealed partial class GasTesterComponent : Component
    {
        [DataField("mixzero")]
        public GasMixture MixZero { get; set; } = new();

        [DataField("mix-x")]
        public GasMixture MixX { get; set; } = new();

        [DataField("mix-y")]
        public GasMixture MixY { get; set; } = new();

        public GasMixture MixLocal;
    }
}
