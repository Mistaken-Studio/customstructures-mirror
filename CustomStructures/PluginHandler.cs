// -----------------------------------------------------------------------
// <copyright file="PluginHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Exiled.API.Enums;
using Exiled.API.Features;
using HarmonyLib;
using Mistaken.CustomStructures.Pathlights;
using Mistaken.UnityPrefabs;

// ReSharper disable StringLiteralTypo
namespace Mistaken.CustomStructures
{
    /// <inheritdoc/>
    internal class PluginHandler : Plugin<Config>
    {
        /// <inheritdoc/>
        public override string Author => "Mistaken Devs";

        /// <inheritdoc/>
        public override string Name => "CustomStructures";

        /// <inheritdoc/>
        public override string Prefix => "MCustomStructures";

        /// <inheritdoc/>
        public override PluginPriority Priority => PluginPriority.Default;

        /// <inheritdoc/>
        public override System.Version RequiredExiledVersion => new System.Version(5, 0, 0);

        /// <inheritdoc/>
        public override void OnEnabled()
        {
            Instance = this;

            this.harmony = new Harmony("mistaken.customstructures");
            this.harmony.PatchAll();

            _ = new CustomStructuresHandler(this);
            _ = new PathLightHandler(this);

            CustomStructuresHandler.AssetsHandlers[AssetMeta.AssetType.SURFACE_GATEA_TOWER_ELEVATOR] = typeof(AssetHandlers.SurfaceGateATowerElevatorHandler);
            CustomStructuresHandler.AssetsHandlers[AssetMeta.AssetType.WARHEAD_TIMER] = typeof(AssetHandlers.WarheadTimerHandler);
            CustomStructuresHandler.AssetsHandlers[AssetMeta.AssetType.EZ_ELECTRICALROOM] = typeof(AssetHandlers.ElectricalRoomHandler);
            CustomStructuresHandler.AssetsHandlers[AssetMeta.AssetType.RESPAWN_TIMER] = typeof(AssetHandlers.RespawnTimerHandler);
            CustomStructuresHandler.AssetsHandlers[AssetMeta.AssetType.EZ_ELECTRICALROOM] = typeof(AssetHandlers.ElectricalRoomHandler);

            API.Diagnostics.Module.OnEnable(this);

            base.OnEnabled();
        }

        /// <inheritdoc/>
        public override void OnDisabled()
        {
            API.Diagnostics.Module.OnDisable(this);

            base.OnDisabled();
        }

        internal static PluginHandler Instance { get; private set; }

        private Harmony harmony;
    }
}
