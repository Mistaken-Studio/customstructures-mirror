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
using MEC;
using Mirror;
using Mistaken.API.Diagnostics;
using Mistaken.CustomStructures.AssetHandlers;
using Mistaken.UnityPrefabs;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mistaken.CustomStructures
{
    /// <inheritdoc/>
    public class CustomStructuresHandler : Module
    {
        /// <summary>
        /// Assets bound to their type.
        /// </summary>
        public static readonly Dictionary<AssetMeta.AssetType, Asset> Assets = new Dictionary<AssetMeta.AssetType, Asset>();

        /// <summary>
        /// Unidentified Assets.
        /// </summary>
        public static readonly List<Asset> UnknownAssets = new List<Asset>();

        /// <summary>
        /// Reloads <see cref="Assets"/> and <see cref="UnknownAssets"/>.
        /// </summary>
        public static void ReloadAssets()
        {
            Assets.Clear();
            UnknownAssets.Clear();
            foreach (var asset in GetAssets())
            {
                var meta = asset.GetComponent<AssetMeta>();
                if (meta == null)
                {
                    Exiled.API.Features.Log.Debug($"Meta Script for {asset.name} not found", PluginHandler.Instance.Config.VerbouseOutput);
                    continue;
                }

                Exiled.API.Features.Log.Debug($"Meta Script for {asset.name} found", PluginHandler.Instance.Config.VerbouseOutput);

                if (meta.Type == AssetMeta.AssetType.UNKNOWN)
                {
                    UnknownAssets.Add(new Asset
                    {
                        Prefab = asset,
                        Meta = meta,
                    });
                }
                else
                {
                    Assets[meta.Type] = new Asset
                    {
                        Prefab = asset,
                        Meta = meta,
                    };
                }
            }
        }

        /// <summary>
        /// Spawns asset.
        /// </summary>
        /// <param name="type">Asset Type.</param>
        /// <param name="parent">Parent if there is.</param>
        /// <returns>Spawn asset.</returns>
        /// <exception cref="ArgumentException">If asset with <paramref name="type"/> was not found.</exception>
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

            Exiled.API.Features.Log.Debug($"Loaded {type}", PluginHandler.Instance.Config.VerbouseOutput);

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

            Exiled.API.Features.Log.Debug($"Loaded {type}", PluginHandler.Instance.Config.VerbouseOutput);

            return true;
        }

        /// <summary>
        /// Tries to get asset.
        /// </summary>
        /// <param name="name">Asset Name.</param>
        /// <param name="asset">Asset.</param>
        /// <returns>If asset was returned.</returns>
        public static bool TryGetAsset(string name, out Asset asset)
        {
            asset = UnknownAssets.SingleOrDefault(x => x.Prefab.name == name);
            return !(asset is null);
        }

        /// <inheritdoc cref="Module"/>
        public CustomStructuresHandler(IPlugin<IConfig> plugin)
            : base(plugin)
        {
        }

        /// <inheritdoc/>
        public override string Name => "CustomStructuresHandler";

        /// <inheritdoc/>
        public override void OnEnable()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers += this.Server_WaitingForPlayers;
            Exiled.Events.Handlers.Player.InteractingDoor += this.Player_InteractingDoor;
            Events.Handlers.CustomEvents.RequestPickItem += this.CustomEvents_RequestPickItem;

            ReloadAssets();
        }

        /// <inheritdoc/>
        public override void OnDisable()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers -= this.Server_WaitingForPlayers;
            Exiled.Events.Handlers.Player.InteractingDoor -= this.Player_InteractingDoor;
            Events.Handlers.CustomEvents.RequestPickItem -= this.CustomEvents_RequestPickItem;

            foreach (var item in Assets.SelectMany(asset => asset.Value.SpawnedChildren))
            {
                foreach (var item2 in item.Value)
                    Object.Destroy(item2);
                Object.Destroy(item.Key);
            }

            foreach (var item in UnknownAssets.SelectMany(asset => asset.SpawnedChildren))
            {
                foreach (var item2 in item.Value)
                    Object.Destroy(item2);
                Object.Destroy(item.Key);
            }

            Assets.Clear();
        }

        internal static readonly Dictionary<AssetMeta.AssetType, Type> AssetsHandlers = new Dictionary<AssetMeta.AssetType, Type>();

        private static IEnumerable<AssetBundle> LoadBundles(string files)
        {
            List<AssetBundle> tor = Directory.GetFiles(files)
                .Where(x => !x.EndsWith(".manifest"))
                .Select(AssetBundle.LoadFromFile)
                .ToList();

            foreach (var item in Directory.GetDirectories(files))
                tor.AddRange(LoadBundles(item));

            return tor;
        }

        private static IEnumerable<GameObject> GetAssets()
        {
            if (!Directory.Exists(Path.Combine(Paths.Plugins, "AssetBoundle")))
            {
                Exiled.API.Features.Log.Warn($"{Path.Combine(Paths.Plugins, "AssetBoundle")} was not found, creating ...");
                Directory.CreateDirectory(Path.Combine(Paths.Plugins, "AssetBoundle"));
                return Array.Empty<GameObject>();
            }

            var bundles = LoadBundles(Path.Combine(Paths.Plugins, "AssetBoundle")).ToArray();
            List<GameObject> assets = new List<GameObject>();

            foreach (var bundle in bundles)
                assets.AddRange(bundle.LoadAllAssets<GameObject>());

            foreach (var bundle in bundles)
                bundle.Unload(false);
            return assets;
        }

        private readonly Dictionary<AssetMeta.AssetType, List<AssetHandler>> assetHandlers = new Dictionary<AssetMeta.AssetType, List<AssetHandler>>();

        private void Player_InteractingDoor(Exiled.Events.EventArgs.InteractingDoorEventArgs ev)
        {
            if (!ev.IsAllowed)
                return;

            if (Asset.ConnectedDoorAnimators.TryGetValue(ev.Door.Base, out var animator))
                animator.Animator.SetBool(animator.Name, animator.Toggle ? !ev.Door.IsOpen : animator.Value);

            if (Asset.ConnectedDoorScriptTriggers.TryGetValue(ev.Door.Base, out var trigger))
            {
                foreach (var item in this.assetHandlers.Values.SelectMany(x => x))
                    item.OnScriptTrigger(trigger.Name);
                ev.IsAllowed = false;
            }

            if (Asset.RemovePostUse.Contains(ev.Door.Base))
                this.CallDelayed(.25f, () => NetworkServer.Destroy(ev.Door.Base.gameObject));

            if (Asset.LockPostUse.Contains(ev.Door.Base))
            {
                ev.Door.Base.ServerChangeLock(Interactables.Interobjects.DoorUtils.DoorLockReason.SpecialDoorFeature, true);
                ev.IsAllowed = false;
            }
        }

        private void CustomEvents_RequestPickItem(Events.EventArgs.PickItemRequestEventArgs ev)
        {
            if (!ev.IsAllowed)
                return;

            if (Asset.ConnectedItemAnimators.TryGetValue(ev.Pickup.Base, out var animator))
            {
                ev.IsAllowed = false;
                animator.Animator.SetBool(animator.Name, animator.Toggle ? !animator.Animator.GetBool(animator.Name) : animator.Value);
            }

            if (Asset.ConnectedItemScriptTriggers.TryGetValue(ev.Pickup.Base, out var trigger))
            {
                ev.IsAllowed = false;
                foreach (var item in this.assetHandlers.Values.SelectMany(x => x))
                    item.OnScriptTrigger(trigger.Name);
            }

            if (Asset.RemovePostUseItem.Contains(ev.Pickup.Base))
            {
                this.CallDelayed(.25f, () => ev.Pickup.Destroy());
                ev.IsAllowed = false;
            }
        }

        private void Server_WaitingForPlayers()
        {
            Asset.ConnectedDoorAnimators.Clear();
            Asset.ConnectedDoorScriptTriggers.Clear();
            Asset.ConnectedItemAnimators.Clear();
            Asset.ConnectedItemScriptTriggers.Clear();

            ReloadAssets();
            this.RunCoroutine(this.LoadAssets());
        }

        private IEnumerator<float> LoadAssets()
        {
            var rooms = Room.List.ToArray();
            rooms.ShuffleList();
            foreach (var room in rooms)
            {
                HashSet<AssetMeta.AssetType> spawned = new HashSet<AssetMeta.AssetType>();
                foreach (var asset in Assets.Values)
                {
                    // ReSharper disable once UnusedVariable
                    var meta = Object.Instantiate(asset.Prefab).GetComponent<AssetMeta>();

                    foreach (var rule in asset.Meta.Rules)
                    {
                        if ((RoomType)rule.Room != room.Type)
                            continue;

                        if (!rule.Spawn)
                            continue;

                        if (rule.MaxAmount <= asset.Spawned)
                            continue;

                        if (spawned.Any(x => rule.ColidingAssetTypes.Contains(x)))
                            continue;

                        if (rule.MinAmount <= asset.Spawned)
                        {
                            if (rule.Chance < UnityEngine.Random.Range(0, 100))
                                continue;
                        }

                        var parent = new GameObject
                        {
                            transform =
                            {
                                parent = room.transform,
                                localPosition = Vector3.zero,
                                localRotation = Quaternion.identity,
                                localScale = Vector3.one,
                            },
                        };

                        var instance = asset.Spawn(parent.transform);

                        foreach (var assetsHandler in AssetsHandlers)
                        {
                            if (assetsHandler.Key != asset.Meta.Type)
                                continue;

                            var handler = (AssetHandler)instance.AddComponent(assetsHandler.Value);

                            this.CallDelayed(10, () =>
                            {
                                handler.Initialize(asset);
                            });

                            if (!this.assetHandlers.ContainsKey(assetsHandler.Key))
                                this.assetHandlers.Add(assetsHandler.Key, new List<AssetHandler>());
                            this.assetHandlers[assetsHandler.Key].Add(handler);
                            break;
                        }

                        Exiled.API.Features.Log.Debug($"Loaded {asset.Meta.Type}", PluginHandler.Instance.Config.VerbouseOutput);

                        spawned.Add(asset.Meta.Type);
                        asset.Spawned++;
                        yield return Timing.WaitForOneFrame;
                    }
                }

                foreach (var asset in UnknownAssets)
                {
                    // ReSharper disable once UnusedVariable
                    var meta = Object.Instantiate(asset.Prefab).GetComponent<AssetMeta>();

                    foreach (var rule in asset.Meta.Rules)
                    {
                        if ((RoomType)rule.Room != room.Type)
                            continue;

                        if (!rule.Spawn)
                            continue;

                        if (rule.MaxAmount <= asset.Spawned)
                            continue;

                        if (spawned.Any(x => rule.ColidingAssetTypes.Contains(x)))
                            continue;

                        if (rule.MinAmount <= asset.Spawned)
                        {
                            if (rule.Chance < UnityEngine.Random.Range(0, 100))
                                continue;
                        }

                        var parent = new GameObject
                        {
                            transform =
                            {
                                parent = room.transform,
                                position = room.Position,
                                rotation = room.transform.rotation,
                            },
                        };

                        // ReSharper disable once UnusedVariable
                        var instance = asset.Spawn(parent.transform);

                        Exiled.API.Features.Log.Debug($"Loaded {asset.Meta.name}", PluginHandler.Instance.Config.VerbouseOutput);

                        asset.Spawned++;
                        yield return Timing.WaitForOneFrame;
                    }
                }
            }
        }
    }
}
