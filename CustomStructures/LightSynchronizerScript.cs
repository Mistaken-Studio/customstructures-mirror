// -----------------------------------------------------------------------
// <copyright file="LightSynchronizerScript.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using AdminToys;
using Exiled.API.Features;
using Mistaken.UnityPrefabs;
using UnityEngine;

namespace Mistaken.CustomStructures
{
    internal class LightSynchronizerScript : MonoBehaviour
    {
        internal LightSourceToy Toy { get; set; }

        private Light light;
        private AssetMeta meta;

        private void Awake()
        {
            this.light = this.GetComponent<Light>();
            this.meta = this.GetComponentInParent<AssetMeta>();
        }

        private void LateUpdate()
        {
            if (this.Toy == null)
                return;

            if (!this.light.enabled && this.light.intensity != 0)
                Log.Warn($"Do not disable light, Set intensity to 0 instead ({this.transform.position}) ({this.meta?.gameObject.name}: {this.meta?.Type})");

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
