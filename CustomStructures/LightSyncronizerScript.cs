// -----------------------------------------------------------------------
// <copyright file="LightSyncronizerScript.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using AdminToys;
using Mirror;
using UnityEngine;

namespace Mistaken.CustomStructures
{
    internal class LightSyncronizerScript : MonoBehaviour
    {
        internal LightSourceToy Toy { get; set; }

        private Light light;

        private void Awake()
        {
            this.light = this.GetComponent<Light>();
        }

        private void LateUpdate()
        {
            if (this.Toy == null)
                return;

            if (this.Toy.enabled != this.light.enabled)
            {
                this.Toy.enabled = this.light.enabled;
                if (this.light.enabled)
                    NetworkServer.Spawn(this.Toy.gameObject);
                else
                    NetworkServer.UnSpawn(this.Toy.gameObject);
            }

            if (this.Toy.NetworkLightColor != this.light.color)
                this.Toy.NetworkLightColor = this.light.color;
            if (this.Toy.NetworkLightIntensity != this.light.intensity)
                this.Toy.NetworkLightRange = this.light.range;
            if (this.Toy.NetworkLightShadows != (this.light.shadows == LightShadows.Soft))
                this.Toy.NetworkLightShadows = this.light.shadows == LightShadows.Soft;
        }
    }
}
