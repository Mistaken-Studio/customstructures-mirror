// -----------------------------------------------------------------------
// <copyright file="RespawnTimerHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections;
using Exiled.API.Features;
using Mistaken.UnityPrefabs;
using Mistaken.UnityPrefabs.SegmentDisplay;
using UnityEngine;

namespace Mistaken.CustomStructures.AssetHandlers
{
    internal class RespawnTimerHandler : SingleAssetHandler
    {
        public override void Initialize(Asset asset)
        {
            base.Initialize(asset);
            this.display = this.gameObject.GetComponent<TimerSegmentScript>();

            this.display.SetText("--");

            Exiled.Events.Handlers.Server.RoundStarted += this.Server_RoundStarted;
        }

        public override void OnDestroy()
        {
            Exiled.Events.Handlers.Server.RoundStarted -= this.Server_RoundStarted;
        }

        protected override AssetMeta.AssetType AssetType => AssetMeta.AssetType.RESPAWN_TIMER;

        private TimerSegmentScript display;

        private IEnumerator UpdateTimer()
        {
            while (Round.IsStarted)
            {
                if (Respawn.IsSpawning || API.Utilities.Map.RespawnLock)
                    this.display.SetText("----");
                else
                    this.display.SetDisplayTime(Respawn.TimeUntilRespawn);

                yield return new WaitForSeconds(1);
            }
        }

        private void Server_RoundStarted()
        {
            this.StartCoroutine(this.UpdateTimer());
        }
    }
}
