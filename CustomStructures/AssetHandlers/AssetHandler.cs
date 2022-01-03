// -----------------------------------------------------------------------
// <copyright file="AssetHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using Mistaken.UnityPrefabs;
using UnityEngine;

namespace Mistaken.CustomStructures.AssetHandlers
{
    internal abstract class AssetHandler
    {
        public abstract void Initialize(GameObject spawned, Asset asset);

        public virtual bool IsColliding(AssetMeta.AssetType[] assets) => false;
    }
}
