// -----------------------------------------------------------------------
// <copyright file="SurfaceGateATowerElevatorHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Pickups;
using MEC;
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
                throw new NullReferenceException($"{nameof(this.bottomFloor)} was null");

            this.topFloor = this.top.transform.Find("Floor");
            if (this.topFloor == null)
                throw new NullReferenceException($"{nameof(this.topFloor)} was null");

            this.offset = this.topFloor.transform.position - this.bottomFloor.transform.position;

            this.bottomDoor = asset.Doors[this.bottom.Find("Entrance").Find("HCZ_DOOR").gameObject];

            if (this.bottomDoor == null)
                throw new NullReferenceException($"{nameof(this.bottomDoor)} was null");

            this.topDoor = asset.Doors[this.top.Find("Entrance").Find("HCZ_DOOR").gameObject];

            if (this.topDoor == null)
                throw new NullReferenceException($"{nameof(this.topDoor)} was null");

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

        private static void Move(GameObject item, Vector3 offset)
        {
            if (item.TryGetComponent<ItemPickupBase>(out var pickup))
            {
                pickup.transform.position += offset;
                pickup.RefreshPositionAndRotation();
            }
            else if (item.TryGetComponent<ReferenceHub>(out var rh))
                rh.playerMovementSync.ForcePosition(rh.playerMovementSync.RealModelPosition + offset);
        }

        private Transform bottom;
        private Transform top;

        private Transform bottomFloor;
        private Transform topFloor;
        private Vector3 offset;

        private DoorVariant bottomDoor;
        private DoorVariant topDoor;

        private bool isOnTop;
        private bool isMoving;

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

            var inRange = Physics.OverlapBox(
                this.bottomFloor.transform.position + Vector3.up,
                (this.bottomFloor.transform.lossyScale / 2.2f) + (Vector3.up * 2),
                this.bottomFloor.transform.rotation);

            foreach (var item in inRange.Where(x => !x.isTrigger).Select(x => x.transform.root.gameObject).ToHashSet())
                Move(item.gameObject, this.offset);

            yield return Timing.WaitForSeconds(2);
            this.topDoor.NetworkTargetState = true;
            this.isOnTop = true;
            yield return Timing.WaitForSeconds(5);

            this.isMoving = false;
            this.bottomDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, false);
            this.topDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, false);
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

            var inRange = Physics.OverlapBox(
                this.topFloor.transform.position + Vector3.up,
                (this.bottomFloor.transform.lossyScale / 2.2f) + (Vector3.up * 2),
                this.topFloor.transform.rotation);

            foreach (var item in inRange.Where(x => !x.isTrigger).Select(x => x.transform.root.gameObject).ToHashSet())
                Move(item.gameObject, -this.offset);

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

            if (ev.Door.Base != this.bottomDoor && ev.Door.Base != this.topDoor)
                return;

            ev.IsAllowed = false;

            if (!this.isMoving)
                Timing.RunCoroutine(this.isOnTop ? this.MoveDown() : this.MoveUp());
        }
    }
}
