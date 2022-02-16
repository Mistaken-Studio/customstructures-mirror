// -----------------------------------------------------------------------
// <copyright file="Asset.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using AdminToys;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Firearms.Ammo;
using InventorySystem.Items.Pickups;
using Mirror;
using Mistaken.API.Extensions;
using Mistaken.UnityPrefabs;
using UnityEngine;

// Code "borrowed" from
// https://github.com/Killers0992/AssetBundleLoader/blob/master/AssetBundleLoader/AssetBundlePrefab.cs
// but modified to allow for spawning items, doors, shooting targets
namespace Mistaken.CustomStructures
{
    /// <summary>
    /// Represents asset.
    /// </summary>
    public class Asset
    {
        /// <summary>
        /// Gets spawned objects bound to top asset.
        /// </summary>
        public Dictionary<GameObject, List<GameObject>> SpawnedChildren { get; } = new Dictionary<GameObject, List<GameObject>>();

        /// <summary>
        /// Gets objects bound to doors.
        /// </summary>
        public Dictionary<GameObject, DoorVariant> Doors { get; } = new Dictionary<GameObject, DoorVariant>();

        /// <summary>
        /// Gets or sets asset meta.
        /// </summary>
        public AssetMeta Meta { get; set; }

        /// <summary>
        /// Gets or sets number of spawned this round.
        /// </summary>
        public int Spawned { get; set; } = 0;

        /// <summary>
        /// Gets or sets asset prefab.
        /// </summary>
        public GameObject Prefab { get; set; }

        /// <summary>
        /// Spawns asset.
        /// </summary>
        /// <param name="position">Position.</param>
        /// <param name="eulerAngles">Rotation as eulerAngles.</param>
        /// <param name="scale">Scale.</param>
        /// <returns>Spawned asset.</returns>
        public GameObject Spawn(Vector3 position, Vector3 eulerAngles, Vector3 scale)
        {
            var tor = new GameObject();
            tor.transform.position = position;
            tor.transform.eulerAngles = eulerAngles;
            tor.transform.localScale = scale;
            this.Spawn(tor.transform);
            return tor;
        }

        /// <summary>
        /// Spawns asset.
        /// </summary>
        /// <param name="parent">Parent.</param>
        /// <returns>Spawned asset.</returns>
        public GameObject Spawn(Transform parent)
        {
            var prefabObject = UnityEngine.Object.Instantiate(this.Prefab, parent);
            prefabObject.hideFlags = HideFlags.HideAndDontSave;
            this.SpawnedChildren[prefabObject] = new List<GameObject>();

            foreach (var transform in prefabObject.GetComponentsInChildren<Transform>())
            {
                if (!transform.gameObject.activeInHierarchy)
                    continue;
                if (
                    transform.name.StartsWith("SPAWN_", StringComparison.InvariantCultureIgnoreCase) ||
                    transform.name.StartsWith("HCZ_DOOR", StringComparison.InvariantCultureIgnoreCase) ||
                    transform.name.StartsWith("EZ_DOOR", StringComparison.InvariantCultureIgnoreCase) ||
                    transform.name.StartsWith("LCZ_DOOR", StringComparison.InvariantCultureIgnoreCase) ||
                    transform.name.StartsWith("BINARY_TARGET", StringComparison.InvariantCultureIgnoreCase) ||
                    transform.name.StartsWith("DBOY_TARGET", StringComparison.InvariantCultureIgnoreCase) ||
                    transform.name.StartsWith("SPORT_TARGET", StringComparison.InvariantCultureIgnoreCase))
                {
                    foreach (var item in transform.GetComponentsInChildren<Transform>())
                    {
                        if (item == transform)
                            continue;
                        item.gameObject.SetActive(false);

                        // Exiled.API.Features.Log.Debug($"Disabled {item.gameObject.name} because of {transform.name}", true);
                    }
                }
            }

            foreach (var transform in prefabObject.GetComponentsInChildren<Transform>())
            {
                if (!transform.gameObject.activeInHierarchy)
                    continue;
                if (transform.TryGetComponent<Light>(out Light light))
                {
                    var toy = CreateLight(
                        transform,
                        light.color,
                        light.intensity,
                        light.range,
                        light.shadows != LightShadows.None);
                    transform.gameObject.AddComponent<LightSyncronizerScript>().Toy = toy;
                    this.SpawnedChildren[prefabObject].Add(toy.gameObject);
                }

                if (!transform.TryGetComponent<MeshFilter>(out MeshFilter filter))
                {
                    string name = transform.name;
                    string[] nameArgs = name.Split(' ');
                    var tor = CreateEmpty(transform);
                    if (transform.TryGetComponent(out UnityPrefabs.Door doorScript))
                    {
                        DoorVariant door = null;

                        switch (nameArgs[0])
                        {
                            case "HCZ_DOOR":
                                transform.gameObject.SetActive(false);
                                Exiled.API.Features.Log.Debug($"Spawning HCZ Door", PluginHandler.Instance.Config.VerbouseOutput);
                                door = DoorUtils.SpawnDoor(DoorUtils.DoorType.HCZ_BREAKABLE, tor.transform.position, tor.transform.eulerAngles, tor.transform.lossyScale);

                                this.Doors[transform.gameObject] = door;
                                this.SpawnedChildren[prefabObject].Add(door.gameObject);
                                break;

                            case "EZ_DOOR":
                                transform.gameObject.SetActive(false);
                                Exiled.API.Features.Log.Debug($"Spawning EZ Door", PluginHandler.Instance.Config.VerbouseOutput);
                                door = DoorUtils.SpawnDoor(DoorUtils.DoorType.EZ_BREAKABLE, tor.transform.position, tor.transform.eulerAngles, tor.transform.lossyScale);

                                this.Doors[transform.gameObject] = door;
                                this.SpawnedChildren[prefabObject].Add(door.gameObject);
                                break;

                            case "LCZ_DOOR":
                                transform.gameObject.SetActive(false);
                                Exiled.API.Features.Log.Debug($"Spawning LCZ Door", PluginHandler.Instance.Config.VerbouseOutput);
                                door = DoorUtils.SpawnDoor(DoorUtils.DoorType.LCZ_BREAKABLE, tor.transform.position, tor.transform.eulerAngles, tor.transform.lossyScale);

                                this.Doors[transform.gameObject] = door;
                                this.SpawnedChildren[prefabObject].Add(door.gameObject);
                                break;

                            default:
                                continue;
                        }

                        if (door != null)
                        {
                            if (doorScript.Locked)
                                door.ServerChangeLock(DoorLockReason.AdminCommand, true);
                            if (!doorScript.Breakable)
                                (door as BreakableDoor)._brokenPrefab = null;

                            if (doorScript.DefaultState)
                                door.NetworkTargetState = true;

                            if (doorScript.Permissions != UnityPrefabs.Door.KeycardPermissions.None)
                                door.RequiredPermissions.RequiredPermissions = (Interactables.Interobjects.DoorUtils.KeycardPermissions)doorScript.Permissions;

                            if (doorScript.LockAfterUse)
                                LockPostUse.Add(door);

                            if (doorScript.DestroyAfterUse)
                                RemovePostUse.Add(door);

                            var animatorTriggerScript = transform.gameObject.GetComponent<UnityPrefabs.AnimatorTrigger>();
                            if (animatorTriggerScript != null)
                                ConnectedDoorAnimators[door] = animatorTriggerScript;

                            var scriptTriggerScript = transform.gameObject.GetComponent<ScriptTrigger>();
                            if (scriptTriggerScript != null)
                                ConnectedDoorScriptTriggers[door] = scriptTriggerScript;
                        }
                    }
                    else if (transform.TryGetComponent<UnityPrefabs.Item>(out UnityPrefabs.Item itemScript))
                    {
                        string[] args = name.Split('_');

                        transform.gameObject.SetActive(false);
                        ItemPickupBase spawned;
                        var itemType = (ItemType)itemScript.Type;
                        switch (itemType)
                        {
                            case ItemType.None:
                                {
                                    Log.Error($"How am I supposed to spawn None item \"{name}\"");
                                    continue;
                                }

                            case ItemType.Ammo12gauge:
                            case ItemType.Ammo44cal:
                            case ItemType.Ammo556x45:
                            case ItemType.Ammo762x39:
                            case ItemType.Ammo9x19:
                                {
                                    var item = new Ammo(itemType);
                                    item.Scale = tor.transform.lossyScale;
                                    spawned = item.Spawn(tor.transform.position, tor.transform.rotation).Base;
                                    (spawned as AmmoPickup).NetworkSavedAmmo = itemScript.Ammo;
                                    spawned.Rb.useGravity = false;
                                    spawned.Rb.isKinematic = true;
                                    break;
                                }

                            case ItemType.GunFSP9:
                            case ItemType.GunRevolver:
                            case ItemType.GunCrossvec:
                            case ItemType.GunE11SR:
                            case ItemType.GunLogicer:
                            case ItemType.GunAK:
                            case ItemType.GunShotgun:
                                {
                                    var item = new Exiled.API.Features.Items.Firearm(itemType);
                                    item.Scale = tor.transform.lossyScale;
                                    spawned = item.Spawn(tor.transform.position, tor.transform.rotation).Base;
                                    spawned.Rb.useGravity = false;
                                    spawned.Rb.isKinematic = true;
                                    break;
                                }

                            default:
                                {
                                    var item = new Exiled.API.Features.Items.Item(itemType);
                                    item.Scale = tor.transform.lossyScale;
                                    spawned = item.Spawn(tor.transform.position, tor.transform.rotation).Base;
                                    spawned.Rb.useGravity = false;
                                    spawned.Rb.isKinematic = true;
                                    break;
                                }
                        }

                        if (itemScript.Locked)
                        {
                            spawned.NetworkInfo = new PickupSyncInfo
                            {
                                Locked = true,
                                InUse = false,
                                ItemId = spawned.Info.ItemId,
                                Position = spawned.Info.Position,
                                Rotation = spawned.Info.Rotation,
                                Serial = spawned.Info.Serial,
                                Weight = spawned.Info.Weight,
                            };
                        }

                        if (itemScript.HasRb)
                        {
                            spawned.Rb.useGravity = transform;
                            spawned.Rb.isKinematic = false;
                        }

                        if (itemScript.DestroyAfterUse)
                            RemovePostUseItem.Add(spawned);

                        var animatorTriggerScript = transform.gameObject.GetComponent<UnityPrefabs.AnimatorTrigger>();
                        if (animatorTriggerScript != null)
                            ConnectedItemAnimators[spawned] = animatorTriggerScript;

                        var scriptTriggerScript = transform.gameObject.GetComponent<ScriptTrigger>();
                        if (scriptTriggerScript != null)
                            ConnectedItemScriptTriggers[spawned] = scriptTriggerScript;
                    }

                    switch (nameArgs[0])
                    {
                        case "BINARY_TARGET":
                            {
                                transform.gameObject.SetActive(false);
                                Exiled.API.Features.Log.Debug($"Spawning BINARY_TARGET", PluginHandler.Instance.Config.VerbouseOutput);
                                var target = SpawnShootingTarget(ShootingTargetType.Binary, tor.transform.position, tor.transform.rotation, tor.transform.lossyScale);

                                this.SpawnedChildren[prefabObject].Add(target.gameObject);
                                break;
                            }

                        case "DBOY_TARGET":
                            {
                                transform.gameObject.SetActive(false);
                                Exiled.API.Features.Log.Debug($"Spawning DBOY_TARGET", PluginHandler.Instance.Config.VerbouseOutput);
                                var target = SpawnShootingTarget(ShootingTargetType.ClassD, tor.transform.position, tor.transform.rotation, tor.transform.lossyScale);

                                this.SpawnedChildren[prefabObject].Add(target.gameObject);
                                break;
                            }

                        case "SPORT_TARGET":
                            {
                                transform.gameObject.SetActive(false);
                                Exiled.API.Features.Log.Debug($"Spawning SPORT_TARGET", PluginHandler.Instance.Config.VerbouseOutput);
                                var target = SpawnShootingTarget(ShootingTargetType.Sport, tor.transform.position, tor.transform.rotation, tor.transform.lossyScale);

                                this.SpawnedChildren[prefabObject].Add(target.gameObject);
                                break;
                            }

                        case "WORK_STATION":
                            transform.gameObject.SetActive(false);
                            Exiled.API.Features.Log.Debug($"Spawning WORK_STATION", PluginHandler.Instance.Config.VerbouseOutput);
                            var workStation = GameObject.Instantiate(CustomNetworkManager.singleton.spawnPrefabs.First(x => x.name == "Work Station"));
                            workStation.transform.position = tor.transform.position;
                            workStation.transform.rotation = tor.transform.rotation;
                            workStation.transform.localScale = tor.transform.localScale;
                            NetworkServer.Spawn(workStation);
                            this.SpawnedChildren[prefabObject].Add(workStation);
                            break;

                        default:
                            break;
                    }

                    this.SpawnedChildren[prefabObject].Add(tor);
                    continue;
                }

                if (!transform.TryGetComponent<MeshRenderer>(out MeshRenderer renderer))
                    continue;

                PrimitiveType type = PrimitiveType.Sphere;
                bool hasCollision = transform.TryGetComponent<Collider>(out _);

                switch (filter.mesh.name)
                {
                    case "Plane Instance":
                        type = PrimitiveType.Plane;
                        break;
                    case "Cylinder Instance":
                        type = PrimitiveType.Cylinder;
                        break;
                    case "Cube Instance":
                        type = PrimitiveType.Cube;
                        break;
                    case "Capsule Instance":
                        type = PrimitiveType.Capsule;
                        break;
                    case "Quad Instance":
                        type = PrimitiveType.Quad;
                        break;
                    case "Sphere Instance":
                        type = PrimitiveType.Sphere;
                        break;
                    default:
                        continue;
                }

                this.SpawnedChildren[prefabObject].Add(CreatePrimitive(
                    transform,
                    type,
                    renderer.material.color,
                    hasCollision).gameObject);
            }

            return prefabObject;
        }

        internal static readonly Dictionary<DoorVariant, AnimatorTrigger> ConnectedDoorAnimators = new Dictionary<DoorVariant, AnimatorTrigger>();
        internal static readonly Dictionary<ItemPickupBase, AnimatorTrigger> ConnectedItemAnimators = new Dictionary<ItemPickupBase, AnimatorTrigger>();

        internal static readonly Dictionary<DoorVariant, ScriptTrigger> ConnectedDoorScriptTriggers = new Dictionary<DoorVariant, ScriptTrigger>();
        internal static readonly Dictionary<ItemPickupBase, ScriptTrigger> ConnectedItemScriptTriggers = new Dictionary<ItemPickupBase, ScriptTrigger>();

        internal static readonly HashSet<DoorVariant> RemovePostUse = new HashSet<DoorVariant>();
        internal static readonly HashSet<ItemPickupBase> RemovePostUseItem = new HashSet<ItemPickupBase>();
        internal static readonly HashSet<DoorVariant> LockPostUse = new HashSet<DoorVariant>();
        internal static readonly HashSet<Transform> HighUpdateRate = new HashSet<Transform>();

        private static AdminToys.ShootingTarget shootingTargetObject_binary = null;
        private static AdminToys.ShootingTarget shootingTargetObject_sport = null;
        private static AdminToys.ShootingTarget shootingTargetObject_dboy = null;

        private static AdminToys.ShootingTarget ShootingTargetObjectBinary
        {
            get
            {
                if (shootingTargetObject_binary == null)
                {
                    foreach (var gameObject in NetworkClient.prefabs.Values)
                    {
                        if (gameObject.TryGetComponent<AdminToys.ShootingTarget>(out var component) && component.name == "binaryTargetPrefab")
                            shootingTargetObject_binary = component;
                    }
                }

                return shootingTargetObject_binary;
            }
        }

        private static AdminToys.ShootingTarget ShootingTargetObjectSport
        {
            get
            {
                if (shootingTargetObject_sport == null)
                {
                    foreach (var gameObject in NetworkClient.prefabs.Values)
                    {
                        if (gameObject.TryGetComponent<AdminToys.ShootingTarget>(out var component) && component.name == "sportTargetPrefab")
                            shootingTargetObject_sport = component;
                    }
                }

                return shootingTargetObject_sport;
            }
        }

        private static AdminToys.ShootingTarget ShootingTargetObjectDBoy
        {
            get
            {
                if (shootingTargetObject_dboy == null)
                {
                    foreach (var gameObject in NetworkClient.prefabs.Values)
                    {
                        if (gameObject.TryGetComponent<AdminToys.ShootingTarget>(out var component) && component.name == "dboyTargetPrefab")
                            shootingTargetObject_dboy = component;
                    }
                }

                return shootingTargetObject_dboy;
            }
        }

        private static PrimitiveObjectToy CreatePrimitive(Transform parent, PrimitiveType type, Color color, bool hasCollision)
        {
            bool sync = false;
            bool colorSync = false;
            var meta = parent.GetComponentInParent<AssetMeta>();
            if (meta != null)
            {
                var tmpParent = parent;
                while (tmpParent != null)
                {
                    if (meta.MovableObjects.Contains(tmpParent.gameObject))
                    {
                        sync = true;
                        break;
                    }

                    if (meta.ColorChangableObjects.Contains(tmpParent.gameObject))
                    {
                        colorSync = true;
                        break;
                    }

                    tmpParent = tmpParent.parent;
                }
            }

            var toy = API.MapPlus.SpawnPrimitive(type, parent, color, hasCollision, sync, sync ? (byte?)60 : null);

            if (colorSync)
                toy.gameObject.AddComponent<ColorSynchronizerScript>();

            return toy;
        }

        private static LightSourceToy CreateLight(Transform parent, Color color, float intensity, float range, bool shadows)
        {
            bool sync = false;
            var meta = parent.GetComponentInParent<AssetMeta>();
            if (meta != null)
            {
                var tmpParent = parent;
                while (tmpParent != null)
                {
                    if (meta.MovableObjects.Contains(tmpParent.gameObject))
                    {
                        sync = true;
                        break;
                    }

                    tmpParent = tmpParent.parent;
                }
            }

            return API.MapPlus.SpawnLight(parent, color, intensity, range, shadows, sync, sync ? (byte?)60 : null);
        }

        private static GameObject CreateEmpty(Transform parent)
        {
            GameObject tor = new GameObject();
            tor.transform.parent = parent;
            tor.transform.localPosition = Vector3.zero;
            tor.transform.localRotation = Quaternion.identity;
            tor.transform.localScale = Vector3.one;
            return tor;
        }

        private static AdminToys.ShootingTarget SpawnShootingTarget(ShootingTargetType type, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            AdminToyBase prefab;
            switch (type)
            {
                case ShootingTargetType.Binary:
                    prefab = ShootingTargetObjectBinary;
                    break;
                case ShootingTargetType.Sport:
                    prefab = ShootingTargetObjectSport;
                    break;
                case ShootingTargetType.ClassD:
                    prefab = ShootingTargetObjectDBoy;
                    break;
                default:
                    return null;
            }

            AdminToyBase toy = UnityEngine.Object.Instantiate(prefab);
            AdminToys.ShootingTarget ptoy = toy.GetComponent<AdminToys.ShootingTarget>();
            ptoy.transform.position = position;
            ptoy.transform.rotation = rotation;
            ptoy.transform.localScale = scale;
            ptoy.NetworkScale = ptoy.transform.lossyScale;
            NetworkServer.Spawn(toy.gameObject);

            ptoy.UpdatePositionServer();

            return ptoy;
        }
    }
}
