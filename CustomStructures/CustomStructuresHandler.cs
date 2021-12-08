// -----------------------------------------------------------------------
// <copyright file="CustomStructuresHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using Mirror;
using Mistaken.API.Diagnostics;
using UnityEngine;

namespace Mistaken.CustomStructures
{
    /// <inheritdoc/>
    public class CustomStructuresHandler : Module
    {
        /// <summary>
        /// Assets bound to their name.
        /// </summary>
        public static readonly Dictionary<string, Asset> Assets = new Dictionary<string, Asset>();

        /// <summary>
        /// Reloads <see cref="Assets"/>.
        /// </summary>
        public static void ReloadAssets()
        {
            Assets.Clear();
            foreach (var asset in GetAssets())
            {
                Assets[asset.name.ToLower()] = new Asset
                {
                    Prefab = asset,
                    AssetName = asset.name,
                };
            }
        }

        /// <summary>
        /// Spawns asset.
        /// </summary>
        /// <param name="name">Asset name.</param>
        /// <param name="parent">Parent if there is.</param>
        /// <returns>Spawn asset.</returns>
        /// <exception cref="ArgumentException">If asset with <paramref name="name"/> was not found.</exception>
        public static GameObject SpawnAsset(string name, Transform parent = null)
        {
            if (!TrySpawnAsset(name, parent, out var tor))
                throw new ArgumentException($"Unknown Asset name \"{name}\"", nameof(name));

            return tor;
        }

        /// <summary>
        /// Tries to spawn asset.
        /// </summary>
        /// <param name="name">Asset name.</param>
        /// <param name="parent">Parent if there is.</param>
        /// <param name="spawnedAsset">Spawn asset.</param>
        /// <returns>If asset was spawned.</returns>
        public static bool TrySpawnAsset(string name, Transform parent, out GameObject spawnedAsset)
        {
            if (!Assets.TryGetValue(name.ToLower(), out var asset))
            {
                spawnedAsset = null;
                return false;
            }

            spawnedAsset = asset.Spawn(parent);

            Exiled.API.Features.Log.Debug($"Loaded {name}", true);

            return true;
        }

        /// <inheritdoc cref="Module.Module(IPlugin{IConfig})"/>
        public CustomStructuresHandler(IPlugin<IConfig> plugin)
            : base(plugin)
        {
        }

        /// <inheritdoc/>
        public override string Name => "CustomStructuresHandler";

        /// <inheritdoc/>
        public override void OnDisable()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers -= this.Server_WaitingForPlayers;
            Exiled.Events.Handlers.Player.InteractingDoor -= this.Player_InteractingDoor;
        }

        /// <inheritdoc/>
        public override void OnEnable()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers += this.Server_WaitingForPlayers;
            Exiled.Events.Handlers.Player.InteractingDoor += this.Player_InteractingDoor;
        }

        private static IEnumerable<AssetBundle> LoadBoundles(string files)
        {
            List<AssetBundle> tor = new List<AssetBundle>();
            foreach (var item in Directory.GetFiles(files))
            {
                tor.Add(AssetBundle.LoadFromFile(item));
            }

            foreach (var item in Directory.GetDirectories(files))
            {
                tor.AddRange(LoadBoundles(item));
            }

            return tor;
        }

        private static IEnumerable<GameObject> GetAssets()
        {
            if (!Directory.Exists(Path.Combine(Paths.Plugins, "AssetBoundle")))
            {
                Exiled.API.Features.Log.Warn($"{Path.Combine(Paths.Plugins, "AssetBoundle")} was not found, creating ...");
                Directory.CreateDirectory(Path.Combine(Paths.Plugins, "AssetBoundle"));
                return new GameObject[0];
            }

            var boundles = LoadBoundles(Path.Combine(Paths.Plugins, "AssetBoundle"));
            List<GameObject> assets = new List<GameObject>();

            foreach (var boundle in boundles)
                assets.AddRange(boundle.LoadAllAssets<GameObject>());

            foreach (var boundle in boundles)
                boundle.Unload(false);

            return assets;
        }

        private void Player_InteractingDoor(Exiled.Events.EventArgs.InteractingDoorEventArgs ev)
        {
            if (!ev.IsAllowed)
                return;
            if (Asset.ConnectedAnimators.TryGetValue(ev.Door.Base, out var animator))
            {
                animator.SetBool("IsOpen", !ev.Door.IsOpen);
            }

            if (Asset.RemovePostUse.Contains(ev.Door.Base))
            {
                this.CallDelayed(.25f, () =>
                {
                    NetworkServer.Destroy(ev.Door.Base.gameObject);
                });
            }
        }

        private void Server_WaitingForPlayers()
        {
            ReloadAssets();

            var bridge1 = new GameObject();
            bridge1.transform.position = new Vector3(0, 1000, 0);
            if (!TrySpawnAsset("surface_gateb_bridge_forward", bridge1.transform, out _))
                this.Log.Warn($"Failed to spawn asset (surface_gateb_bridge_forward), is it present in AssetBoundle folder or in any boundle?");
            Task.Delay(5).Wait();

            var bridge2 = new GameObject();
            bridge2.transform.position = new Vector3(0, 1000, 0);
            if (!TrySpawnAsset("surface_gateb_bridge_left", bridge2.transform, out _))
                this.Log.Warn($"Failed to spawn asset (surface_gateb_bridge_left), is it present in AssetBoundle folder or in any boundle?");
            Task.Delay(5).Wait();

            var surfaceGateBBridgeConnector = new GameObject();
            surfaceGateBBridgeConnector.transform.position = new Vector3(0, 1000, 0);
            if (!TrySpawnAsset("surface_gateb_bridge_connector", surfaceGateBBridgeConnector.transform, out _))
                this.Log.Warn($"Failed to spawn asset (surface_gateb_bridge_connector), is it present in AssetBoundle folder or in any boundle?");
            Task.Delay(5).Wait();

            var surfaceGateAStairsLock = new GameObject();
            surfaceGateAStairsLock.transform.position = new Vector3(0, 1000, 0);
            if (!TrySpawnAsset("surface_gatea_stairs_lock", surfaceGateAStairsLock.transform, out _))
                this.Log.Warn($"Failed to spawn asset (surface_gatea_stairs_lock), is it present in AssetBoundle folder or in any boundle?");
            Task.Delay(5).Wait();

            var surfaceGateATunnelCIDoor = new GameObject();
            surfaceGateATunnelCIDoor.transform.position = new Vector3(0, 1000, 0);
            if (!TrySpawnAsset("surface_gatea_tunnel_ci_door", surfaceGateATunnelCIDoor.transform, out _))
                this.Log.Warn($"Failed to spawn asset (surface_gatea_tunnel_ci_door), is it present in AssetBoundle folder or in any boundle?");
            Task.Delay(5).Wait();

            var surfaceGateATunnelElevatorDoor = new GameObject();
            surfaceGateATunnelElevatorDoor.transform.position = new Vector3(0, 1000, 0);
            if (!TrySpawnAsset("surface_gatea_tunnel_elevator_door", surfaceGateATunnelElevatorDoor.transform, out _))
                this.Log.Warn($"Failed to spawn asset (surface_gatea_tunnel_elevator_door), is it present in AssetBoundle folder or in any boundle?");
            Task.Delay(5).Wait();

            var surfaceGateATower = new GameObject();
            surfaceGateATower.transform.position = new Vector3(0, 1000, 0);
            if (!TrySpawnAsset("surface_gatea_middletower", surfaceGateATower.transform, out _))
                this.Log.Warn($"Failed to spawn asset (surface_gatea_middletower), is it present in AssetBoundle folder or in any boundle?");
            Task.Delay(5).Wait();

            var surfaceGateATowerArmory = new GameObject();
            surfaceGateATowerArmory.transform.position = new Vector3(0, 1000, 0);
            if (!TrySpawnAsset("surface_gatea_tower_armory", surfaceGateATowerArmory.transform, out _))
                this.Log.Warn($"Failed to spawn asset (surface_gatea_tower_armory), is it present in AssetBoundle folder or in any boundle?");
            Task.Delay(5).Wait();

            var surfaceGateATowerSCP1499Chamber = new GameObject();
            surfaceGateATowerSCP1499Chamber.transform.position = new Vector3(0, 1000, 0);
            if (!TrySpawnAsset("surface_gatea_tower_scp1499_chamber", surfaceGateATowerSCP1499Chamber.transform, out _))
                this.Log.Warn($"Failed to spawn asset (surface_gatea_tower_scp1499_chamber), is it present in AssetBoundle folder or in any boundle?");
            Task.Delay(5).Wait();

            var surfaceGateATowerElevatorBottom = new GameObject();
            surfaceGateATowerElevatorBottom.transform.position = new Vector3(0, 1000, 0);
            if (!TrySpawnAsset("surface_gatea_tower_elevator_bottom", surfaceGateATowerElevatorBottom.transform, out _))
                this.Log.Warn($"Failed to spawn asset (surface_gatea_tower_elevator_bottom), is it present in AssetBoundle folder or in any boundle?");
            Task.Delay(5).Wait();

            var surfaceGateATowerElevatorTop = new GameObject();
            surfaceGateATowerElevatorTop.transform.position = new Vector3(0, 1000, 0);
            if (!TrySpawnAsset("surface_gatea_tower_elevator_top", surfaceGateATowerElevatorTop.transform, out _))
                this.Log.Warn($"Failed to spawn asset (surface_gatea_tower_elevator_top), is it present in AssetBoundle folder or in any boundle?");
            Task.Delay(5).Wait();

            var surfaceCICar = new GameObject();
            surfaceCICar.transform.position = new Vector3(0, 1000, 0);
            if (!TrySpawnAsset("surface_cicar", surfaceCICar.transform, out _))
            this.Log.Warn($"Failed to spawn asset (surface_cicar), is it present in AssetBoundle folder or in any boundle?");
            Task.Delay(5).Wait();
        }
    }
}
