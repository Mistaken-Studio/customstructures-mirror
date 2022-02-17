// -----------------------------------------------------------------------
// <copyright file="ElectricalRoomHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Linq;
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
            if (this.lever is null || this.display is null)
                this.Invoke(nameof(this.SetLever), 5);

            this.triggerItem = Asset.ConnectedItemScriptTriggers.Single(x => x.Value.Name == "EZ_ELECTRICAL_ROOM_TESLA_LEVER").Key;
        }

        public override void OnDestroy()
        {
        }

        public override void OnScriptTrigger(string name)
        {
            if (this.cooldown)
                return;

            if (name == "EZ_ELECTRICAL_ROOM_TESLA_LEVER")
            {
                this.currentState = !this.currentState;
                this.lever.SetBool("Enabled", this.currentState);

                if (this.currentState)
                {
                    API.Utilities.Map.TeslaMode = API.Utilities.TeslaMode.ENABLED;
                    Respawning.RespawnEffectsController.PlayCassieAnnouncement("FACILITY WIDE OVERRIDE . TESLA GATES ENGAGED", false, false, true);
                }
                else
                {
                    API.Utilities.Map.TeslaMode = API.Utilities.TeslaMode.DISABLED;
                    Respawning.RespawnEffectsController.PlayCassieAnnouncement("FACILITY WIDE OVERRIDE . TESLA GATES DISENGAGED", false, false, true);
                }

                this.cooldown = true;
                this.display.SetTime(300);
                this.display.OnFinishCounting = () => this.cooldown = false;
            }
        }

        protected override AssetMeta.AssetType AssetType => AssetMeta.AssetType.EZ_ELECTRICALROOM;

        private Animator lever;
        private TimerSegmentScript display;
        private ItemPickupBase triggerItem;

        private bool currentState = true;
        private bool cooldown = false;

        private void SetLever()
        {
            this.lever = this.gameObject.GetComponentInChildren<Animator>();
            this.display = this.gameObject.GetComponentInChildren<TimerSegmentScript>();
            this.display?.SetText("----");
        }
    }
}
