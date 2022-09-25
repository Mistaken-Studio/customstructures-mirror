// -----------------------------------------------------------------------
// <copyright file="LightSynchronizerScript.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using AdminToys;
using Mirror;
using UnityEngine;

// ReSharper disable UnusedMember.Local
// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable NonReadonlyMemberInGetHashCode
namespace Mistaken.CustomStructures.Optimization
{
    internal class LightSynchronizerScript : SynchronizerScript
    {
        protected override State CurrentState => this.currentLightState;

        protected override Type ToyType => typeof(LightSourceToy);

        protected override bool ShouldUpdate()
        {
            if (base.ShouldUpdate() ||
                this.light.color != this.currentLightState.Color ||
                this.light.intensity != this.currentLightState.Intensity ||
                this.light.range != this.currentLightState.Range ||
                (this.light.shadows == LightShadows.Soft) != this.currentLightState.Shadows)
            {
                this.currentLightState.Color = this.light.color;
                this.currentLightState.Intensity = this.light.intensity;
                this.currentLightState.Range = this.light.range;
                this.currentLightState.Shadows = this.light.shadows == LightShadows.Soft;
                return true;
            }

            return false;
        }

        protected override ulong GetStateFlags(State playerState)
        {
            var tor = base.GetStateFlags(playerState);

            if (!(playerState is LightState state))
                throw new ArgumentException($"Supplied {nameof(playerState)} was not {nameof(LightState)}, it was {playerState?.GetType().FullName ?? "NULL"}", nameof(playerState));

            if (this.currentLightState.Intensity != state.Intensity) tor += 16;
            if (this.currentLightState.Range != state.Range) tor += 32;
            if (this.currentLightState.Color != state.Color) tor += 64;
            if (this.currentLightState.Shadows != state.Shadows) tor += 128;

            return tor;
        }

        protected override Action<NetworkWriter> CustomSyncVarGenerator(ulong flags, Action<NetworkWriter> callBackAction = null)
        {
            return base.CustomSyncVarGenerator(flags, targetWriter =>
            {
                targetWriter.WriteUInt64(flags & (16 + 32 + 64 + 128));  // intensity (16) + range (32) + color (64) + shadows (128)
                if ((flags & 16) != 0) targetWriter.WriteSingle(this.currentLightState.Intensity);
                if ((flags & 32) != 0) targetWriter.WriteSingle(this.currentLightState.Range);
                if ((flags & 64) != 0) targetWriter.WriteColor(this.currentLightState.Color);
                if ((flags & 128) != 0) targetWriter.WriteBoolean(this.currentLightState.Shadows);
                callBackAction?.Invoke(targetWriter);
            });
        }

        protected class LightState : State
        {
            public float Intensity { get; set; }

            public float Range { get; set; }

            public bool Shadows { get; set; }

            public Color Color { get; set; }

            public override bool Equals(State other)
                =>
                    base.Equals(other) &&
                    other is LightState light &&
                    this.Intensity.Equals(light.Intensity) &&
                    this.Range.Equals(light.Range) &&
                    this.Shadows == light.Shadows &&
                    this.Color.Equals(light.Color);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = base.GetHashCode();
                    hashCode = (hashCode * 397) ^ this.Intensity.GetHashCode();
                    hashCode = (hashCode * 397) ^ this.Range.GetHashCode();
                    hashCode = (hashCode * 397) ^ this.Shadows.GetHashCode();
                    hashCode = (hashCode * 397) ^ this.Color.GetHashCode();
                    return hashCode;
                }
            }
        }

        private readonly LightState currentLightState = new LightState();
        private Light light;

        private new LightSourceToy Toy => (LightSourceToy)base.Toy;

        private void Awake()
        {
            this.light = this.GetComponent<Light>();
        }
    }
}
