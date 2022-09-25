// -----------------------------------------------------------------------
// <copyright file="PrimitiveSynchronizerScript.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using AdminToys;
using Exiled.API.Features;
using Mirror;
using UnityEngine;
// ReSharper disable NonReadonlyMemberInGetHashCode

#pragma warning disable SA1118 // Parameter should not span multiple lines
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1116 // Split parameters should start on line after declaration
#pragma warning disable IDE0051

namespace Mistaken.CustomStructures.Optimization
{
    internal class PrimitiveSynchronizerScript : SynchronizerScript
    {
        internal readonly Dictionary<Player, PrimitiveState> LastStates = new Dictionary<Player, PrimitiveState>();

        internal class PrimitiveState
        {
            public static bool operator ==(PrimitiveState a, PrimitiveState b)
                => a?.Equals(b) ?? b is null;

            public static bool operator !=(PrimitiveState a, PrimitiveState b)
                => !(a == b);

            public bool Equals(PrimitiveState other)
                => this.visible == other.visible &&
                   this.position.Equals(other.position) &&
                   this.rotation.Equals(other.rotation) &&
                   this.scale.Equals(other.scale) &&
                   this.color.Equals(other.color);

            public override bool Equals(object obj)
                => obj is PrimitiveState other && this.Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = this.visible.GetHashCode();
                    hashCode = (hashCode * 397) ^ this.position.GetHashCode();
                    hashCode = (hashCode * 397) ^ this.rotation.GetHashCode();
                    hashCode = (hashCode * 397) ^ this.scale.GetHashCode();
                    hashCode = (hashCode * 397) ^ this.color.GetHashCode();
                    return hashCode;
                }
            }

            // ToDo - Add support for de-spawning objects
            public bool visible { get; set; }

            public Vector3 position { get; set; }

            public LowPrecisionQuaternion rotation { get; set; }

            public Vector3 scale { get; set; }

            public Color color { get; set; }
        }

        internal new PrimitiveObjectToy Toy => (PrimitiveObjectToy)base.Toy;

        internal override void UpdateSubscriber(Player player)
        {
            if (!this.LastStates.ContainsKey(player))
                this.LastStates[player] = default;

            if (this.LastStates[player] == this.lastState)
                return;

            this.SyncFor(player, this.LastStates[player]);

            this.LastStates[player] = this.lastState;
        }

        private PrimitiveState lastState;

        private void LateUpdate()
        {
            if (this.Toy == null)
                return;

            if (this.Toy.Position != this.lastState.position ||
                this.Toy.Rotation != this.lastState.rotation ||
                this.Toy.Scale != this.lastState.scale ||
                this.Toy.MaterialColor != this.lastState.color)
            {
                this.lastState.position = this.Toy.Position;
                this.lastState.rotation = this.Toy.Rotation;
                this.lastState.scale = this.Toy.Scale;
                this.lastState.color = this.Toy.MaterialColor;

                foreach (var item in this.Controller.Subscribers)
                    this.UpdateSubscriber(item);
            }
        }

        private void SyncFor(Player player, PrimitiveState playerState)
            =>
                this.SyncFor(player,
                    this.lastState.position != playerState.position,
                    this.lastState.rotation != playerState.rotation,
                    this.lastState.scale != playerState.scale,
                    this.lastState.color != playerState.color);

        private void SyncFor(Player player, bool syncPosition, bool syncRotation, bool syncScale, bool syncColor)
        {
            var writer = NetworkWriterPool.GetWriter();
            var writer2 = NetworkWriterPool.GetWriter();

            MakeCustomSyncWriter.Invoke(null, new object[]
            {
                this.Toy.netIdentity,
                typeof(PrimitiveObjectToy),
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
                // ToDo - Rewrite to send only what has changed
                targetWriter.WriteUInt64(0UL + (syncPosition ? 1UL : 0UL) + (syncRotation ? 2UL : 0UL) + (syncScale ? 4UL : 0UL)); // position (1) + rotation (2) + scale (4)
                targetWriter.WriteVector3(this.lastState.position);
                targetWriter.WriteLowPrecisionQuaternion(this.lastState.rotation);
                targetWriter.WriteVector3(this.lastState.scale);
                targetWriter.WriteUInt64(0UL + (syncColor ? 32UL : 0UL)); // color (32)
                targetWriter.WriteColor(this.lastState.color);
            }
        }
    }
}
