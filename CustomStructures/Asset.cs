﻿using System;
using System.Collections.Generic;
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
    public class Asset
    {
        public readonly Dictionary<GameObject, List<GameObject>> SpawnedChildren = new Dictionary<GameObject, List<GameObject>>();

        public string AssetName { get; set; }

        public GameObject Prefab { get; set; }

        public GameObject Spawn(Vector3 position, Vector3 eulerAngles, Vector3 scale)
        {
            var tor = new GameObject();
            tor.transform.position = position;
            tor.transform.eulerAngles = eulerAngles;
            tor.transform.localScale = scale;
            this.Spawn(tor.transform);
            return tor;
        }

        public GameObject Spawn(Transform parent)
        {
            var prefabObject = UnityEngine.Object.Instantiate(this.Prefab, parent);
            prefabObject.hideFlags = HideFlags.HideAndDontSave;

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
                        DoorVariant door;
                        string[] nameArgs = name.Split(' ');
                        Exiled.API.Features.Log.Debug($"Spawning GameObject ({nameArgs[0]})", true);
                        switch (nameArgs[0])
                        {
                            case "HCZ_DOOR":
                                Exiled.API.Features.Log.Debug($"Spawning HCZ Door", true);
                                door = DoorUtils.SpawnDoor(DoorUtils.DoorType.HCZ_BREAKABLE, tor.transform.position, tor.transform.eulerAngles, tor.transform.localScale);

                                if (nameArgs.Length > 1 && nameArgs[1] == "(LOCKED)")
                                    door.ServerChangeLock(DoorLockReason.AdminCommand, true);
                                this.SpawnedChildren[prefabObject].Add(door.gameObject);
                                break;

                            case "EZ_DOOR":
                                Exiled.API.Features.Log.Debug($"Spawning EZ Door", true);
                                door = DoorUtils.SpawnDoor(DoorUtils.DoorType.EZ_BREAKABLE, tor.transform.position, tor.transform.eulerAngles, tor.transform.localScale);

                                if (nameArgs.Length > 1 && nameArgs[1] == "(LOCKED)")
                                    door.ServerChangeLock(DoorLockReason.AdminCommand, true);
                                this.SpawnedChildren[prefabObject].Add(door.gameObject);
                                break;

                            case "LCZ_DOOR":
                                Exiled.API.Features.Log.Debug($"Spawning LCZ Door", true);
                                door = DoorUtils.SpawnDoor(DoorUtils.DoorType.LCZ_BREAKABLE, tor.transform.position, tor.transform.eulerAngles, tor.transform.localScale);

                                if (nameArgs.Length > 1 && nameArgs[1] == "(LOCKED)")
                                    door.ServerChangeLock(DoorLockReason.AdminCommand, true);
                                this.SpawnedChildren[prefabObject].Add(door.gameObject);
                                break;

                            case "TARGET_DBOY":
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

        public static PrimitiveObjectToy PrimitiveBaseObject
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

        public static LightSourceToy PrimitiveBaseLight
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

        public static PrimitiveObjectToy CreatePrimitive(Transform parent, PrimitiveType type, Color color)
        {
            AdminToyBase toy = UnityEngine.Object.Instantiate(PrimitiveBaseObject, parent);
            PrimitiveObjectToy ptoy = toy.GetComponent<PrimitiveObjectToy>();
            ptoy.NetworkPrimitiveType = type;
            ptoy.NetworkMaterialColor = color;
            ptoy.transform.localPosition = Vector3.zero;
            ptoy.transform.localRotation = Quaternion.identity;
            ptoy.transform.localScale = Vector3.one;
            ptoy.NetworkScale = ptoy.transform.localScale;
            NetworkServer.Spawn(toy.gameObject);
            return ptoy;
        }

        public static LightSourceToy CreateLight(Transform parent, Color color, float intensity, float range, bool shadows)
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

        public static GameObject CreateEmpty(Transform parent)
        {
            GameObject tor = new GameObject();
            tor.transform.parent = parent;
            tor.transform.localPosition = Vector3.zero;
            tor.transform.localRotation = Quaternion.identity;
            tor.transform.localScale = Vector3.one;
            return tor;
        }

        private static LightSourceToy primitiveBaseLight = null;
        private static PrimitiveObjectToy primitiveBaseObject = null;
    }
}
