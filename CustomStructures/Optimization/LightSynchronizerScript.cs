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

#pragma warning disable SA1118 // Parameter should not span multiple lines
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable IDE0051

// ReSharper disable UnusedMember.Local
// ReSharper disable CompareOfFloatsByEqualityOperator
namespace Mistaken.CustomStructures.Optimization
{
    internal class LightSynchronizerScript : SynchronizerScript
    {
        internal readonly Dictionary<Player, float> LastStates = new Dictionary<Player, float>();

        internal new LightSourceToy Toy => (LightSourceToy)base.Toy;

        internal override void UpdateSubscriber(Player player)
        {
            if (!this.LastStates.ContainsKey(player))
                this.LastStates[player] = 0;

            if (this.LastStates[player] == this.lastState)
                return;

            this.SyncFor(player);

            this.LastStates[player] = this.lastState;
        }

        private Light light;

        private float lastState;

        private void Awake()
        {
            this.light = this.GetComponent<Light>();
        }

        private void LateUpdate()
        {
            if (this.Toy == null)
                return;

            if (this.Toy.NetworkLightColor != this.light.color)
            {
                // ToDo - Rewrite to support color synchronization on the same level as color intensity
                this.Toy.NetworkLightColor = this.light.color;
                this.Toy.NetworkLightIntensity = this.light.intensity;
                this.lastState = this.light.intensity;
            }
            else if (this.light.intensity != this.lastState)
            {
                this.lastState = this.light.intensity;

                foreach (var item in this.Controller.Subscribers)
                    this.UpdateSubscriber(item);
            }
        }

        private void SyncFor(Player player)
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
                targetWriter.WriteUInt64(16UL);
                targetWriter.WriteSingle(this.lastState);
            }
        }
    }
}
