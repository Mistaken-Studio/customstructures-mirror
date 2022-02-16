// -----------------------------------------------------------------------
// <copyright file="ElectricalRoomHandler.cs" company="Mistaken">
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
using Mistaken.UnityPrefabs;
using Mistaken.UnityPrefabs.SegmentDisplay;
using UnityEngine;

namespace Mistaken.CustomStructures.AssetHandlers
{
    internal class ElectricalRoomHandler : SingleAssetHandler
    {
        public override void Initialize(Asset asset)
        {
            base.Initialize(asset);

            this.SetLever();
            if (this.Lever is null || this.Display is null)
                this.Invoke(nameof(this.SetLever), 5);

            this.ItemTrigger = Asset.ConnectedItemScriptTriggers.Single(x => x.Value.Name == "EZ_ELECTRICAL_ROOM_TESLA_LEVER").Key;
        }

        private void SetLever()
        {
            this.Lever = this.gameObject.GetComponentInChildren<Animator>();
            this.Display = this.gameObject.GetComponentInChildren<TimerSegmentScript>();
            this.Display?.SetText("----");
        }

        public override void OnDestroy()
        {
        }

        protected override AssetMeta.AssetType AssetType => AssetMeta.AssetType.EZ_ELECTRICALROOM;

        private Animator Lever;
        private TimerSegmentScript Display;
        private ItemPickupBase ItemTrigger;

        private bool currentState = true;
        private bool cooldown = false;

        public override void OnScriptTrigger(string name)
        {
            if (this.cooldown)
                return;

            if (name == "EZ_ELECTRICAL_ROOM_TESLA_LEVER")
            {
                this.currentState = !this.currentState;
                this.Lever.SetBool("Enabled", this.currentState);

                if (this.currentState)
                {
                    API.Utilities.Map.TeslaMode = API.Utilities.TeslaMode.ENABLED;
                    Cassie.Message("FACILITY WIDE OVERRIDE . TESLA GATES ENGAGED");
                }
                else
                {
                    API.Utilities.Map.TeslaMode = API.Utilities.TeslaMode.DISABLED;
                    Cassie.Message("FACILITY WIDE OVERRIDE . TESLA GATES DISENGAGED");
                }

                this.cooldown = true;
                this.Display.SetTime(300);
                this.Display.OnFinishCounting = () => this.cooldown = false;
            }
        }
    }
}
