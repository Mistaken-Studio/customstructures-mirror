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
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Mistaken.CustomStructures
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
            // var boundles = LoadBoundles(Path.Combine(Paths.Plugins, "AssetBoundle"));

            var bridge1 = new GameObject();
            bridge1.transform.position = new Vector3(75.28f, 1000 - 7.289f, -49.5f);
            LoadBoundle("bridge", bridge1.transform);
            Task.Delay(5).Wait();

            var bridge2 = new GameObject();
            bridge2.transform.position = new Vector3(87.53f, 1000 - 7.289f, -59.27f);
            bridge2.transform.eulerAngles = Vector3.up * -90f;
            LoadBoundle("bridge", bridge2.transform);
            Task.Delay(5).Wait();

            var surfaceGateBBridgeConnector = new GameObject();
            surfaceGateBBridgeConnector.transform.position = new Vector3(0, 1000, 0);
            LoadBoundle("surface_gateb_bridge_connector", surfaceGateBBridgeConnector.transform);
            Task.Delay(5).Wait();

            var surfaceGateAStairsLock = new GameObject();
            surfaceGateAStairsLock.transform.position = new Vector3(0, 1000, 0);
            LoadBoundle("surface_gatea_stairs_lock", surfaceGateAStairsLock.transform);
            Task.Delay(5).Wait();

            var surfaceGateATunnelCIDoor = new GameObject();
            surfaceGateATunnelCIDoor.transform.position = new Vector3(0, 1000, 0);
            LoadBoundle("surface_gatea_tunnel_ci_door", surfaceGateATunnelCIDoor.transform);
            Task.Delay(5).Wait();

            var surfaceGateATunnelElevatorDoor = new GameObject();
            surfaceGateATunnelElevatorDoor.transform.position = new Vector3(0, 1000, 0);
            LoadBoundle("surface_gatea_tunnel_elevator_door", surfaceGateATunnelElevatorDoor.transform);
            Task.Delay(5).Wait();

            var surfaceGateATower = new GameObject();
            surfaceGateATower.transform.position = new Vector3(0, 1000, 0);
            LoadBoundle("surface_gatea_middletower", surfaceGateATower.transform);
            Task.Delay(5).Wait();

            var surfaceGateATowerArmory = new GameObject();
            surfaceGateATowerArmory.transform.position = new Vector3(0, 1000, 0);
            LoadBoundle("surface_gatea_tower_armory", surfaceGateATowerArmory.transform);
            Task.Delay(5).Wait();

            var surfaceGateATowerSCP1499Chamber = new GameObject();
            surfaceGateATowerSCP1499Chamber.transform.position = new Vector3(0, 1000, 0);
            LoadBoundle("surface_gatea_tower_scp1499_chamber", surfaceGateATowerSCP1499Chamber.transform);
            Task.Delay(5).Wait();

            var surfaceGateATowerElevatorBottom = new GameObject();
            surfaceGateATowerElevatorBottom.transform.position = new Vector3(0, 1000, 0);
            LoadBoundle("surface_gatea_tower_elevator_bottom", surfaceGateATowerElevatorBottom.transform);
            Task.Delay(5).Wait();

            var surfaceGateATowerElevatorTop = new GameObject();
            surfaceGateATowerElevatorTop.transform.position = new Vector3(0, 1000, 0);
            LoadBoundle("surface_gatea_tower_elevator_top", surfaceGateATowerElevatorTop.transform);
            Task.Delay(5).Wait();

            var surfaceCICar = new GameObject();
            surfaceCICar.transform.position = new Vector3(0, 1000, 0);
            LoadBoundle("surface_cicar", surfaceCICar.transform);
            Task.Delay(5).Wait();

            // foreach (var item in boundles)
            //     item.Unload(false);
        }

        internal static IEnumerable<AssetBundle> LoadBoundles(string files)
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

        internal static GameObject LoadBoundle(string name, Transform parent = null)
        {
            var boundle = AssetBundle.LoadFromFile(Path.Combine(Paths.Plugins, "AssetBoundle", "assets"));
            var prefab = boundle.LoadAsset<GameObject>(name);

            if (prefab == null)
            {
                Exiled.API.Features.Log.Error($"{name} was not found in the boundle");
                return null;
            }

            // GameObject.Instantiate(prefab);

            var toy = ConvertToToy(prefab, parent);
            boundle.Unload(false);
            Exiled.API.Features.Log.Debug($"Loaded {name}", true);

            return toy;
        }

        internal static GameObject _LoadBoundle(string name, Transform parent = null)
        {
            var boundles = LoadBoundles(Path.Combine(Paths.Plugins, "AssetBoundle"));

            var tor = LoadBoundle(name, boundles, parent);

            foreach (var item in boundles)
                item.Unload(false);

            return tor;
        }

        internal static GameObject LoadBoundle(string name, IEnumerable<AssetBundle> boundles, Transform parent = null)
        {
            foreach (var boundle in boundles)
            {
                if (boundle.Contains(name))
                {
                    var prefab = boundle.LoadAsset<GameObject>(name);

                    if (prefab == null)
                    {
                        Exiled.API.Features.Log.Error($"{name} was not found in the boundle");
                        return null;
                    }

                    GameObject.Instantiate(prefab);

                    var toy = ConvertToToy(prefab, parent);
                    Exiled.API.Features.Log.Debug($"Loaded {name}", true);

                    return toy;
                }
            }

            return null;
        }

        private static GameObject ConvertToToy(GameObject toConvert, Transform parent = null)
        {
            Exiled.API.Features.Log.Debug($"Loading {toConvert.name}", true);
            if (!toConvert.activeSelf)
                return null;
            var meshFilter = toConvert.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                var tor = new GameObject();
                if (parent != null)
                    tor.transform.parent = parent.transform;
                string name = toConvert.name;
                tor.name = name;
                tor.transform.localPosition = toConvert.transform.localPosition;
                Exiled.API.Features.Log.Debug($"Position: {tor.transform.position}", true);
                tor.transform.localRotation = toConvert.transform.localRotation;
                Exiled.API.Features.Log.Debug($"Rotation: {tor.transform.rotation}", true);
                tor.transform.localScale = toConvert.transform.localScale;
                Exiled.API.Features.Log.Debug($"Scale   : {tor.transform.lossyScale}", true);

                DoorVariant door;
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
                    string[] nameArgs = name.Split(' ');
                    Exiled.API.Features.Log.Debug($"Spawning GameObject ({nameArgs[0]})", true);
                    switch (nameArgs[0])
                    {
                        case "HCZ_DOOR":
                            Exiled.API.Features.Log.Debug($"Spawning HCZ Door", true);
                            door = DoorUtils.SpawnDoor(DoorUtils.DoorType.HCZ_BREAKABLE, tor.transform.position, tor.transform.eulerAngles, tor.transform.localScale);

                            if (nameArgs.Length > 1 && nameArgs[1] == "(LOCKED)")
                                door.ServerChangeLock(DoorLockReason.AdminCommand, true);
                            break;

                        case "EZ_DOOR":
                            Exiled.API.Features.Log.Debug($"Spawning EZ Door", true);
                            door = DoorUtils.SpawnDoor(DoorUtils.DoorType.EZ_BREAKABLE, tor.transform.position, tor.transform.eulerAngles, tor.transform.localScale);

                            if (nameArgs.Length > 1 && nameArgs[1] == "(LOCKED)")
                                door.ServerChangeLock(DoorLockReason.AdminCommand, true);
                            break;

                        case "LCZ_DOOR":
                            Exiled.API.Features.Log.Debug($"Spawning LCZ Door", true);
                            door = DoorUtils.SpawnDoor(DoorUtils.DoorType.LCZ_BREAKABLE, tor.transform.position, tor.transform.eulerAngles, tor.transform.localScale);

                            if (nameArgs.Length > 1 && nameArgs[1] == "(LOCKED)")
                                door.ServerChangeLock(DoorLockReason.AdminCommand, true);
                            break;

                        case "TARGET_DBOY":
                            Exiled.API.Features.Log.Debug($"Spawning TARGET_DBOY", true);
                            var dBoy = GetTarget();
                            dBoy.transform.position = tor.transform.position;
                            dBoy.transform.eulerAngles = tor.transform.eulerAngles;
                            dBoy.transform.localScale = tor.transform.localScale;
                            break;

                        default:
                            for (int i = 0; i < toConvert.transform.childCount; i++)
                            {
                                var child = toConvert.transform.GetChild(i);
                                ConvertToToy(child.gameObject, tor.transform);
                            }

                            break;
                    }
                }

                return tor;
            }
            else
            {
                PrimitiveObjectToy toy = GetPrimitiveObjectToy();
                if (parent != null)
                    toy.transform.parent = parent.transform;
                toy.name = toConvert.name;
                toy.transform.localPosition = toConvert.transform.localPosition;
                Exiled.API.Features.Log.Debug($"Position: {toy.transform.position}", true);
                toy.transform.localRotation = toConvert.transform.localRotation;
                Exiled.API.Features.Log.Debug($"Rotation: {toy.transform.rotation}", true);
                toy.transform.localScale = toConvert.transform.localScale;
                Exiled.API.Features.Log.Debug($"Scale   : {toy.transform.lossyScale}", true);
                toy.NetworkScale = toy.transform.lossyScale;

                var meshRenderer = toConvert.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    toy.NetworkMaterialColor = meshRenderer.material.color;
                }

                string mesh = meshFilter.mesh.name.Split(' ')[0];
                Exiled.API.Features.Log.Debug($"Mesh: {mesh}", true);
                switch (mesh)
                {
                    case "Cube":
                        toy.NetworkPrimitiveType = PrimitiveType.Cube;
                        break;
                    case "Cylinder":
                        toy.NetworkPrimitiveType = PrimitiveType.Cylinder;
                        break;
                    case "Capsule":
                        toy.NetworkPrimitiveType = PrimitiveType.Capsule;
                        break;
                    case "Sphere":
                        toy.NetworkPrimitiveType = PrimitiveType.Sphere;
                        break;
                    case "Quad":
                        toy.NetworkPrimitiveType = PrimitiveType.Quad;
                        break;
                    case "Plane":
                        toy.NetworkPrimitiveType = PrimitiveType.Plane;
                        break;
                    default:
                        Exiled.API.Features.Log.Warn($"Unknown mesh type: {mesh}");
                        break;
                }

                for (int i = 0; i < toConvert.transform.childCount; i++)
                {
                    var child = toConvert.transform.GetChild(i);
                    ConvertToToy(child.gameObject, toy.transform);
                }

                bool hasCollider = false;
                foreach (var component in toConvert.GetComponents<Component>())
                {
                    if (component is Rigidbody rb)
                    {
                        var toyRb = (Rigidbody)toy.gameObject.AddComponent(component.GetType());
                        toyRb.centerOfMass = rb.centerOfMass;
                        toyRb.collisionDetectionMode = rb.collisionDetectionMode;
                        toyRb.constraints = rb.constraints;
                        toyRb.detectCollisions = rb.detectCollisions;
                        toyRb.freezeRotation = rb.freezeRotation;
                        toyRb.interpolation = rb.interpolation;
                        toyRb.isKinematic = rb.isKinematic;
                        toyRb.mass = rb.mass;
                        toyRb.useGravity = rb.useGravity;
                    }
                    else if (component is Collider collider)
                    {
                        hasCollider = true;
                    }
                    else if (component is Transform)
                    {
                    }
                    else if (component is MeshFilter)
                    {
                    }
                    else if (component is MeshRenderer)
                    {
                    }
                    else
                        Exiled.API.Features.Log.Warn($"Unknown component {component.GetType()}");
                }

                if (!hasCollider)
                    toy.transform.localScale = toConvert.transform.localScale * -1f;

                Exiled.API.Features.Log.Debug($"Loaded {toConvert.name}", true);
                return toy.gameObject;
            }
        }

        private static PrimitiveObjectToy GetPrimitiveObjectToy()
        {
            foreach (var item in NetworkClient.prefabs.Values)
            {
                if (item.TryGetComponent<PrimitiveObjectToy>(out PrimitiveObjectToy adminToyBase))
                {
                    PrimitiveObjectToy toy = UnityEngine.Object.Instantiate<PrimitiveObjectToy>(adminToyBase);
                    toy.SpawnerFootprint = new Footprint(Server.Host.ReferenceHub);
                    NetworkServer.Spawn(toy.gameObject);
                    toy.NetworkPrimitiveType = PrimitiveType.Sphere;
                    toy.NetworkMaterialColor = Color.gray;
                    toy.transform.position = Vector3.zero;
                    toy.transform.eulerAngles = Vector3.zero;
                    toy.transform.localScale = Vector3.one;
                    toy.NetworkScale = toy.transform.localScale;
                    return toy;
                }
            }

            return null;
        }

        private static AdminToys.ShootingTarget GetTarget()
        {
            return Exiled.API.Features.ShootingTarget.Spawn(ShootingTargetType.ClassD, Vector3.zero).Base;
        }
    }
}
