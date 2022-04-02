// -----------------------------------------------------------------------
// <copyright file="ElectricalRoomHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using InventorySystem.Items.Pickups;
using MEC;
using Mistaken.API;
using Mistaken.API.Diagnostics;
using Mistaken.UnityPrefabs;
using Mistaken.UnityPrefabs.Misc;
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

            this.electricalBox = this.GetComponentInChildren<ElectricalBoxScript>();

            this.teslas = FindObjectsOfType<TeslaGate>();
            foreach (var tesla in this.teslas)
                this.StartCoroutine(this.UpdateLight(tesla));

            Room.List.First(x => x.Type == Exiled.API.Enums.RoomType.EzCollapsedTunnel)
                .Color = Color.white;

            Exiled.Events.Handlers.Player.TriggeringTesla += this.Player_TriggeringTesla;
            Exiled.Events.Handlers.Server.RoundStarted += this.Server_RoundStarted;
            Exiled.Events.Handlers.Scp079.InteractingTesla += this.Scp079_InteractingTesla;
            Exiled.Events.Handlers.Player.ChangingRole += this.Player_ChangingRole;
        }

        public override void OnDestroy()
        {
            Exiled.Events.Handlers.Player.TriggeringTesla -= this.Player_TriggeringTesla;
            Exiled.Events.Handlers.Server.RoundStarted -= this.Server_RoundStarted;
            Exiled.Events.Handlers.Scp079.InteractingTesla -= this.Scp079_InteractingTesla;
            Exiled.Events.Handlers.Player.ChangingRole -= this.Player_ChangingRole;

            this.cooldown = true;
            this.display.OnFinishCounting = null;
        }

        public override void OnScriptTrigger(string name)
        {
            if (Round.ElapsedTime.TotalSeconds < 10)
                return;

            if (this.cooldown)
                return;

            if (this.lever is null)
            {
                Log.Warn("Lever is null");
                this.SetLever();
            }

            if (name == "EZ_ELECTRICAL_ROOM_TESLA_LEVER")
            {
                try
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
                catch (System.Exception ex)
                {
                    Log.Error(ex);
                    foreach (var item in RealPlayers.List.Where(x => Vector3.Distance(x.Position, this.transform.position) < 5))
                    {
                        item.Broadcast(5, "<color=red>Dzwignia jest zablokowana</color>");
                    }
                }
            }
        }

        protected override AssetMeta.AssetType AssetType => AssetMeta.AssetType.EZ_ELECTRICALROOM;

        private readonly HashSet<TeslaGate> suppresedTeslas = new HashSet<TeslaGate>();
        private Animator lever;
        private TimerSegmentScript display;
        private ItemPickupBase triggerItem;
        private ElectricalBoxScript electricalBox;
        private TeslaGate[] teslas;

        private bool currentState = true;
        private bool cooldown = false;

        private void SetLever()
        {
            this.lever = this.gameObject.GetComponentInChildren<Animator>();
            this.display = this.gameObject.GetComponentInChildren<TimerSegmentScript>();
            this.display?.SetText("----");
        }

        private void Server_RoundStarted()
        {
            MEC.Timing.CallDelayed(3, () =>
            {
                foreach (var player in RealPlayers.Get(RoleType.FacilityGuard))
                {
                    if (player.CurrentRoom?.Type == Exiled.API.Enums.RoomType.EzCollapsedTunnel)
                        player.Position = Room.List.First(x => x.Type == Exiled.API.Enums.RoomType.EzCafeteria).Position + Vector3.up;
                }
            });
        }

        private IEnumerator UpdateLight(TeslaGate teslaGate)
        {
            int index = this.teslas.IndexOf(teslaGate);
            while (true)
            {
                yield return new WaitForSeconds(1);

                if (this.suppresedTeslas.Contains(teslaGate))
                    continue;

                if (API.Utilities.Map.TeslaMode == API.Utilities.TeslaMode.ENABLED)
                {
                    if (teslaGate.isIdling)
                        this.electricalBox.SetStatus(index + 1, Color.yellow);
                    else if (teslaGate.InProgress)
                        this.electricalBox.SetStatus(index + 1, Color.red);
                    else
                        this.electricalBox.SetStatus(index + 1, Color.green);
                }
                else
                    this.electricalBox.SetStatus(index + 1, new Color(0, 0, 0, 0));
            }
        }

        private void Player_TriggeringTesla(Exiled.Events.EventArgs.TriggeringTeslaEventArgs ev)
        {
            if (!ev.IsTriggerable || API.Utilities.Map.TeslaMode != API.Utilities.TeslaMode.ENABLED)
                return;

            int index = this.teslas.IndexOf(ev.Tesla.Base);

            this.electricalBox.SetStatus(index + 1, Color.red);
            this.suppresedTeslas.Add(ev.Tesla.Base);

            Module.CallSafeDelayed(
                1.5f,
                () =>
                {
                    this.electricalBox.SetStatus(index + 1, Color.yellow);
                    this.suppresedTeslas.Remove(ev.Tesla.Base);
                },
                "UnsuppressTesla");
        }

        private void Scp079_InteractingTesla(Exiled.Events.EventArgs.InteractingTeslaEventArgs ev)
        {
            if (!ev.IsAllowed || API.Utilities.Map.TeslaMode != API.Utilities.TeslaMode.ENABLED)
                return;

            int index = this.teslas.IndexOf(ev.Tesla.Base);

            this.electricalBox.SetStatus(index + 1, Color.red);
            this.suppresedTeslas.Add(ev.Tesla.Base);

            Module.CallSafeDelayed(
                .75f,
                () =>
                {
                    this.electricalBox.SetStatus(index + 1, Color.yellow);
                    this.suppresedTeslas.Remove(ev.Tesla.Base);
                },
                "UnsuppressTesla");
        }

        private void Player_ChangingRole(Exiled.Events.EventArgs.ChangingRoleEventArgs ev)
        {
            if (ev.NewRole != RoleType.FacilityGuard || !ev.IsAllowed)
                return;
            Module.CallSafeDelayed(
                1.5f,
                () =>
                {
                    if (ev.Player.CurrentRoom?.Type == Exiled.API.Enums.RoomType.EzCollapsedTunnel)
                        ev.Player.Position = Room.List.First(x => x.Type == Exiled.API.Enums.RoomType.EzCafeteria).Position + Vector3.up;
                },
                "UnsuppressTesla");
        }
    }
}
