// -----------------------------------------------------------------------
// <copyright file="SurfaceGateATowerElevatorHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Pickups;
using MEC;
using Mistaken.API.Extensions;
using Mistaken.UnityPrefabs;
using UnityEngine;

namespace Mistaken.CustomStructures.AssetHandlers
{
    internal class SurfaceGateATowerElevatorHandler : SingleAssetHandler
    {
        public override void Initialize(Asset asset)
        {
            base.Initialize(asset);

            this.bottom = this.gameObject.transform.Find("Surface_GateA_Tower_Elevator_Bottom");
            this.top = this.gameObject.transform.Find("Surface_GateA_Tower_Elevator_Top");

            this.bottomFloor = this.bottom.transform.Find("Floor");
            if (this.bottomFloor == null)
                throw new ArgumentNullException("bottomFloor");
            this.topFloor = this.top.transform.Find("Floor");
            if (this.topFloor == null)
                throw new ArgumentNullException("topFloor");
            this.offset = this.topFloor.transform.position - this.bottomFloor.transform.position;

            this.bottomDoor = asset.Doors[this.bottom.Find("Entrance").Find("LCZ_DOOR").gameObject];

            if (this.bottomDoor == null)
                throw new ArgumentNullException("this.BottomDoor");

            this.topDoor = asset.Doors[this.top.Find("Entrance").Find("LCZ_DOOR").gameObject];

            if (this.topDoor == null)
                throw new ArgumentNullException("this.TopDoor");

            this.bottomDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, false);
            this.bottomDoor.NetworkTargetState = true;

            this.topDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, true);
            this.topDoor.NetworkTargetState = false;

            Exiled.Events.Handlers.Player.InteractingDoor += this.Player_InteractingDoor;
        }

        public override void OnDestroy()
        {
            Exiled.Events.Handlers.Player.InteractingDoor -= this.Player_InteractingDoor;
        }

        protected override AssetMeta.AssetType AssetType => AssetMeta.AssetType.SURFACE_GATEA_TOWER_ELEVATOR;

        private Transform bottom;
        private Transform top;

        private Transform bottomFloor;
        private Transform topFloor;
        private Vector3 offset;

        private DoorVariant bottomDoor;
        private DoorVariant topDoor;

        private bool isOnTop = false;
        private bool isMoving = false;

        private void Start()
        {
            this.StartCoroutine(this.HandleKiller());
        }

        private IEnumerator HandleKiller()
        {
            Dictionary<Player, int> counter = new Dictionary<Player, int>();
            Dictionary<Player, List<Type>> enabledEffects = new Dictionary<Player, List<Type>>();
            Vector3 center = new Vector3(-21.75f, 1020.75f, -43.5f);
            Vector3 size = new Vector3(5.5f, 2.5f, 5.5f);

            int pointsPerSecond = 2;

            while (true)
            {
                yield return new WaitForSeconds(1);
                var inRange = Physics.OverlapBox(center, size, Quaternion.identity);

                foreach (var item in inRange.Where(x => !x.isTrigger).Select(x => x.transform.root.gameObject).ToHashSet())
                {
                    var player = Player.Get(item);
                    if (player is null || player.IsDead || player.IsGodModeEnabled)
                        continue;
                    if (!counter.ContainsKey(player))
                        counter.Add(player, pointsPerSecond + 1);
                    else
                        counter[player] += pointsPerSecond + 1;

                    if (!enabledEffects.ContainsKey(player))
                        enabledEffects.Add(player, new List<Type>());
                }

                foreach (var item in counter.Where(x => x.Value > 0).ToArray())
                {
                    var player = item.Key;
                    var counterValue = --counter[player];
                    if (player.IsDead)
                        counterValue = counter[player] = 0;
                    player.SetGUI("Test", API.GUI.PseudoGUIPosition.TOP, "Points: " + counterValue);

                    switch (counterValue)
                    {
                        case int x when x > 3 * 60 * pointsPerSecond:
                            if (!player.GetEffectActive<CustomPlayerEffects.Bleeding>())
                            {
                                player.EnableEffect<CustomPlayerEffects.Bleeding>();
                                enabledEffects[player].Add(typeof(CustomPlayerEffects.Bleeding));
                            }

                            if (!player.GetEffectActive<CustomPlayerEffects.Blinded>())
                            {
                                player.EnableEffect<CustomPlayerEffects.Blinded>();
                                enabledEffects[player].Add(typeof(CustomPlayerEffects.Blinded));
                            }

                            if (!player.GetEffectActive<CustomPlayerEffects.Burned>())
                            {
                                player.EnableEffect<CustomPlayerEffects.Burned>();
                                enabledEffects[player].Add(typeof(CustomPlayerEffects.Burned));
                            }

                            if (!player.GetEffectActive<CustomPlayerEffects.Asphyxiated>())
                            {
                                player.EnableEffect<CustomPlayerEffects.Asphyxiated>();
                                enabledEffects[player].Add(typeof(CustomPlayerEffects.Asphyxiated));
                            }

                            break;

                        case int x when x > 2 * 60 * pointsPerSecond:
                            if (!player.GetEffectActive<CustomPlayerEffects.Concussed>())
                            {
                                player.EnableEffect<CustomPlayerEffects.Concussed>();
                                enabledEffects[player].Add(typeof(CustomPlayerEffects.Concussed));
                            }

                            if (!player.GetEffectActive<CustomPlayerEffects.Disabled>())
                            {
                                player.EnableEffect<CustomPlayerEffects.Disabled>();
                                enabledEffects[player].Add(typeof(CustomPlayerEffects.Disabled));
                            }

                            if (!player.GetEffectActive<CustomPlayerEffects.Deafened>())
                            {
                                player.EnableEffect<CustomPlayerEffects.Deafened>();
                                enabledEffects[player].Add(typeof(CustomPlayerEffects.Deafened));
                            }

                            break;

                        default:
                            foreach (var effectType in enabledEffects[player])
                            {
                                var effect = player.ActiveEffects.FirstOrDefault(x => x.GetType() == effectType);
                                if (!(effect is null))
                                    effect.Intensity = 0;
                            }

                            enabledEffects[player].Clear();
                            break;
                    }
                }
            }
        }

        private IEnumerator<float> MoveUp()
        {
            if (this.isOnTop)
            {
                Log.Error("Elevator is already on top");
                yield break;
            }

            if (this.isMoving)
            {
                Log.Error("Elevator is currently in moving state");
                yield break;
            }

            if (this.topDoor.IsConsideredOpen())
            {
                Log.Error("Elevator Door is open on wrong side");
                yield break;
            }

            this.isMoving = true;
            this.topDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, true);
            this.bottomDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, true);
            this.bottomDoor.NetworkTargetState = false;
            yield return Timing.WaitForSeconds(3);
            if (this.bottomDoor.IsConsideredOpen())
            {
                this.bottomDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, false);
                this.topDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, false);
                this.isMoving = false;
                yield break;
            }

            var inRange = Physics.OverlapBox(this.bottomFloor.transform.position + Vector3.up, this.bottomFloor.transform.lossyScale / 2.2f, this.bottomFloor.transform.rotation);

            foreach (var item in inRange.Where(x => !x.isTrigger).Select(x => x.transform.root.gameObject).ToHashSet())
                this.Move(item.gameObject, this.offset);

            yield return Timing.WaitForSeconds(2);
            this.topDoor.NetworkTargetState = true;
            this.isOnTop = true;
            yield return Timing.WaitForSeconds(5);

            this.isMoving = false;
            this.bottomDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, false);
            this.topDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, false);
        }

        private void Move(GameObject item, Vector3 offset)
        {
            if (item.TryGetComponent<ItemPickupBase>(out var pickup))
            {
                pickup.transform.position += offset;
                pickup.RefreshPositionAndRotation();
            }
            else if (item.TryGetComponent<ReferenceHub>(out var rh))
                rh.playerMovementSync.ForcePosition(rh.playerMovementSync.RealModelPosition + offset);
        }

        private IEnumerator<float> MoveDown()
        {
            if (!this.isOnTop)
            {
                Log.Error("Elevator is already on bottom");
                yield break;
            }

            if (this.isMoving)
            {
                Log.Error("Elevator is currently in moving state");
                yield break;
            }

            if (this.bottomDoor.IsConsideredOpen())
            {
                Log.Error("Elevator Door is open on wrong side");
                yield break;
            }

            this.isMoving = true;
            this.bottomDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, true);
            this.topDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, true);
            this.topDoor.NetworkTargetState = false;
            yield return Timing.WaitForSeconds(3);
            if (this.topDoor.IsConsideredOpen())
            {
                this.bottomDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, false);
                this.topDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, false);
                this.isMoving = false;
                yield break;
            }

            var inRange = Physics.OverlapBox(this.topFloor.transform.position + Vector3.up, this.topFloor.transform.lossyScale / 2.2f, this.topFloor.transform.rotation);

            foreach (var item in inRange.Where(x => !x.isTrigger).Select(x => x.transform.root.gameObject).ToHashSet())
                this.Move(item.gameObject, -this.offset);

            yield return Timing.WaitForSeconds(2);

            this.bottomDoor.NetworkTargetState = true;
            this.isOnTop = false;
            yield return Timing.WaitForSeconds(5);

            this.isMoving = false;
            this.bottomDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, false);
            this.topDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, false);
        }

        private void Player_InteractingDoor(Exiled.Events.EventArgs.InteractingDoorEventArgs ev)
        {
            if (!ev.IsAllowed)
                return;
            if (ev.Door.Base == this.bottomDoor || ev.Door.Base == this.topDoor)
            {
                ev.IsAllowed = false;
                if (!this.isMoving)
                {
                    if (this.isOnTop)
                        Timing.RunCoroutine(this.MoveDown());
                    else
                        Timing.RunCoroutine(this.MoveUp());
                }
            }
        }
    }
}
