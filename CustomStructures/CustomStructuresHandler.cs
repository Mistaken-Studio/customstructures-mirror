// -----------------------------------------------------------------------
// <copyright file="CustomStructuresHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using Mirror;
using Mistaken.API.Diagnostics;
using Mistaken.UnityPrefabs;
using UnityEngine;

namespace Mistaken.CustomStructures
{
    /// <inheritdoc/>
    public class CustomStructuresHandler : Module
    {
        /// <summary>
        /// Assets bound to their name.
        /// </summary>
        public static readonly Dictionary<AssetMeta.AssetType, Asset> Assets = new Dictionary<AssetMeta.AssetType, Asset>();

        internal static readonly Dictionary<AssetMeta.AssetType, Type> AssetsHandlers = new Dictionary<AssetMeta.AssetType, Type>();

        /// <summary>
        /// Reloads <see cref="Assets"/>.
        /// </summary>
        public static void ReloadAssets()
        {
            Assets.Clear();
            foreach (var asset in GetAssets())
            {
                var meta = asset.GetComponent<AssetMeta>();
                if (meta == null)
                {
                    Exiled.API.Features.Log.Warn($"Meta Script for {asset.name} not found");
                    continue;
                }

                Exiled.API.Features.Log.Debug($"Meta Script for {asset.name} found");

                Assets[meta.Type] = new Asset
                {
                    Prefab = asset,
                    Meta = meta,
                };
            }
        }

        /// <summary>
        /// Spawns asset.
        /// </summary>
        /// <param name="type">Asset Type.</param>
        /// <param name="parent">Parent if there is.</param>
        /// <returns>Spawn asset.</returns>
        /// <exception cref="ArgumentException">If asset with <paramref name="name"/> was not found.</exception>
        public static GameObject SpawnAsset(AssetMeta.AssetType type, Transform parent = null)
        {
            if (!TrySpawnAsset(type, parent, out var tor))
                throw new ArgumentException($"Unknown Asset Type \"{type}\"", nameof(type));

            return tor;
        }

        /// <summary>
        /// Tries to spawn asset.
        /// </summary>
        /// <param name="type">Asset Type.</param>
        /// <param name="parent">Parent if there is.</param>
        /// <param name="spawnedAsset">Spawn asset.</param>
        /// <returns>If asset was spawned.</returns>
        public static bool TrySpawnAsset(AssetMeta.AssetType type, Transform parent, out GameObject spawnedAsset)
        {
            if (!Assets.TryGetValue(type, out var asset))
            {
                spawnedAsset = null;
                return false;
            }

            spawnedAsset = asset.Spawn(parent);

            Exiled.API.Features.Log.Debug($"Loaded {type}", true);

            return true;
        }

        /// <summary>
        /// Tries to spawn asset.
        /// </summary>
        /// <param name="type">Asset Type.</param>
        /// <param name="parent">Parent if there is.</param>
        /// <param name="result">Spawned asset.</param>
        /// <returns>If asset was spawned.</returns>
        public static bool TryGetAndSpawnAsset(AssetMeta.AssetType type, Transform parent, out (GameObject obj, Asset asset) result)
        {
            if (!Assets.TryGetValue(type, out var asset))
            {
                result = default;
                return false;
            }

            result = (asset.Spawn(parent), asset);

            Exiled.API.Features.Log.Debug($"Loaded {type}", true);

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

            foreach (var asset in Assets)
            {
                foreach (var item in asset.Value.SpawnedChildren)
                {
                    foreach (var item2 in item.Value)
                        GameObject.Destroy(item2);
                    GameObject.Destroy(item.Key);
                }
            }

            Assets.Clear();
        }

        /// <inheritdoc/>
        public override void OnEnable()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers += this.Server_WaitingForPlayers;
            Exiled.Events.Handlers.Player.InteractingDoor += this.Player_InteractingDoor;

            ReloadAssets();
        }

        private static IEnumerable<AssetBundle> LoadBoundles(string files)
        {
            List<AssetBundle> tor = new List<AssetBundle>();
            foreach (var item in Directory.GetFiles(files))
                tor.Add(AssetBundle.LoadFromFile(item));

            foreach (var item in Directory.GetDirectories(files))
                tor.AddRange(LoadBoundles(item));

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

            /*foreach (var asset in assets)
                Exiled.API.Features.Log.Debug(asset.GetComponent<AssetMeta>()?.Rules.FirstOrDefault()?.Room, true);

            Exiled.API.Features.Log.Debug("==================", true);
            foreach (var asset in assets)
                Exiled.API.Features.Log.Debug(GameObject.Instantiate(asset).GetComponent<AssetMeta>()?.Rules.FirstOrDefault()?.Room, true);*/

            foreach (var boundle in boundles)
                boundle.Unload(false);
            return assets;
        }

        private static bool HasFlag(MapModType type, MapModType flag) => (type & flag) != 0;

        private readonly AssetMeta.AssetType[] alwaysLoaded = new AssetMeta.AssetType[]
        {
             AssetMeta.AssetType.SURFACE_GATEA_TOWER_SCP1499_CHAMBER,
             AssetMeta.AssetType.SURFACE_GATEA_TOWER_ELEVATOR,

             // AssetMeta.AssetType.SURFACE_CICAR,
             AssetMeta.AssetType.SURFACE_HELIPAD,

             // AssetMeta.AssetType.SURFACE_HELICOPTER,
             AssetMeta.AssetType.SURFACE_GATEA_TOWER_ARMORY_BIG,

             AssetMeta.AssetType.EZ_CURVE_ROOM,
             AssetMeta.AssetType.EZ_VENT_MEDICALROOM,

             AssetMeta.AssetType.WARHEAD_TIMER,
        };

        private ulong GenerateRandomULong(System.Random rng)
        {
            byte[] buf = new byte[8];
            rng.NextBytes(buf);
            ulong ulongRand = BitConverter.ToUInt64(buf, 0);

            return ulongRand;
        }

        private MapModType GenerateMapMods()
        {
            ulong random = this.GenerateRandomULong(new System.Random());
            random >>= 63 - 10;

            MapModType flags = this.ValidateMapMods((MapModType)random);

            return flags;
        }

        private MapModType ValidateMapMods(MapModType input)
        {
            if (HasFlag(input, MapModType.SURFACE_GATEB_BRIDGE_LEFT))
            {
                if (HasFlag(input, MapModType.SURFACE_GATEB_BRIDGE_LEFT_BUNKER))
                    input &= ~MapModType.SURFACE_GATEB_BRIDGE_LEFT_BUNKER;
            }

            if (HasFlag(input, MapModType.SURFACE_GATEA_TUNNEL_ELEVATOR_DOOR))
            {
                if (HasFlag(input, MapModType.SURFACE_GATEA_STAIRS_LOCK))
                    input &= ~MapModType.SURFACE_GATEA_STAIRS_LOCK;
            }

            if (HasFlag(input, MapModType.SURFACE_GATEA_TUNNEL_CI_DOOR))
            {
                if (HasFlag(input, MapModType.SURFACE_GATEA_TUNNEL_CI_DOOR_LOCKED))
                    input &= ~MapModType.SURFACE_GATEA_TUNNEL_CI_DOOR_LOCKED;
            }

            return input;
        }

        private AssetMeta.AssetType[] ParseMapMods(MapModType mod)
        {
            ulong modUl = (ulong)mod;
            List<AssetMeta.AssetType> assets = new List<AssetMeta.AssetType>();
            Exiled.API.Features.Log.Debug(mod, true);
            for (int i = 0; i < 64; i++)
            {
                if (((modUl >> i) & 1) != 0)
                {
                    Exiled.API.Features.Log.Debug(i + "|" + (MapModType)(1ul << i), true);
                    switch ((MapModType)(1ul << i))
                    {
                        case MapModType.SURFACE_GATEA_MIDDLE_TOWER:
                            assets.Add(AssetMeta.AssetType.SURFACE_GATEA_MIDDLE_TOWER);
                            break;
                        case MapModType.SURFACE_GATEA_STAIRS_LOCK:
                            assets.Add(AssetMeta.AssetType.SURFACE_GATEA_STAIRS_LOCK);
                            break;
                        /*case MapModType.SURFACE_GATEA_TOWER_ARMORY:
                            assets.Add(AssetType.SURFACE_GATEA_TOWER_ARMORY);
                            break;*/
                        case MapModType.SURFACE_GATEA_TUNNEL_CI_DOOR:
                            assets.Add(AssetMeta.AssetType.SURFACE_GATEA_TUNNEL_CI_DOOR);
                            break;
                        case MapModType.SURFACE_GATEA_TUNNEL_CI_DOOR_LOCKED:
                            assets.Add(AssetMeta.AssetType.SURFACE_GATEA_TUNNEL_CI_DOOR_LOCKED);
                            break;
                        case MapModType.SURFACE_GATEA_TUNNEL_ELEVATOR_DOOR:
                            assets.Add(AssetMeta.AssetType.SURFACE_GATEA_TUNNEL_ELEVATOR_DOOR);
                            break;
                        case MapModType.SURFACE_GATEB_BRIDGE_FORWARD:
                            assets.Add(AssetMeta.AssetType.SURFACE_GATEB_BRIDGE_FORWARD);
                            break;
                        case MapModType.SURFACE_GATEB_BRIDGE_LEFT:
                            assets.Add(AssetMeta.AssetType.SURFACE_GATEB_BRIDGE_LEFT);
                            break;
                        case MapModType.SURFACE_GATEB_BRIDGE_LEFT_BUNKER:
                            assets.Add(AssetMeta.AssetType.SURFACE_GATEB_BRIDGE_LEFT_BUNKER);
                            break;
                        default:
                            break;

                            // throw new ArgumentException($"Unknown {nameof(MapModType)} ({(MapModType)(1ul << i)})");
                    }
                }
            }

            return assets.ToArray();
        }

        private Asset LoadAsset(AssetMeta.AssetType assetType)
        {
            GameObject parent = new GameObject();
            (GameObject Obj, Asset asset) spawned;
            switch (assetType)
            {
                /*case AssetType.SURFACE_GATEB_BRIDGE_FORWARD:
                    parent.transform.position = new Vector3(0, 1000, 0);
                    if (!TrySpawnAssetAndGet(assetType.ToString(), parent.transform, out spawned))
                        this.Log.Warn($"Failed to spawn asset ({assetType}), is it present in AssetBoundle folder or in any boundle?");
                    return spawned;
                case AssetType.SURFACE_GATEB_BRIDGE_LEFT:
                    parent.transform.position = new Vector3(0, 1000, 0);
                    if (!TrySpawnAssetAndGet(assetType.ToString(), parent.transform, out spawned))
                        this.Log.Warn($"Failed to spawn asset ({assetType}), is it present in AssetBoundle folder or in any boundle?");
                    return spawned;
                case AssetType.SURFACE_GATEB_BRIDGE_LEFT_BUNKER:
                    parent.transform.position = new Vector3(0, 1000, 0);
                    if (!TrySpawnAssetAndGet(assetType.ToString(), parent.transform, out spawned))
                        this.Log.Warn($"Failed to spawn asset ({assetType}), is it present in AssetBoundle folder or in any boundle?");
                    return spawned;
                case AssetType.SURFACE_GATEB_BRIDGE_CONNECTOR:
                    parent.transform.position = new Vector3(0, 1000, 0);
                    if (!TrySpawnAssetAndGet(assetType.ToString(), parent.transform, out spawned))
                        this.Log.Warn($"Failed to spawn asset ({assetType}), is it present in AssetBoundle folder or in any boundle?");
                    return spawned;
                case AssetType.SURFACE_GATEA_STAIRS_LOCK:
                    parent.transform.position = new Vector3(0, 1000, 0);
                    if (!TrySpawnAssetAndGet(assetType.ToString(), parent.transform, out spawned))
                        this.Log.Warn($"Failed to spawn asset ({assetType}), is it present in AssetBoundle folder or in any boundle?");
                    return spawned;
                case AssetType.SURFACE_GATEA_TUNNEL_CI_DOOR:
                    parent.transform.position = new Vector3(0, 1000, 0);
                    if (!TrySpawnAssetAndGet(assetType.ToString(), parent.transform, out spawned))
                        this.Log.Warn($"Failed to spawn asset ({assetType}), is it present in AssetBoundle folder or in any boundle?");
                    return spawned;
                case AssetType.SURFACE_GATEA_TUNNEL_CI_DOOR_LOCKED:
                    parent.transform.position = new Vector3(0, 1000, 0);
                    if (!TrySpawnAssetAndGet(assetType.ToString(), parent.transform, out spawned))
                        this.Log.Warn($"Failed to spawn asset ({assetType}), is it present in AssetBoundle folder or in any boundle?");
                    return spawned;
                case AssetType.SURFACE_GATEA_TUNNEL_ELEVATOR_DOOR:
                    parent.transform.position = new Vector3(0, 1000, 0);
                    if (!TrySpawnAssetAndGet(assetType.ToString(), parent.transform, out spawned))
                        this.Log.Warn($"Failed to spawn asset ({assetType}), is it present in AssetBoundle folder or in any boundle?");
                    return spawned;
                case AssetType.SURFACE_GATEA_TOWER_ELEVATOR:
                    parent.transform.position = new Vector3(0, 1000, 0);
                    if (!TrySpawnAssetAndGet(assetType.ToString(), parent.transform, out spawned))
                        this.Log.Warn($"Failed to spawn asset ({assetType}), is it present in AssetBoundle folder or in any boundle?");
                    return spawned;
                case AssetType.SURFACE_GATEA_TOWER_SCP1499_CHAMBER:
                    parent.transform.position = new Vector3(0, 1000, 0);
                    if (!TrySpawnAssetAndGet(assetType.ToString(), parent.transform, out spawned))
                        this.Log.Warn($"Failed to spawn asset ({assetType}), is it present in AssetBoundle folder or in any boundle?");
                    return spawned;
                case AssetType.SURFACE_GATEA_TOWER_ARMORY:
                    parent.transform.position = new Vector3(0, 1000, 0);
                    if (!TrySpawnAssetAndGet(assetType.ToString(), parent.transform, out spawned))
                        this.Log.Warn($"Failed to spawn asset ({assetType}), is it present in AssetBoundle folder or in any boundle?");
                    return spawned;
                case AssetType.SURFACE_CICAR:
                    parent.transform.position = new Vector3(0, 1000, 0);
                    if (!TrySpawnAssetAndGet(assetType.ToString(), parent.transform, out spawned))
                        this.Log.Warn($"Failed to spawn asset ({assetType}), is it present in AssetBoundle folder or in any boundle?");
                    return spawned;
                case AssetType.SURFACE_GATEA_MIDDLE_TOWER:
                    parent.transform.position = new Vector3(0, 1000, 0);
                    if (!TrySpawnAssetAndGet(assetType.ToString(), parent.transform, out spawned))
                        this.Log.Warn($"Failed to spawn asset ({assetType}), is it present in AssetBoundle folder or in any boundle?");
                    return spawned;
                case AssetType.SURFACE_HELIPAD:
                    parent.transform.position = new Vector3(0, 1000, 0);
                    if (!TrySpawnAssetAndGet(assetType.ToString(), parent.transform, out spawned))
                        this.Log.Warn($"Failed to spawn asset ({assetType}), is it present in AssetBoundle folder or in any boundle?");
                    return spawned;
                case AssetType.SURFACE_GATEA_TOWER_ARMORY_BIG:
                    parent.transform.position = new Vector3(0, 1000, 0);
                    if (!TrySpawnAssetAndGet(assetType.ToString(), parent.transform, out spawned))
                        this.Log.Warn($"Failed to spawn asset ({assetType}), is it present in AssetBoundle folder or in any boundle?");
                    return spawned;
                case AssetType.SURFACE_HELICOPTER:
                    parent.transform.position = new Vector3(0, 1000, 0);
                    if (!TrySpawnAssetAndGet(assetType.ToString(), parent.transform, out spawned))
                        this.Log.Warn($"Failed to spawn asset ({assetType}), is it present in AssetBoundle folder or in any boundle?");
                    return spawned;*/
                case AssetMeta.AssetType.WARHEAD_TIMER:

                    // Surface Warhead
                    parent = new GameObject();
                    parent.transform.position = new Vector3(40.5f, 1000 - 7.4f, -47.481f);
                    parent.transform.rotation = Quaternion.identity;
                    if (!TryGetAndSpawnAsset(assetType, parent.transform, out spawned))
                        this.Log.Warn($"Failed to spawn asset ({assetType}), is it present in AssetBoundle folder or in any boundle?");
                    var script = spawned.Obj.GetComponent<UnityPrefabs.SegmentDisplay.MutliSegmentDisplayScript>();
                    script.Background.material.color = new Color(0, 0, 0, 0);
                    var toy = script.Background.GetComponentInChildren<AdminToys.PrimitiveObjectToy>();
                    if (toy != null)
                        toy.NetworkMaterialColor = script.Background.material.color;
                    spawned.Obj.transform.localPosition = Vector3.zero;
                    spawned.Obj.transform.localRotation = Quaternion.identity;

                    return spawned.asset;
                default:
                    string[] args = assetType.ToString().ToUpper().Split('_');
                    string name = args[0];
                    switch (name)
                    {
                        case "SURFACE":
                            parent.transform.position = new Vector3(0, 1000, 0);
                            if (!TryGetAndSpawnAsset(assetType, parent.transform, out spawned))
                                this.Log.Warn($"Failed to spawn asset ({assetType}), is it present in AssetBoundle folder or in any boundle?");
                            return spawned.asset;
                        default:
                            if (Enum.TryParse<RoomType>(name + args[1], true, out var roomType))
                            {
                                spawned = default;
                                foreach (var room in Map.Rooms.Where(x => x.Type == roomType))
                                {
                                    parent = new GameObject();
                                    parent.transform.position = room.Position;
                                    parent.transform.rotation = room.transform.rotation;
                                    if (!TryGetAndSpawnAsset(assetType, parent.transform, out spawned))
                                        this.Log.Warn($"Failed to spawn asset ({assetType}), is it present in AssetBoundle folder or in any boundle?");
                                }

                                return spawned.asset;
                            }

                            throw new ArgumentException($"Unknown {nameof(AssetMeta.AssetType)} ({assetType})");
                    }
            }
        }

        private void Player_InteractingDoor(Exiled.Events.EventArgs.InteractingDoorEventArgs ev)
        {
            if (!ev.IsAllowed)
                return;

            if (Asset.ConnectedAnimators.TryGetValue(ev.Door.Base, out var animator))
                animator.SetBool("IsOpen", !ev.Door.IsOpen);

            if (Asset.RemovePostUse.Contains(ev.Door.Base))
                this.CallDelayed(.25f, () => NetworkServer.Destroy(ev.Door.Base.gameObject));

            if (Asset.LockPostUse.Contains(ev.Door.Base))
            {
                ev.Door.Base.ServerChangeLock(Interactables.Interobjects.DoorUtils.DoorLockReason.SpecialDoorFeature, true);
                ev.IsAllowed = false;
            }
        }

        private void Server_WaitingForPlayers()
        {
            ReloadAssets();
            this.LoadAssets();

            /*Dictionary<AssetMeta.AssetType, Asset> spawnedAssets = new Dictionary<AssetMeta.AssetType, Asset>();
            foreach (var item in this.alwaysLoaded)
            {
                spawnedAssets[item] = this.LoadAsset(item);
            }

            foreach (var item in this.ParseMapMods(this.GenerateMapMods()))
                spawnedAssets[item] = this.LoadAsset(item);

            foreach (var assetsHandler in AssetsHandlers)
            {
                foreach (var spawnedAsset in spawnedAssets)
                {
                    if (assetsHandler.Key == spawnedAsset.Key)
                    {
                        foreach (var item in spawnedAsset.Value.SpawnedChildren)
                        {
                            var handler = (AssetHandlers.AssetHandler)Activator.CreateInstance(assetsHandler.Value);
                            handler.Initialize(item.Key, spawnedAsset.Value);
                        }

                        break;
                    }
                }
            }*/
        }

        private void LoadAssets()
        {
            var rooms = Map.Rooms.ToArray();
            rooms.ShuffleList();
            foreach (var room in rooms)
            {
                // Log.Debug($"Checking {room.Type}", true);
                HashSet<AssetMeta.AssetType> spawned = new HashSet<AssetMeta.AssetType>();
                foreach (var asset in Assets.Values)
                {
                    var meta = GameObject.Instantiate(asset.Prefab).GetComponent<AssetMeta>();

                    // Log.Debug($"Checking {asset.Meta.Type}", true);
                    foreach (var rule in asset.Meta.Rules)
                    {
                        // Log.Debug($"Checking rule", true);
                        if ((RoomType)rule.Room != room.Type)
                            continue;

                        // Log.Debug($"Found rule with matching room type", true);
                        if (!rule.Spawn)
                            continue;

                        // Log.Debug($"Should be spawned", true);
                        if (rule.MaxAmount <= asset.Spawned)
                            continue;

                        // Log.Debug($"Not spawned max", true);
                        // Log.Debug(asset.Meta.Type, true);
                        // Log.Debug(rule.MaxAmount, true);
                        // Log.Debug(asset.Spawned, true);
                        if (spawned.Any(x => rule.ColidingAssetTypes.Contains(x)))
                            continue;

                        // Log.Debug($"No Coliding found", true);
                        if (rule.MinAmount <= asset.Spawned)
                        {
                            if (rule.Chance < UnityEngine.Random.Range(0, 100))
                            {
                                // Log.Debug($"Random not good", true);
                                continue;
                            }

                            // else
                            // Log.Debug($"Randomised", true);
                        }

                        // else
                        // Log.Debug($"Filling min", true);
                        GameObject parent = new GameObject();
                        parent.transform.position = room.Position;
                        parent.transform.rotation = room.transform.rotation;

                        asset.Spawn(parent.transform);

                        Exiled.API.Features.Log.Debug($"Loaded {asset.Meta.Type}", true);

                        // Log.Debug($"Spawned", true);
                        spawned.Add(asset.Meta.Type);
                        asset.Spawned++;
                    }
                }
            }
        }
    }
}
