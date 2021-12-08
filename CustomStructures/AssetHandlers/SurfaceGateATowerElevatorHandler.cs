// -----------------------------------------------------------------------
// <copyright file="SurfaceGateATowerElevatorHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Exiled.API.Features;
using Interactables.Interobjects.DoorUtils;
using MEC;
using UnityEngine;

namespace Mistaken.CustomStructures.AssetHandlers
{
    internal class SurfaceGateATowerElevatorHandler : LinkedAssetHandler
    {
        protected override AssetType AssetType => AssetType.SURFACE_GATEA_TOWER_ELEVATOR_BOTTOM;

        protected override AssetType OtherAssetType => AssetType.SURFACE_GATEA_TOWER_ELEVATOR_TOP;

        private Transform BottomTrigger;
        private Transform TopTrigger;
        private Vector3 Offset;

        private DoorVariant BottomDoor;
        private DoorVariant TopDoor;

        private bool IsOnTop = false;
        private bool IsMoving = false;

        public override void Initialize(GameObject spawned, Asset asset, GameObject otherSpawned, Asset otherAsset)
        {
            base.Initialize(spawned, asset, otherSpawned, otherAsset);

            this.BottomTrigger = spawned.transform.Find("Trigger");
            if (this.BottomTrigger == null)
                throw new ArgumentNullException("BottomTrigger");
            this.TopTrigger = otherSpawned.transform.Find("Trigger");
            if (this.TopTrigger == null)
                throw new ArgumentNullException("TopTrigger");
            this.Offset = this.TopTrigger.transform.position - this.BottomTrigger.transform.position;

            foreach (var item in spawned.GetComponentsInChildren<Transform>())
            {
                if (item.name == "LCZ_DOOR")
                {
                    this.BottomDoor = asset.Doors[item.gameObject];
                    break;
                }
            }

            if (this.BottomDoor == null)
                throw new ArgumentNullException("this.BottomDoor");

            foreach (var item in otherSpawned.GetComponentsInChildren<Transform>())
            {
                if (item.name == "LCZ_DOOR")
                {
                    this.TopDoor = asset.Doors[item.gameObject];
                    break;
                }
            }

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
            if (ev.Door.Base == this.BottomDoor)
            {
                ev.IsAllowed = false;
                Timing.RunCoroutine(this.MoveUp());
            }
            else if (ev.Door.Base == this.TopDoor)
            {
                ev.IsAllowed = false;
                Timing.RunCoroutine(this.MoveDown());
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
                Log.Error("Elevator Door is open");
                this.BottomDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, false);
                this.IsMoving = false;
                yield break;
            }

            var inRange = Physics.OverlapBox(this.BottomTrigger.transform.position, this.BottomTrigger.transform.lossyScale / 2, this.BottomTrigger.transform.rotation);

            foreach (var item in inRange)
            {
                Log.Debug($"[UP] {item.name} was in range");
                item.transform.position += this.Offset;
            }

            yield return Timing.WaitForSeconds(2);

            this.TopDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, false);
            this.TopDoor.NetworkTargetState = true;
            this.IsOnTop = true;
            this.IsMoving = false;
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
                Log.Error("Elevator Door is open");
                this.BottomDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, false);
                this.IsMoving = false;
                yield break;
            }

            var inRange = Physics.OverlapBox(this.TopTrigger.transform.position, this.TopTrigger.transform.lossyScale / 2, this.TopTrigger.transform.rotation);

            foreach (var item in inRange)
            {
                Log.Debug($"[UP] {item.name} was in range");
                item.transform.position -= this.Offset;
            }

            yield return Timing.WaitForSeconds(2);

            this.BottomDoor.ServerChangeLock(DoorLockReason.SpecialDoorFeature, false);
            this.BottomDoor.NetworkTargetState = true;
            this.IsOnTop = false;
            this.IsMoving = false;
        }
    }
}
