// -----------------------------------------------------------------------
// <copyright file="WarheadTimerHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using AdminToys;
using Exiled.API.Features;
using MEC;
using Mistaken.UnityPrefabs;
using Mistaken.UnityPrefabs.SegmentDisplay;
using UnityEngine;

namespace Mistaken.CustomStructures.AssetHandlers
{
    internal class WarheadTimerHandler : SingleAssetHandler
    {
        public override void Initialize(GameObject spawned, Asset asset)
        {
            base.Initialize(spawned, asset);
            this.display = this.GameObject.GetComponent<MutliSegmentDisplayScript>();

            this.display.SetText("--");

            Exiled.Events.Handlers.Warhead.Starting += this.Warhead_Starting;
            Exiled.Events.Handlers.Warhead.Stopping += this.Warhead_Stopping;
            Exiled.Events.Handlers.Warhead.Detonated += this.Warhead_Detonated;
            Exiled.Events.Handlers.Player.ActivatingWarheadPanel += this.Player_ActivatingWarheadPanel;
        }

        public override void OnDeinitialize(GameObject gameObject)
        {
            base.OnDeinitialize(gameObject);

            Exiled.Events.Handlers.Warhead.Starting -= this.Warhead_Starting;
            Exiled.Events.Handlers.Warhead.Stopping -= this.Warhead_Stopping;
            Exiled.Events.Handlers.Warhead.Detonated -= this.Warhead_Detonated;
            Exiled.Events.Handlers.Player.ActivatingWarheadPanel -= this.Player_ActivatingWarheadPanel;
        }

        protected override AssetMeta.AssetType AssetType => AssetMeta.AssetType.WARHEAD_TIMER;

        private MutliSegmentDisplayScript display;

        private IEnumerator<float> UpdateTimer()
        {
            while (Warhead.IsInProgress && !Warhead.IsDetonated)
            {
                try
                {
                    this.display.SetText(Mathf.RoundToInt(Warhead.DetonationTimer).ToString());
                }
                catch (ArgumentOutOfRangeException)
                {
                    this.display.SetText("99");
                }

                yield return Timing.WaitForSeconds(1);
            }
        }

        private void Warhead_Detonated()
        {
            this.display.SetText("--");
        }

        private void Player_ActivatingWarheadPanel(Exiled.Events.EventArgs.ActivatingWarheadPanelEventArgs ev)
        {
            if (!ev.IsAllowed)
                return;

            try
            {
                this.display.SetText(Mathf.RoundToInt(Warhead.DetonationTimer).ToString());
            }
            catch (ArgumentOutOfRangeException)
            {
                this.display.SetText("99");
            }
        }

        private void Warhead_Stopping(Exiled.Events.EventArgs.StoppingEventArgs ev)
        {
            if (!ev.IsAllowed)
                return;

            try
            {
                this.display.SetText(Mathf.RoundToInt(Warhead.DetonationTimer).ToString());
            }
            catch (ArgumentOutOfRangeException)
            {
                this.display.SetText("99");
            }
        }

        private void Warhead_Starting(Exiled.Events.EventArgs.StartingEventArgs ev)
        {
            if (!ev.IsAllowed)
                return;

            Timing.RunCoroutine(this.UpdateTimer());
        }
    }
}
