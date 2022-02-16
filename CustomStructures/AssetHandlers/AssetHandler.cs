// -----------------------------------------------------------------------
// <copyright file="AssetHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Mistaken.UnityPrefabs;
using UnityEngine;

namespace Mistaken.CustomStructures.AssetHandlers
{
    internal abstract class AssetHandler : MonoBehaviour
    {
        public abstract void Initialize(Asset asset);

        public virtual bool IsColliding(AssetMeta.AssetType[] assets) => false;

        public virtual void OnScriptTrigger(string name)
        {
        }
    }
}
