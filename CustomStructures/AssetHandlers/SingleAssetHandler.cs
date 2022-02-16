// -----------------------------------------------------------------------
// <copyright file="SingleAssetHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Mistaken.UnityPrefabs;
using UnityEngine;

namespace Mistaken.CustomStructures.AssetHandlers
{
    internal abstract class SingleAssetHandler : AssetHandler
    {
        public override void Initialize(Asset asset)
        {
            if (asset == null)
                throw new ArgumentNullException("asset");

            this.Asset = asset;
        }

        public virtual void OnDestroy()
        {
        }

        protected Asset Asset { get; private set; }

        protected abstract AssetMeta.AssetType AssetType { get; }

        [System.Obsolete("Use this.gameObject")]
        protected GameObject GameObject => this.gameObject;
    }
}
