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

        internal static readonly Dictionary<AssetType, AssetHandlers.AssetHandler> AssetsHandlers = new Dictionary<AssetType, AssetHandlers.AssetHandler>();

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

        /// <summary>
        /// Tries to spawn asset.
        /// </summary>
        /// <param name="name">Asset name.</param>
        /// <param name="parent">Parent if there is.</param>
        /// <param name="spawnedAsset">Spawn asset.</param>
        /// <returns>If asset was spawned.</returns>
        public static bool TrySpawnAssetAndGet(string name, Transform parent, out (GameObject obj, Asset asset) result)
        {
            if (!Assets.TryGetValue(name.ToLower(), out var asset))
            {
                result = default;
                return false;
            }

            result = (asset.Spawn(parent), asset);

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
            Exiled.Events.Handlers.Player.ChangingItem += Player_ChangingItem;
        }
        private HelicopterScript helicopter;
        private void Player_ChangingItem(Exiled.Events.EventArgs.ChangingItemEventArgs ev)
        {
            if(ev.NewItem?.Type == ItemType.KeycardJanitor)
            {
                helicopter.Land();
            }
            else if (ev.NewItem?.Type == ItemType.KeycardScientist)
            {
                helicopter.TakeOff();
            }
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

            foreach (var boundle in boundles)
                boundle.Unload(false);

            return assets;
        }

        private static bool HasFlag(MapModType type, MapModType flag) => (type & flag) != 0;

        private readonly AssetType[] alwaysLoaded = new AssetType[]
        {
             AssetType.SURFACE_GATEA_TOWER_SCP1499_CHAMBER,
             AssetType.SURFACE_GATEA_TOWER_ELEVATOR,
             AssetType.SURFACE_CICAR,
             AssetType.SURFACE_HELIPAD,

             // AssetType.SURFACE_HELICOPTER,
             AssetType.SURFACE_GATEA_TOWER_ARMORY_BIG,

             AssetType.EZ_CURVE_ROOM,
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

        private AssetType[] ParseMapMods(MapModType mod)
        {
            ulong modUl = (ulong)mod;
            List<AssetType> assets = new List<AssetType>();
            Exiled.API.Features.Log.Debug(mod, true);
            for (int i = 0; i < 64; i++)
            {
                if (((modUl >> i) & 1) != 0)
                {
                    Exiled.API.Features.Log.Debug(i + "|" + (MapModType)(1ul << i), true);
                    switch ((MapModType)(1ul << i))
                    {
                        case MapModType.SURFACE_GATEA_MIDDLE_TOWER:
                            assets.Add(AssetType.SURFACE_GATEA_MIDDLE_TOWER);
                            break;
                        case MapModType.SURFACE_GATEA_STAIRS_LOCK:
                            assets.Add(AssetType.SURFACE_GATEA_STAIRS_LOCK);
                            break;
                        /*case MapModType.SURFACE_GATEA_TOWER_ARMORY:
                            assets.Add(AssetType.SURFACE_GATEA_TOWER_ARMORY);
                            break;*/
                        case MapModType.SURFACE_GATEA_TUNNEL_CI_DOOR:
                            assets.Add(AssetType.SURFACE_GATEA_TUNNEL_CI_DOOR);
                            break;
                        case MapModType.SURFACE_GATEA_TUNNEL_CI_DOOR_LOCKED:
                            assets.Add(AssetType.SURFACE_GATEA_TUNNEL_CI_DOOR_LOCKED);
                            break;
                        case MapModType.SURFACE_GATEA_TUNNEL_ELEVATOR_DOOR:
                            assets.Add(AssetType.SURFACE_GATEA_TUNNEL_ELEVATOR_DOOR);
                            break;
                        case MapModType.SURFACE_GATEB_BRIDGE_FORWARD:
                            assets.Add(AssetType.SURFACE_GATEB_BRIDGE_FORWARD);
                            break;
                        case MapModType.SURFACE_GATEB_BRIDGE_LEFT:
                            assets.Add(AssetType.SURFACE_GATEB_BRIDGE_LEFT);
                            break;
                        case MapModType.SURFACE_GATEB_BRIDGE_LEFT_BUNKER:
                            assets.Add(AssetType.SURFACE_GATEB_BRIDGE_LEFT_BUNKER);
                            break;
                        default:
                            break;

                            // throw new ArgumentException($"Unknown {nameof(MapModType)} ({(MapModType)(1ul << i)})");
                    }
                }
            }

            return assets.ToArray();
        }

        private (GameObject Obj, Asset asset) LoadAsset(AssetType assetType)
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
                default:
                    string[] args = assetType.ToString().ToUpper().Split('_');
                    string name = args[0];
                    switch (name)
                    {
                        case "SURFACE":
                            parent.transform.position = new Vector3(0, 1000, 0);
                            if (!TrySpawnAssetAndGet(assetType.ToString(), parent.transform, out spawned))
                                this.Log.Warn($"Failed to spawn asset ({assetType}), is it present in AssetBoundle folder or in any boundle?");
                            return spawned;
                        default:
                            if (Enum.TryParse<RoomType>(name + args[1], true, out var roomType))
                            {
                                spawned = default;
                                foreach (var room in Map.Rooms.Where(x => x.Type == roomType))
                                {
                                    parent = new GameObject();
                                    parent.transform.position = room.Position;
                                    parent.transform.rotation = room.transform.rotation;
                                    if (!TrySpawnAssetAndGet(assetType.ToString(), parent.transform, out spawned))
                                        this.Log.Warn($"Failed to spawn asset ({assetType}), is it present in AssetBoundle folder or in any boundle?");
                                }

                                return spawned;
                            }

                            throw new ArgumentException($"Unknown {nameof(AssetType)} ({assetType})");
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

            Dictionary<AssetType, (GameObject obj, Asset asset)> spawnedAssets = new Dictionary<AssetType, (GameObject obj, Asset asset)>();
            foreach (var item in this.alwaysLoaded)
            {
                spawnedAssets[item] = this.LoadAsset(item);
                if(item == AssetType.SURFACE_HELICOPTER)
                {
                    helicopter = spawnedAssets[item].obj.GetComponent<HelicopterScript>();
                    if (helicopter == null)
                        helicopter = spawnedAssets[item].obj.GetComponentInChildren<HelicopterScript>();
                }
            }

            foreach (var item in this.ParseMapMods(this.GenerateMapMods()))
                spawnedAssets[item] = this.LoadAsset(item);

            HashSet<AssetHandlers.AssetHandler> used = new HashSet<AssetHandlers.AssetHandler>();
            foreach (var assetsHandler in AssetsHandlers)
            {
                if (used.Contains(assetsHandler.Value))
                    continue;
                foreach (var spawnedAsset in spawnedAssets)
                {
                    if (assetsHandler.Key == spawnedAsset.Key)
                    {
                        assetsHandler.Value.Initialize(spawnedAssets);
                        used.Add(assetsHandler.Value);
                        break;
                    }
                }
            }
        }
    }
}
