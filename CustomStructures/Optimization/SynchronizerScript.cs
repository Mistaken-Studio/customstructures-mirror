// -----------------------------------------------------------------------
// <copyright file="SynchronizerScript.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using AdminToys;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Mirror;
using UnityEngine;

#pragma warning disable SA1116 // Split parameters should start on line after declaration
#pragma warning disable SA1118 // Parameter should not span multiple lines
#pragma warning disable SA1401 // Fields should be private

// ReSharper disable UnusedMember.Local
// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable NonReadonlyMemberInGetHashCode
namespace Mistaken.CustomStructures.Optimization
{
    internal abstract class SynchronizerScript : MonoBehaviour
    {
        static SynchronizerScript()
        {
            SynchronizerScript.MakeCustomSyncWriter = typeof(MirrorExtensions)
                .GetMethod("MakeCustomSyncWriter",
                    BindingFlags.NonPublic | BindingFlags.Static);
        }

        internal RoomSynchronizerScript Controller { get; set; }

        internal AdminToyBase Toy { get; set; }

        internal void UpdateSubscriber(Player player)
        {
            if (!this.LastStates.ContainsKey(player))
                this.LastStates[player] = default;

            if (this.LastStates[player] == this.CurrentState)
                return;

            this.SyncFor(player, this.LastStates[player]);

            this.LastStates[player] = this.CurrentState;
        }

        protected static readonly MethodInfo MakeCustomSyncWriter;
        protected readonly Dictionary<Player, State> LastStates = new Dictionary<Player, State>();

        protected abstract Type ToyType { get; }

        protected abstract State CurrentState { get; }

        protected virtual ulong GetStateFlags(State playerState)
        {
            return (this.CurrentState.Position != playerState.Position ? 1UL : 0UL)
                   + (this.CurrentState.Rotation != playerState.Rotation ? 2UL : 0UL)
                   + (this.CurrentState.Scale != playerState.Scale ? 4UL : 0UL);
        }

        protected virtual Action<NetworkWriter> CustomSyncVarGenerator(ulong flags, Action<NetworkWriter> callBackAction = null)
        {
            return targetWriter =>
            {
                targetWriter.WriteUInt64(flags & 7); // position (1) + rotation (2) + scale (4)
                if ((flags & 1) != 0) targetWriter.WriteVector3(this.CurrentState.Position);
                if ((flags & 2) != 0) targetWriter.WriteLowPrecisionQuaternion(this.CurrentState.Rotation);
                if ((flags & 4) != 0) targetWriter.WriteVector3(this.CurrentState.Scale);
                callBackAction?.Invoke(targetWriter);
            };
        }

        protected virtual bool ShouldUpdate()
        {
            if (this.Toy.Position != this.CurrentState.Position ||
                this.Toy.Rotation != this.CurrentState.Rotation ||
                this.Toy.Scale != this.CurrentState.Scale)
            {
                this.CurrentState.Position = this.Toy.Position;
                this.CurrentState.Rotation = this.Toy.Rotation;
                this.CurrentState.Scale = this.Toy.Scale;

                return true;
            }

            return false;
        }

        protected class State
        {
            public static bool operator ==(State a, State b)
                => a?.Equals(b) ?? b is null;

            public static bool operator !=(State a, State b)
                => !(a == b);

            public Vector3 Position { get; set; }

            public LowPrecisionQuaternion Rotation { get; set; }

            public Vector3 Scale { get; set; }

            public virtual bool Equals(State other)
                => this.Position.Equals(other.Position) &&
                   this.Rotation.Equals(other.Rotation) &&
                   this.Scale.Equals(other.Scale);

            public override bool Equals(object obj)
                => obj is State other && this.Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = this.Position.GetHashCode();
                    hashCode = (hashCode * 397) ^ this.Rotation.GetHashCode();
                    hashCode = (hashCode * 397) ^ this.Scale.GetHashCode();
                    return hashCode;
                }
            }
        }

        private void SyncFor(Player player, State playerState)
            => this.SyncFor(player, this.GetStateFlags(playerState));

        private void SyncFor(Player player, ulong flags)
        {
            var writer = NetworkWriterPool.GetWriter();
            var writer2 = NetworkWriterPool.GetWriter();

            MakeCustomSyncWriter.Invoke(null, new object[]
            {
                this.Toy.netIdentity,
                this.ToyType,
                null,
                this.CustomSyncVarGenerator(flags),
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
        }

        private void LateUpdate()
        {
            if (this.Toy == null)
                return;

            if (!this.ShouldUpdate())
                return;

            foreach (var item in this.Controller.Subscribers)
                this.UpdateSubscriber(item);
        }
    }
}
