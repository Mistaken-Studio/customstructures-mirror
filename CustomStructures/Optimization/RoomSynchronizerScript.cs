// -----------------------------------------------------------------------
// <copyright file="RoomSynchronizerScript.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using Exiled.API.Features;
using UnityEngine;

#pragma warning disable SA1401 // Fields should be private
#pragma warning disable IDE0051

// ReSharper disable UnusedMember.Local
// ReSharper disable once IdentifierTypo
namespace Mistaken.CustomStructures.Optimization
{
    internal class RoomSynchronizerScript : MonoBehaviour
    {
        public void AddSubscriber(Player player)
        {
            if (this.Subscribers.Contains(player))
                return;

            this.Subscribers.Add(player);
            foreach (var light in this.synchronizerScripts)
            {
                light.UpdateSubscriber(player);
            }
        }

        public void RemoveSubscriber(Player player)
        {
            this.Subscribers.Remove(player);
        }

        internal readonly HashSet<Player> Subscribers = new HashSet<Player>();

        private SynchronizerScript[] synchronizerScripts;

        private void Awake()
        {
            this.synchronizerScripts = this.GetComponentsInChildren<SynchronizerScript>();

            foreach (var item in this.synchronizerScripts)
                item.Controller = this;
        }
    }
}
