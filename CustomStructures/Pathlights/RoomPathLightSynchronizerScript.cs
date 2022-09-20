// -----------------------------------------------------------------------
// <copyright file="RoomPathLightSynchronizerScript.cs" company="Mistaken">
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
namespace Mistaken.CustomStructures.Pathlights
{
    internal class RoomPathLightSynchronizerScript : MonoBehaviour
    {
        public void AddSubscriber(Player player)
        {
            if (this.Subscribers.Contains(player))
                return;

            this.Subscribers.Add(player);
            foreach (var light in this.lights)
            {
                if (!light.LastStates.ContainsKey(player))
                    light.LastStates[player] = 0;
                light.UpdateSubscriber(player);
            }
        }

        public void RemoveSubscriber(Player player)
        {
            this.Subscribers.Remove(player);
        }

        internal readonly HashSet<Player> Subscribers = new HashSet<Player>();

        private PathLightSynchronizerScript[] lights;

        private void Awake()
        {
            this.lights = this.GetComponentsInChildren<PathLightSynchronizerScript>();

            foreach (var item in this.lights)
                item.Controller = this;
        }
    }
}
