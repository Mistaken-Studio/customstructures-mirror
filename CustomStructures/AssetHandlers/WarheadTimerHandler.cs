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
        public Color BackgroundColor
        {
            get => this.display.Background.material.color;
            set
            {
                this.display.Background.material.color = value;
                var toy = this.display.Background.GetComponentInChildren<AdminToys.PrimitiveObjectToy>();
                if (toy != null)
                    toy.NetworkMaterialColor = this.display.Background.material.color;
            }
        }

        public override void Initialize(GameObject spawned, Asset asset)
        {
            base.Initialize(spawned, asset);
            this.display = this.GameObject.GetComponent<MutliSegmentDisplayScript>();

            foreach (var segment in this.display.segments)
            {
                segment.EnabledColor = LockedColor;
                segment.DisabledColor = new Color(0, 0, 0, 0);
            }

            foreach (var segment in this.display.segments)
                segment.SetNumber(null);

            this.SyncColors();

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

        private static Color LockedColor { get; } = Color.green;

        private static Color PausedColor { get; } = Color.yellow;

        private static Color InProgressColor { get; } = Color.red;

        private MutliSegmentDisplayScript display;

        private IEnumerator<float> UpdateTimer()
        {
            while (Warhead.IsInProgress && !Warhead.IsDetonated)
            {
                try
                {
                    this.display.SetNumber((ushort)Mathf.RoundToInt(Warhead.DetonationTimer));
                }
                catch (ArgumentOutOfRangeException)
                {
                    this.display.SetNumber(99);
                }

                this.SyncColors();

                yield return Timing.WaitForSeconds(1);
            }

            if (Warhead.IsDetonated)
            {
                foreach (var segment in this.display.segments)
                    segment.SetNumber(null);
                this.SyncColors();
            }
        }

        private void SyncColors()
        {
            foreach (var segment in this.display.segments)
            {
                var toy = segment.TopSegment.GetComponentInChildren<PrimitiveObjectToy>();
                if (toy != null)
                    toy.NetworkMaterialColor = segment.TopSegment.material.color;
                toy = segment.LeftTopSegment.GetComponentInChildren<PrimitiveObjectToy>();
                if (toy != null)
                    toy.NetworkMaterialColor = segment.LeftTopSegment.material.color;
                toy = segment.RightTopSegment.GetComponentInChildren<PrimitiveObjectToy>();
                if (toy != null)
                    toy.NetworkMaterialColor = segment.RightTopSegment.material.color;
                toy = segment.MiddleSegment.GetComponentInChildren<PrimitiveObjectToy>();
                if (toy != null)
                    toy.NetworkMaterialColor = segment.MiddleSegment.material.color;
                toy = segment.LeftBottomSegment.GetComponentInChildren<PrimitiveObjectToy>();
                if (toy != null)
                    toy.NetworkMaterialColor = segment.LeftBottomSegment.material.color;
                toy = segment.RightBottomSegment.GetComponentInChildren<PrimitiveObjectToy>();
                if (toy != null)
                    toy.NetworkMaterialColor = segment.RightBottomSegment.material.color;
                toy = segment.BottomSegment.GetComponentInChildren<PrimitiveObjectToy>();
                if (toy != null)
                    toy.NetworkMaterialColor = segment.BottomSegment.material.color;
            }
        }

        private void Warhead_Detonated()
        {
            foreach (var segment in this.display.segments)
                segment.SetNumber(null);

            this.SyncColors();
        }

        private void Player_ActivatingWarheadPanel(Exiled.Events.EventArgs.ActivatingWarheadPanelEventArgs ev)
        {
            if (!ev.IsAllowed)
                return;

            foreach (var segment in this.display.segments)
                segment.EnabledColor = PausedColor;

            try
            {
                this.display.SetNumber((ushort)Mathf.RoundToInt(Warhead.DetonationTimer));
            }
            catch (ArgumentOutOfRangeException)
            {
                this.display.SetNumber(99);
            }

            this.SyncColors();
        }

        private void Warhead_Stopping(Exiled.Events.EventArgs.StoppingEventArgs ev)
        {
            if (!ev.IsAllowed)
                return;

            foreach (var segment in this.display.segments)
                segment.EnabledColor = PausedColor;

            try
            {
                this.display.SetNumber((ushort)Mathf.RoundToInt(Warhead.DetonationTimer));
            }
            catch (ArgumentOutOfRangeException)
            {
                this.display.SetNumber(99);
            }

            this.SyncColors();
        }

        private void Warhead_Starting(Exiled.Events.EventArgs.StartingEventArgs ev)
        {
            if (!ev.IsAllowed)
                return;

            foreach (var segment in this.display.segments)
                segment.EnabledColor = InProgressColor;
            Timing.RunCoroutine(this.UpdateTimer());
        }
    }
}
