using Content.Shared.Psionics.Abilities;
using Content.Shared.Psionics;
using Content.Shared.Eye;
using Content.Server.NPC.Systems;
using Robust.Shared.Containers;
using Robust.Server.GameObjects;

namespace Content.Server.Psionics
{
    public sealed class PsionicInvisibilitySystem : EntitySystem
    {
        [Dependency] private readonly VisibilitySystem _visibilitySystem = default!;
        [Dependency] private readonly PsionicInvisibilityPowerSystem _invisSystem = default!;
        [Dependency] private readonly NpcFactionSystem _npcFactonSystem = default!;
        [Dependency] private readonly SharedEyeSystem _eye = default!;
        public override void Initialize()
        {
            base.Initialize();
            /// Masking
            SubscribeLocalEvent<PotentialPsionicComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<PsionicInsulationComponent, ComponentInit>(OnInsulInit);
            SubscribeLocalEvent<PsionicInsulationComponent, ComponentShutdown>(OnInsulShutdown);

            /// Layer
            SubscribeLocalEvent<PsionicallyInvisibleComponent, ComponentInit>(OnInvisInit);
            SubscribeLocalEvent<PsionicallyInvisibleComponent, ComponentShutdown>(OnInvisShutdown);

            // PVS Stuff
            SubscribeLocalEvent<PsionicallyInvisibleComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
            SubscribeLocalEvent<PsionicallyInvisibleComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        }

        private void OnInit(EntityUid uid, PotentialPsionicComponent component, ComponentInit args)
        {
            SetCanSeePsionicInvisiblity(uid, false);
        }

        /// <summary>
        /// Being able to see invisible by default is no longer tracked by "Not having Potential Psionic".
        /// Anything intended to be immune to invisibility(and mind magic in general) should instead have PsionicInsulation as a built-in component
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="component"></param>
        /// <param name="args"></param>
        private void OnInsulInit(EntityUid uid, PsionicInsulationComponent component, ComponentInit args)
        {
            RaiseLocalEvent(uid, new PsionicInsulationEvent());

            if (HasComp<PsionicInvisibilityUsedComponent>(uid))
                _invisSystem.ToggleInvisibility(uid);

            if (_npcFactonSystem.ContainsFaction(uid, "PsionicInterloper"))
            {
                component.SuppressedFactions.Add("PsionicInterloper");
                _npcFactonSystem.RemoveFaction(uid, "PsionicInterloper");
            }

            if (_npcFactonSystem.ContainsFaction(uid, "GlimmerMonster"))
            {
                component.SuppressedFactions.Add("GlimmerMonster");
                _npcFactonSystem.RemoveFaction(uid, "GlimmerMonster");
            }

            SetCanSeePsionicInvisiblity(uid, true);
        }

        private void OnInsulShutdown(EntityUid uid, PsionicInsulationComponent component, ComponentShutdown args)
        {
            SetCanSeePsionicInvisiblity(uid, false);

            if (!HasComp<PsionicComponent>(uid))
            {
                component.SuppressedFactions.Clear();
                return;
            }

            foreach (var faction in component.SuppressedFactions)
            {
                _npcFactonSystem.AddFaction(uid, faction);
            }
            component.SuppressedFactions.Clear();
        }

        private void OnInvisInit(EntityUid uid, PsionicallyInvisibleComponent component, ComponentInit args)
        {
            var visibility = EntityManager.EnsureComponent<VisibilityComponent>(uid);

            _visibilitySystem.AddLayer(uid, visibility, (int) VisibilityFlags.PsionicInvisibility, false);
            _visibilitySystem.RemoveLayer(uid, visibility, (int) VisibilityFlags.Normal, false);
            _visibilitySystem.RefreshVisibility(uid, visibility);
            SetCanSeePsionicInvisiblity(uid, true);
        }


        private void OnInvisShutdown(EntityUid uid, PsionicallyInvisibleComponent component, ComponentShutdown args)
        {
            if (TryComp<VisibilityComponent>(uid, out var visibility))
            {
                _visibilitySystem.RemoveLayer(uid, visibility, (int) VisibilityFlags.PsionicInvisibility, false);
                _visibilitySystem.AddLayer(uid, visibility, (int) VisibilityFlags.Normal, false);
                _visibilitySystem.RefreshVisibility(uid, visibility);
                SetCanSeePsionicInvisiblity(uid, false);
            }
        }

        private void OnEntInserted(EntityUid uid, PsionicallyInvisibleComponent component, EntInsertedIntoContainerMessage args)
        {
            DirtyEntity(args.Entity);
        }

        private void OnEntRemoved(EntityUid uid, PsionicallyInvisibleComponent component, EntRemovedFromContainerMessage args)
        {
            DirtyEntity(args.Entity);
        }

        public void SetCanSeePsionicInvisiblity(EntityUid uid, bool set)
        {
            if (set == true)
            {
                if (EntityManager.TryGetComponent(uid, out EyeComponent? eye))
                {
                    _eye.SetVisibilityMask(uid, eye.VisibilityMask | (int) VisibilityFlags.PsionicInvisibility, eye);
                }
            } else
            {
                if (EntityManager.TryGetComponent(uid, out EyeComponent? eye))
                {
                    _eye.SetVisibilityMask(uid, eye.VisibilityMask & ~(int) VisibilityFlags.PsionicInvisibility, eye);
                }
            }
        }
    }
}
