// -----------------------------------------------------------------------
// <copyright file="ColorSynchronizerScript.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using AdminToys;
using UnityEngine;

namespace Mistaken.CustomStructures
{
    internal class ColorSynchronizerScript : MonoBehaviour
    {
        internal PrimitiveObjectToy Toy { get; set; }

        private MeshRenderer mesh;

        private void Awake()
        {
            this.mesh = this.GetComponent<MeshRenderer>();
        }

        private void LateUpdate()
        {
            if (this.Toy == null)
                return;

            if (this.Toy.NetworkMaterialColor != this.mesh.material.color)
                this.Toy.NetworkMaterialColor = this.mesh.material.color;
        }
    }
}
