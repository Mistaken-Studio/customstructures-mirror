// -----------------------------------------------------------------------
// <copyright file="LightSynchronizerScript.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using AdminToys;
using Exiled.API.Features;
using UnityEngine;

namespace Mistaken.CustomStructures
{
    internal class LightSynchronizerScript : MonoBehaviour
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

            if (!this.light.enabled && this.light.intensity != 0)
                Log.Warn($"Do not disable light, Set intensity to 0 instead ({this.transform.position})");

            if (this.Toy.NetworkLightColor != this.light.color)
                this.Toy.NetworkLightColor = this.light.color;
            if (this.Toy.NetworkLightIntensity != this.light.intensity)
                this.Toy.NetworkLightIntensity = this.light.intensity;
            if (this.Toy.NetworkLightRange != this.light.range)
                this.Toy.NetworkLightRange = this.light.range;
            if (this.Toy.NetworkLightShadows != (this.light.shadows == LightShadows.Soft))
                this.Toy.NetworkLightShadows = this.light.shadows == LightShadows.Soft;
        }
    }
}
