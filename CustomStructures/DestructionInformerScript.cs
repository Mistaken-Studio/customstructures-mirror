// -----------------------------------------------------------------------
// <copyright file="DestructionInformerScript.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using UnityEngine;

namespace Mistaken.CustomStructures
{
    internal abstract class DestructionInformerScript : MonoBehaviour
    {
        public event Action<GameObject> OnDestroyed;

        private void OnDestroy()
        {
            this.OnDestroyed?.Invoke(this.gameObject);
        }
    }
}
