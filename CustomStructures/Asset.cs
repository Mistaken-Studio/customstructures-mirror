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
using Exiled.API.Features.Items;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Firearms.Ammo;
using Mirror;
using Mistaken.API.Extensions;
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
        /// Spawned objects bound to top asset.
        /// </summary>
        public Dictionary<GameObject, List<GameObject>> SpawnedChildren { get; } = new Dictionary<GameObject, List<GameObject>>();

        /// <summary>
        /// Asset name.
        /// </summary>
        public string AssetName { get; set; }

        /// <summary>
        /// Asset prefab
        /// </summary>
        public GameObject Prefab { get; set; }

        /// <summary>
        /// Spawns asset
        /// </summary>
        /// <param name="position">Position</param>
        /// <param name="eulerAngles">Rotation as eulerAngles</param>
        /// <param name="scale">Scale</param>
        /// <returns>Spawned asset</returns>
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
        /// Spawns asset
        /// </summary>
        /// <param name="parent">Parent</param>
        /// <returns>Spawned asset</returns>
        public GameObject Spawn(Transform parent)
        {
            var prefabObject = UnityEngine.Object.Instantiate(this.Prefab, parent);
            prefabObject.hideFlags = HideFlags.HideAndDontSave;
            this.SpawnedChildren[prefabObject] = new List<GameObject>();

            foreach (var item in prefabObject.GetComponentsInChildren<Animator>())
            {
                foreach (var item2 in item.GetComponentsInChildren<Transform>())
                    HighUpdateRate.Add(item2);
            }

            foreach (var transform in prefabObject.GetComponentsInChildren<Transform>())
            {
                if (!transform.gameObject.activeSelf)
                    continue;
                if (
                    transform.name.StartsWith("SPAWN_", StringComparison.InvariantCultureIgnoreCase) ||
                    transform.name.StartsWith("HCZ_DOOR", StringComparison.InvariantCultureIgnoreCase) ||
                    transform.name.StartsWith("EZ_DOOR", StringComparison.InvariantCultureIgnoreCase) ||
                    transform.name.StartsWith("LCZ_DOOR", StringComparison.InvariantCultureIgnoreCase) ||
                    transform.name.StartsWith("TARGET_DBOY", StringComparison.InvariantCultureIgnoreCase))
                {
                    foreach (var item in transform.GetComponentsInChildren<Transform>())
                    {
                        if (item == transform)
                            continue;
                        item.gameObject.SetActive(false);
                        Exiled.API.Features.Log.Debug($"Disabled {item.gameObject.name} because of {transform.name}", true);
                    }
                }
            }

            foreach (var transform in prefabObject.GetComponentsInChildren<Transform>())
            {
                if (!transform.gameObject.activeSelf)
                    continue;
                if (transform.TryGetComponent<Light>(out Light light))
                {
                    this.SpawnedChildren[prefabObject].Add(CreateLight(
                        transform,
                        light.color,
                        light.intensity,
                        light.range,
                        light.shadows != LightShadows.None).gameObject);
                }

                if (!transform.TryGetComponent<MeshFilter>(out MeshFilter filter))
                {
                    var tor = CreateEmpty(transform);
                    string name = transform.name;
                    if (name.StartsWith("SPAWN_", StringComparison.InvariantCultureIgnoreCase))
                    {
                        string[] args = name.Split('_');
                        Exiled.API.Features.Log.Debug($"Spawning Item", true);
                        var itemType = (ItemType)Enum.Parse(typeof(ItemType), args[1].Split('(')[0].Trim(), true);
                        transform.gameObject.SetActive(false);
                        switch (itemType)
                        {
                            case ItemType.Ammo12gauge:
                            case ItemType.Ammo44cal:
                            case ItemType.Ammo556x45:
                            case ItemType.Ammo762x39:
                            case ItemType.Ammo9x19:
                                {
                                    var item = new Ammo(itemType);
                                    item.Scale = tor.transform.localScale;
                                    (item.Spawn(tor.transform.position, tor.transform.rotation).Base as AmmoPickup).NetworkSavedAmmo = ushort.Parse(args[2]);
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
                                    item.Scale = tor.transform.localScale;
                                    item.Spawn(tor.transform.position, tor.transform.rotation);
                                    break;
                                }

                            default:
                                {
                                    var item = new Item(itemType);
                                    item.Scale = tor.transform.localScale;
                                    item.Spawn(tor.transform.position, tor.transform.rotation);
                                    break;
                                }
                        }
                    }
                    else
                    {
                        DoorVariant door = null;
                        string[] nameArgs = name.Split(' ');
                        Exiled.API.Features.Log.Debug($"Spawning GameObject ({nameArgs[0]})", true);
                        switch (nameArgs[0])
                        {
                            case "HCZ_DOOR":
                                transform.gameObject.SetActive(false);
                                Exiled.API.Features.Log.Debug($"Spawning HCZ Door", true);
                                door = DoorUtils.SpawnDoor(DoorUtils.DoorType.HCZ_BREAKABLE, tor.transform.position, tor.transform.eulerAngles, tor.transform.lossyScale);

                                this.SpawnedChildren[prefabObject].Add(door.gameObject);
                                break;

                            case "EZ_DOOR":
                                transform.gameObject.SetActive(false);
                                Exiled.API.Features.Log.Debug($"Spawning EZ Door", true);
                                door = DoorUtils.SpawnDoor(DoorUtils.DoorType.EZ_BREAKABLE, tor.transform.position, tor.transform.eulerAngles, tor.transform.lossyScale);

                                this.SpawnedChildren[prefabObject].Add(door.gameObject);
                                break;

                            case "LCZ_DOOR":
                                transform.gameObject.SetActive(false);
                                Exiled.API.Features.Log.Debug($"Spawning LCZ Door", true);
                                door = DoorUtils.SpawnDoor(DoorUtils.DoorType.LCZ_BREAKABLE, tor.transform.position, tor.transform.eulerAngles, tor.transform.lossyScale);

                                this.SpawnedChildren[prefabObject].Add(door.gameObject);
                                break;

                            case "TARGET_DBOY":
                                transform.gameObject.SetActive(false);
                                Exiled.API.Features.Log.Debug($"Spawning TARGET_DBOY", true);
                                var dBoy = Exiled.API.Features.ShootingTarget.Spawn(ShootingTargetType.ClassD, Vector3.zero).Base;
                                dBoy.transform.position = tor.transform.position;
                                dBoy.transform.eulerAngles = tor.transform.eulerAngles;
                                dBoy.transform.localScale = tor.transform.localScale;
                                this.SpawnedChildren[prefabObject].Add(dBoy.gameObject);
                                break;

                            default:
                                break;
                        }

                        if (door != null)
                        {
                            if (nameArgs.Length > 1 && nameArgs[1] == "(LOCKED)")
                                door.ServerChangeLock(DoorLockReason.AdminCommand, true);
                            if (nameArgs.Any(x => x.StartsWith("(JOIN:", StringComparison.InvariantCultureIgnoreCase)))
                            {
                                var arg = nameArgs.First(x => x.StartsWith("(JOIN:", StringComparison.InvariantCultureIgnoreCase)).Split(':')[1].Split(')')[0];
                                var id = uint.Parse(arg.Split('|')[0]);
                                if (arg.ToUpper().Contains("|ONETIME"))
                                    RemovePostUse.Add(door);
                                ConnectedAnimators[door] = null;
                                foreach (var animator in prefabObject.GetComponentsInChildren<Animator>())
                                {
                                    if (animator.name.ToUpper().Contains($"(JOIN:{id})"))
                                    {
                                        ConnectedAnimators[door] = animator;

                                        Exiled.API.Features.Log.Debug($"Joined {transform.gameObject.name} with {animator.name}", true);
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    this.SpawnedChildren[prefabObject].Add(tor);
                    continue;
                }

                if (!transform.TryGetComponent<MeshRenderer>(out MeshRenderer renderer))
                    continue;

                PrimitiveType type = PrimitiveType.Sphere;

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
                    renderer.material.color).gameObject);
            }

            return prefabObject;
        }

        internal static readonly Dictionary<DoorVariant, Animator> ConnectedAnimators = new Dictionary<DoorVariant, Animator>();
        internal static readonly HashSet<DoorVariant> RemovePostUse = new HashSet<DoorVariant>();
        internal static readonly HashSet<Transform> HighUpdateRate = new HashSet<Transform>();

        private static LightSourceToy primitiveBaseLight = null;
        private static PrimitiveObjectToy primitiveBaseObject = null;

        private static PrimitiveObjectToy PrimitiveBaseObject
        {
            get
            {
                if (primitiveBaseObject == null)
                {
                    foreach (var gameObject in NetworkClient.prefabs.Values)
                    {
                        if (gameObject.TryGetComponent<PrimitiveObjectToy>(out var component))
                            primitiveBaseObject = component;
                    }
                }

                return primitiveBaseObject;
            }
        }

        private static LightSourceToy PrimitiveBaseLight
        {
            get
            {
                if (primitiveBaseLight == null)
                {
                    foreach (var gameObject in NetworkClient.prefabs.Values)
                    {
                        if (gameObject.TryGetComponent<LightSourceToy>(out var component))
                            primitiveBaseLight = component;
                    }
                }

                return primitiveBaseLight;
            }
        }

        private static PrimitiveObjectToy CreatePrimitive(Transform parent, PrimitiveType type, Color color)
        {
            AdminToyBase toy = UnityEngine.Object.Instantiate(PrimitiveBaseObject, parent);
            PrimitiveObjectToy ptoy = toy.GetComponent<PrimitiveObjectToy>();
            ptoy.NetworkPrimitiveType = type;
            ptoy.NetworkMaterialColor = color;
            ptoy.transform.localPosition = Vector3.zero;
            ptoy.transform.localRotation = Quaternion.identity;
            ptoy.transform.localScale = Vector3.one;
            ptoy.NetworkScale = ptoy.transform.localScale;
            if (HighUpdateRate.Contains(parent))
                ptoy.NetworkMovementSmoothing = 60;
            NetworkServer.Spawn(toy.gameObject);
            return ptoy;
        }

        private static LightSourceToy CreateLight(Transform parent, Color color, float intensity, float range, bool shadows)
        {
            AdminToyBase toy = UnityEngine.Object.Instantiate(PrimitiveBaseLight, parent);
            LightSourceToy ptoy = toy.GetComponent<LightSourceToy>();
            ptoy.NetworkLightColor = color;
            ptoy.NetworkLightIntensity = intensity;
            ptoy.NetworkLightRange = range;
            ptoy.NetworkLightShadows = shadows;
            ptoy.transform.localPosition = Vector3.zero;
            ptoy.transform.localRotation = Quaternion.identity;
            ptoy.transform.localScale = Vector3.one;
            ptoy.NetworkScale = ptoy.transform.localScale;
            NetworkServer.Spawn(toy.gameObject);
            return ptoy;
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
    }
}
