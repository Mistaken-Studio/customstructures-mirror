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
        protected override AssetMeta.AssetType AssetType => AssetMeta.AssetType.SURFACE_GATEA_TOWER_ELEVATOR;

        private Transform Bottom;
        private Transform Top;

        private Transform BottomTrigger;
        private Transform TopTrigger;
        private Vector3 Offset;

        private DoorVariant BottomDoor;
        private DoorVariant TopDoor;

        private bool IsOnTop = false;
        private bool IsMoving = false;

        public override void Initialize(GameObject spawned, Asset asset)
        {
            base.Initialize(spawned, asset);

            this.Bottom = spawned.transform.Find("Surface_GateA_Tower_Elevator_Bottom");
            this.Top = spawned.transform.Find("Surface_GateA_Tower_Elevator_Top");

            this.BottomTrigger = this.Bottom.transform.Find("Trigger");
            if (this.BottomTrigger == null)
                throw new ArgumentNullException("BottomTrigger");
            this.TopTrigger = this.Top.transform.Find("Trigger");
            if (this.TopTrigger == null)
                throw new ArgumentNullException("TopTrigger");
            this.Offset = this.TopTrigger.transform.position - this.BottomTrigger.transform.position;

            this.BottomDoor = asset.Doors[this.Bottom.Find("Entrance").Find("LCZ_DOOR").gameObject];

            if (this.BottomDoor == null)
                throw new ArgumentNullException("this.BottomDoor");

            this.TopDoor = asset.Doors[this.Top.Find("Entrance").Find("LCZ_DOOR").gameObject];

            if (this.TopDoor == null)
                throw new ArgumentNullException("this.TopDoor");

            this.BottomDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, false);
            this.BottomDoor.NetworkTargetState = true;

            this.TopDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, true);
            this.TopDoor.NetworkTargetState = false;

            Exiled.Events.Handlers.Player.InteractingDoor += this.Player_InteractingDoor;
        }

        private void Player_InteractingDoor(Exiled.Events.EventArgs.InteractingDoorEventArgs ev)
        {
            if (!ev.IsAllowed)
                return;
            if (ev.Door.Base == this.BottomDoor || ev.Door.Base == this.TopDoor)
            {
                ev.IsAllowed = false;
                if (!this.IsMoving)
                {
                    if (this.IsOnTop)
                        Timing.RunCoroutine(this.MoveDown());
                    else
                        Timing.RunCoroutine(this.MoveUp());
                }
            }
        }

        public override void OnDeinitialize(GameObject gameObject)
        {
            base.OnDeinitialize(gameObject);
            Exiled.Events.Handlers.Player.InteractingDoor -= this.Player_InteractingDoor;
        }

        public IEnumerator<float> MoveUp()
        {
            if (this.IsOnTop)
            {
                Log.Error("Elevator is already on top");
                yield break;
            }

            if (this.IsMoving)
            {
                Log.Error("Elevator is currently in moving state");
                yield break;
            }

            if (this.TopDoor.IsConsideredOpen())
            {
                Log.Error("Elevator Door is open on wrong side");
                yield break;
            }

            this.IsMoving = true;
            this.TopDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, true);
            this.BottomDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, true);
            this.BottomDoor.NetworkTargetState = false;
            yield return Timing.WaitForSeconds(3);
            if (this.BottomDoor.IsConsideredOpen())
            {
                this.BottomDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, false);
                this.TopDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, false);
                this.IsMoving = false;
                yield break;
            }

            var inRange = Physics.OverlapBox(this.BottomTrigger.transform.position, this.BottomTrigger.transform.lossyScale / 2, this.BottomTrigger.transform.rotation);

            foreach (var item in inRange.Where(x => !x.isTrigger).Select(x => x.transform.root.gameObject).ToHashSet())
                this.Move(item.gameObject, this.Offset);

            yield return Timing.WaitForSeconds(2);

            this.BottomDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, false);
            this.TopDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, false);
            this.TopDoor.NetworkTargetState = true;
            this.IsOnTop = true;
            this.IsMoving = false;
        }

        public void Move(GameObject item, Vector3 offset)
        {
            if (item.TryGetComponent<ItemPickupBase>(out var pickup))
            {
                pickup.transform.position += offset;
                pickup.RefreshPositionAndRotation();
            }
            else if (item.TryGetComponent<ReferenceHub>(out var rh))
                rh.playerMovementSync.ForcePosition(rh.playerMovementSync.RealModelPosition + offset);
        }

        public IEnumerator<float> MoveDown()
        {
            if (!this.IsOnTop)
            {
                Log.Error("Elevator is already on bottom");
                yield break;
            }

            if (this.IsMoving)
            {
                Log.Error("Elevator is currently in moving state");
                yield break;
            }

            if (this.BottomDoor.IsConsideredOpen())
            {
                Log.Error("Elevator Door is open on wrong side");
                yield break;
            }

            this.IsMoving = true;
            this.BottomDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, true);
            this.TopDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, true);
            this.TopDoor.NetworkTargetState = false;
            yield return Timing.WaitForSeconds(3);
            if (this.TopDoor.IsConsideredOpen())
            {
                this.BottomDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, false);
                this.TopDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, false);
                this.IsMoving = false;
                yield break;
            }

            var inRange = Physics.OverlapBox(this.TopTrigger.transform.position, this.TopTrigger.transform.lossyScale / 2, this.TopTrigger.transform.rotation);

            foreach (var item in inRange.Where(x => !x.isTrigger).Select(x => x.transform.root.gameObject).ToHashSet())
                this.Move(item.gameObject, -this.Offset);

            yield return Timing.WaitForSeconds(2);

            this.BottomDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, false);
            this.TopDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, false);
            this.BottomDoor.NetworkTargetState = true;
            this.IsOnTop = false;
            this.IsMoving = false;
        }
    }
}
