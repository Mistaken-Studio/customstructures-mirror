// -----------------------------------------------------------------------
// <copyright file="PrimitiveSynchronizerScript.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using AdminToys;
using Mirror;
using UnityEngine;

// ReSharper disable NonReadonlyMemberInGetHashCode
namespace Mistaken.CustomStructures.Optimization
{
    internal class PrimitiveSynchronizerScript : SynchronizerScript
    {
        protected override Type ToyType => typeof(PrimitiveObjectToy);

        protected override State CurrentState => this.currentPrimitiveState;

        protected override bool ShouldUpdate()
        {
            if (!base.ShouldUpdate() && this.meshRenderer.material.color == this.currentPrimitiveState.Color)
                return false;

            this.currentPrimitiveState.Color = this.meshRenderer.material.color;
            return true;
        }

        protected override ulong GetStateFlags(State playerState)
        {
            var tor = base.GetStateFlags(playerState);

            if (!(playerState is PrimitiveState state))
                throw new ArgumentException($"Supplied {nameof(playerState)} was not {nameof(PrimitiveState)}, it was {playerState?.GetType().FullName ?? "NULL"}", nameof(playerState));

            if (this.currentPrimitiveState.Color != state.Color) tor += 32;

            return tor;
        }

        protected override Action<NetworkWriter> CustomSyncVarGenerator(ulong flags, Action<NetworkWriter> callBackAction = null)
        {
            return base.CustomSyncVarGenerator(flags, targetWriter =>
            {
                targetWriter.WriteUInt64(flags & 32UL); // color (32) | flags & (~31UL)
                if ((flags & 32) != 0) targetWriter.WriteColor(this.currentPrimitiveState.Color);
                callBackAction?.Invoke(targetWriter);
            });
        }

        protected class PrimitiveState : State
        {
            public override bool Equals(State other)
                =>
                    base.Equals(other) &&
                    other is PrimitiveState primitive &&
                    this.Visible == primitive.Visible &&
                    this.Color == primitive.Color;

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = base.GetHashCode();
                    hashCode = (hashCode * 397) ^ this.Visible.GetHashCode();
                    hashCode = (hashCode * 397) ^ this.Color.GetHashCode();
                    return hashCode;
                }
            }

            // ToDo - Add support for de-spawning objects
            public bool Visible { get; set; }

            public Color Color { get; set; }
        }

        private readonly PrimitiveState currentPrimitiveState = new PrimitiveState();
        private MeshRenderer meshRenderer;

        private new PrimitiveObjectToy Toy => (PrimitiveObjectToy)base.Toy;

        private void Awake()
        {
            this.meshRenderer = this.GetComponent<MeshRenderer>();
        }
    }
}
