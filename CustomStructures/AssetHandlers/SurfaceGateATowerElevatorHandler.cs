// -----------------------------------------------------------------------
// <copyright file="SurfaceGateATowerElevatorHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Pickups;
using MEC;
using Mistaken.UnityPrefabs;
using UnityEngine;

namespace Mistaken.CustomStructures.AssetHandlers
{
    internal class SurfaceGateATowerElevatorHandler : SingleAssetHandler
    {
        public override void Initialize(GameObject spawned, Asset asset)
        {
            base.Initialize(spawned, asset);

            this.bottom = spawned.transform.Find("Surface_GateA_Tower_Elevator_Bottom");
            this.top = spawned.transform.Find("Surface_GateA_Tower_Elevator_Top");

            this.bottomTrigger = this.bottom.transform.Find("Trigger");
            if (this.bottomTrigger == null)
                throw new ArgumentNullException("BottomTrigger");
            this.topTrigger = this.top.transform.Find("Trigger");
            if (this.topTrigger == null)
                throw new ArgumentNullException("TopTrigger");
            this.offset = this.topTrigger.transform.position - this.bottomTrigger.transform.position;

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

        public override void OnDeinitialize(GameObject gameObject)
        {
            base.OnDeinitialize(gameObject);
            Exiled.Events.Handlers.Player.InteractingDoor -= this.Player_InteractingDoor;
        }

        protected override AssetMeta.AssetType AssetType => AssetMeta.AssetType.SURFACE_GATEA_TOWER_ELEVATOR;

        private Transform bottom;
        private Transform top;

        private Transform bottomTrigger;
        private Transform topTrigger;
        private Vector3 offset;

        private DoorVariant bottomDoor;
        private DoorVariant topDoor;

        private bool isOnTop = false;
        private bool isMoving = false;

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

            var inRange = Physics.OverlapBox(this.bottomTrigger.transform.position, this.bottomTrigger.transform.lossyScale / 2, this.bottomTrigger.transform.rotation);

            foreach (var item in inRange.Where(x => !x.isTrigger).Select(x => x.transform.root.gameObject).ToHashSet())
                this.Move(item.gameObject, this.offset);

            yield return Timing.WaitForSeconds(2);

            this.bottomDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, false);
            this.topDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, false);
            this.topDoor.NetworkTargetState = true;
            this.isOnTop = true;
            this.isMoving = false;
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

            var inRange = Physics.OverlapBox(this.topTrigger.transform.position, this.topTrigger.transform.lossyScale / 2, this.topTrigger.transform.rotation);

            foreach (var item in inRange.Where(x => !x.isTrigger).Select(x => x.transform.root.gameObject).ToHashSet())
                this.Move(item.gameObject, -this.offset);

            yield return Timing.WaitForSeconds(2);

            this.bottomDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, false);
            this.topDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, false);
            this.bottomDoor.NetworkTargetState = true;
            this.isOnTop = false;
            this.isMoving = false;
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
