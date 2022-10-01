// -----------------------------------------------------------------------
// <copyright file="SingleAssetHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Mistaken.UnityPrefabs;

namespace Mistaken.CustomStructures.AssetHandlers
{
    internal abstract class SingleAssetHandler : AssetHandler
    {
        public override void Initialize(Asset asset)
        {
            this.Asset = asset ?? throw new ArgumentNullException(nameof(asset));
        }

        public virtual void OnDestroy()
        {
        }

        protected Asset Asset { get; private set; }

        protected abstract AssetMeta.AssetType AssetType { get; }
    }
}
