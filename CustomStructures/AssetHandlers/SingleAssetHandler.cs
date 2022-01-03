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
        public override void Initialize(GameObject spawned, Asset asset)
        {
            if (spawned == null)
                throw new ArgumentNullException("spawned");

            if (asset == null)
                throw new ArgumentNullException("asset");

            this.GameObject = spawned;
            this.Asset = asset;
            var tmp = this.GameObject.AddComponent<DestructionInformerScript>();
            if (tmp == null)
            {
                tmp = this.GameObject.GetComponent<DestructionInformerScript>();
                if (tmp == null)
                    throw new ArgumentNullException("tmp ims null");
            }

            try
            {
                tmp.OnDestroyed += this.OnDeinitialize;
            }
            catch (Exception)
            {
                throw new ArgumentNullException("tmp.OnDestroyed");
            }
        }

        public virtual void OnDeinitialize(GameObject gameObject)
        {
        }

        protected Asset Asset { get; private set; }

        protected abstract AssetMeta.AssetType AssetType { get; }

        protected GameObject GameObject { get; private set; }
    }
}
