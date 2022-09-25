// -----------------------------------------------------------------------
// <copyright file="LightSynchronizerScript.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using AdminToys;
using Exiled.API.Features;
using Mirror;
using UnityEngine;

// ReSharper disable UnusedMember.Local
// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable NonReadonlyMemberInGetHashCode
#pragma warning disable SA1118 // Parameter should not span multiple lines
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable IDE0051

namespace Mistaken.CustomStructures.Optimization
{
    internal class LightSynchronizerScript : SynchronizerScript
    {
        internal readonly Dictionary<Player, LightState> LastStates = new Dictionary<Player, LightState>();

        internal class LightState
        {
            public static bool operator ==(LightState a, LightState b)
                => a?.Equals(b) ?? b is null;

            public static bool operator !=(LightState a, LightState b)
                => !(a == b);

            public override bool Equals(object obj)
                => obj is LightState other && this.Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = this.intensity.GetHashCode();
                    hashCode = (hashCode * 397) ^ this.range.GetHashCode();
                    hashCode = (hashCode * 397) ^ this.shadows.GetHashCode();
                    hashCode = (hashCode * 397) ^ this.color.GetHashCode();
                    return hashCode;
                }
            }

            protected bool Equals(LightState other)
            {
                return this.intensity.Equals(other.intensity) &&
                       this.range.Equals(other.range) &&
                       this.shadows == other.shadows &&
                       this.color.Equals(other.color);
            }

            public float intensity { get; set; }

            public float range { get; set; }

            public bool shadows { get; set; }

            public Color color { get; set; }
        }

        internal new LightSourceToy Toy => (LightSourceToy)base.Toy;

        internal override void UpdateSubscriber(Player player)
        {
            if (!this.LastStates.ContainsKey(player))
                this.LastStates[player] = new LightState();

            if (this.LastStates[player] == this.lastState)
                return;

            this.SyncFor(player, this.LastStates[player]);

            this.LastStates[player] = this.lastState;
        }

        private Light light;

        private LightState lastState;

        private void Awake()
        {
            this.light = this.GetComponent<Light>();
        }

        private void LateUpdate()
        {
            if (this.Toy == null)
                return;

            if (this.light.color != this.lastState.color ||
                this.light.intensity != this.lastState.intensity ||
                this.light.range != this.lastState.range ||
                (this.light.shadows == LightShadows.Soft) != this.lastState.shadows)
            {
                this.lastState.color = this.light.color;
                this.lastState.intensity = this.light.intensity;
                this.lastState.range = this.light.range;
                this.lastState.shadows = this.light.shadows == LightShadows.Soft;

                foreach (var item in this.Controller.Subscribers)
                    this.UpdateSubscriber(item);
            }
        }

        private void SyncFor(Player player, LightState playerState)
            =>
                this.SyncFor(player,
                    this.lastState.color != playerState.color,
                    this.lastState.intensity != playerState.intensity,
                    this.lastState.range != playerState.range,
                    this.lastState.shadows != playerState.shadows);

        private void SyncFor(Player player, bool syncColor, bool syncIntensity, bool syncRange, bool syncShadows)
        {
            var writer = NetworkWriterPool.GetWriter();
            var writer2 = NetworkWriterPool.GetWriter();

            MakeCustomSyncWriter.Invoke(null, new object[]
            {
                this.Toy.netIdentity,
                typeof(LightSourceToy),
                null,
                (Action<NetworkWriter>)CustomSyncVarGenerator,
                writer,
                writer2,
            });

            player.ReferenceHub.networkIdentity.connectionToClient.Send(new UpdateVarsMessage
            {
                netId = this.Toy.netIdentity.netId,
                payload = writer.ToArraySegment(),
            });

            NetworkWriterPool.Recycle(writer);
            NetworkWriterPool.Recycle(writer2);

            void CustomSyncVarGenerator(NetworkWriter targetWriter)
            {
                targetWriter.WriteUInt64(0UL);
                targetWriter.WriteUInt64((syncIntensity ? 16UL : 0UL) + (syncRange ? 32UL : 0UL) + (syncColor ? 64UL : 0UL) + (syncShadows ? 128UL : 0UL)); // intensity (16) + range (32) + color (64) + shadows (128)
                if (syncIntensity) targetWriter.WriteSingle(this.lastState.intensity);
                if (syncRange) targetWriter.WriteSingle(this.lastState.range);
                if (syncColor) targetWriter.WriteColor(this.lastState.color);
                if (syncShadows) targetWriter.WriteBoolean(this.lastState.shadows);
            }
        }
    }
}
