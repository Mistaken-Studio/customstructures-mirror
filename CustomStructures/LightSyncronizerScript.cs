// -----------------------------------------------------------------------
// <copyright file="LightSyncronizerScript.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using AdminToys;
using UnityEngine;

namespace Mistaken.CustomStructures
{
    internal class LightSyncronizerScript : MonoBehaviour
    {
        public LightSourceToy Toy;
        public Light Light;

        public void Awake()
        {
            this.Light = this.GetComponent<Light>();
        }

        public void LateUpdate()
        {
            if (this.Toy == null)
                return;

            if (this.Toy.enabled != this.Light.enabled)
                this.Toy.enabled = this.Light.enabled;
            if (this.Toy.NetworkLightColor != this.Light.color)
                this.Toy.NetworkLightColor = this.Light.color;
            if (this.Toy.NetworkLightIntensity != this.Light.intensity)
                this.Toy.NetworkLightRange = this.Light.range;
            if (this.Toy.NetworkLightShadows != (this.Light.shadows == LightShadows.Soft))
                this.Toy.NetworkLightShadows = this.Light.shadows == LightShadows.Soft;
        }
    }
}
