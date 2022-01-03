// -----------------------------------------------------------------------
// <copyright file="PluginHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Exiled.API.Enums;
using Exiled.API.Features;
using HarmonyLib;
using Mistaken.UnityPrefabs;

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
        public override Version RequiredExiledVersion => new Version(4, 1, 2);

        /// <inheritdoc/>
        public override void OnEnabled()
        {
            Instance = this;

            this.harmony = new HarmonyLib.Harmony("mistaken.customstructures");
            this.harmony.PatchAll();

            new CustomStructuresHandler(this);

            CustomStructuresHandler.AssetsHandlers[AssetMeta.AssetType.SURFACE_GATEA_TOWER_ELEVATOR] = typeof(AssetHandlers.SurfaceGateATowerElevatorHandler);
            CustomStructuresHandler.AssetsHandlers[AssetMeta.AssetType.WARHEAD_TIMER] = typeof(AssetHandlers.WarheadTimerHandler);

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
