// -----------------------------------------------------------------------
// <copyright file="CustomStructuresHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using AdminToys;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Interfaces;
using Footprinting;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Ammo;
using Mirror;
using Mistaken.API.Diagnostics;
using Mistaken.API.Extensions;
using System.Threading.Tasks;
using UnityEngine;

namespace Mistaken.CustomStructures //V3
{
    public class CustomStructuresHandler : Module
    {
        public CustomStructuresHandler(IPlugin<IConfig> plugin)
            : base(plugin)
        {
        }

        public override string Name => "CustomStructuresHandler";

        public override void OnDisable()
        {
            // throw new NotImplementedException();
        }

        public override void OnEnable()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers += this.Server_WaitingForPlayers;

            // var watcher = new FileSystemWatcher(Path.Combine(Paths.Plugins, "AssetBoundle"));
            // watcher.Changed += Watcher_Changed;
        }

        private void Server_WaitingForPlayers()
        {
            ReloadAssets();

            var bridge1 = new GameObject();
            bridge1.transform.position = new Vector3(75.28f, 1000 - 7.289f, -49.5f);
            SpawnAsset("bridge", bridge1.transform);
            Task.Delay(5).Wait();

            var bridge2 = new GameObject();
            bridge2.transform.position = new Vector3(87.53f, 1000 - 7.289f, -59.27f);
            bridge2.transform.eulerAngles = Vector3.up * -90f;
            SpawnAsset("bridge", bridge2.transform);
            Task.Delay(5).Wait();

            var surfaceGateBBridgeConnector = new GameObject();
            surfaceGateBBridgeConnector.transform.position = new Vector3(0, 1000, 0);
            SpawnAsset("surface_gateb_bridge_connector", surfaceGateBBridgeConnector.transform);
            Task.Delay(5).Wait();

            var surfaceGateAStairsLock = new GameObject();
            surfaceGateAStairsLock.transform.position = new Vector3(0, 1000, 0);
            SpawnAsset("surface_gatea_stairs_lock", surfaceGateAStairsLock.transform);
            Task.Delay(5).Wait();

            var surfaceGateATunnelCIDoor = new GameObject();
            surfaceGateATunnelCIDoor.transform.position = new Vector3(0, 1000, 0);
            SpawnAsset("surface_gatea_tunnel_ci_door", surfaceGateATunnelCIDoor.transform);
            Task.Delay(5).Wait();

            var surfaceGateATunnelElevatorDoor = new GameObject();
            surfaceGateATunnelElevatorDoor.transform.position = new Vector3(0, 1000, 0);
            SpawnAsset("surface_gatea_tunnel_elevator_door", surfaceGateATunnelElevatorDoor.transform);
            Task.Delay(5).Wait();

            var surfaceGateATower = new GameObject();
            surfaceGateATower.transform.position = new Vector3(0, 1000, 0);
            SpawnAsset("surface_gatea_middletower", surfaceGateATower.transform);
            Task.Delay(5).Wait();

            var surfaceGateATowerArmory = new GameObject();
            surfaceGateATowerArmory.transform.position = new Vector3(0, 1000, 0);
            SpawnAsset("surface_gatea_tower_armory", surfaceGateATowerArmory.transform);
            Task.Delay(5).Wait();

            var surfaceGateATowerSCP1499Chamber = new GameObject();
            surfaceGateATowerSCP1499Chamber.transform.position = new Vector3(0, 1000, 0);
            SpawnAsset("surface_gatea_tower_scp1499_chamber", surfaceGateATowerSCP1499Chamber.transform);
            Task.Delay(5).Wait();

            var surfaceGateATowerElevatorBottom = new GameObject();
            surfaceGateATowerElevatorBottom.transform.position = new Vector3(0, 1000, 0);
            SpawnAsset("surface_gatea_tower_elevator_bottom", surfaceGateATowerElevatorBottom.transform);
            Task.Delay(5).Wait();

            var surfaceGateATowerElevatorTop = new GameObject();
            surfaceGateATowerElevatorTop.transform.position = new Vector3(0, 1000, 0);
            SpawnAsset("surface_gatea_tower_elevator_top", surfaceGateATowerElevatorTop.transform);
            Task.Delay(5).Wait();

            var surfaceCICar = new GameObject();
            surfaceCICar.transform.position = new Vector3(0, 1000, 0);
            SpawnAsset("surface_cicar", surfaceCICar.transform);
            Task.Delay(5).Wait();
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
            var boundles = LoadBoundles(Path.Combine(Paths.Plugins, "AssetBoundle"));
            List<GameObject> assets = new List<GameObject>();

            foreach (var boundle in boundles)
                assets.AddRange(boundle.LoadAllAssets<GameObject>());

            foreach (var boundle in boundles)
                boundle.Unload(false);

            return assets;
        }

        public static readonly Dictionary<string, Asset> Assets = new Dictionary<string, Asset>();

        public static void ReloadAssets()
        {
            Assets.Clear();
            foreach (var asset in GetAssets())
            {
                Assets.Add(
                    asset.name,
                    new Asset
                {
                    Prefab = asset,
                    AssetName = asset.name,
                });
            }
        }

        public static GameObject SpawnAsset(string name, Transform parent = null)
        {
            if (!Assets.TryGetValue(name, out var asset))
            {
                throw new ArgumentException($"Unknown Asset name \"{name}\"", nameof(name));
            }

            var spawned = asset.Spawn(parent);

            Exiled.API.Features.Log.Debug($"Loaded {name}", true);

            return spawned;
        }
    }
}
