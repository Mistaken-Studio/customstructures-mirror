// -----------------------------------------------------------------------
// <copyright file="LinkedAssetHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace Mistaken.CustomStructures.AssetHandlers
{

    internal abstract class LinkedAssetHandler : AssetHandler
    {
        protected Asset Asset { get; private set; }

        protected Asset OtherAsset { get; private set; }

        protected abstract AssetType AssetType { get; }

        protected GameObject GameObject { get; private set; }

        protected abstract AssetType OtherAssetType { get; }

        protected GameObject OtherGameObject { get; private set; }

        public override void Initialize(Dictionary<AssetType, (GameObject Obj, Asset Asset)> spawned)
        {
            var tmp = spawned[this.AssetType];
            var tmp2 = spawned[this.OtherAssetType];
            this.Initialize(tmp.Obj, tmp.Asset, tmp2.Obj, tmp2.Asset);
        }

        public virtual void Initialize(GameObject spawned, Asset asset, GameObject otherSpawned, Asset otherAsset)
        {
            this.GameObject = spawned;
            this.Asset = asset;
            this.GameObject.AddComponent<DestructionInformerScript>().OnDestroyed += this.OnDeinitialize;
            this.OtherGameObject = otherSpawned;
            this.OtherAsset = otherAsset;
            this.OtherGameObject.AddComponent<DestructionInformerScript>().OnDestroyed += this.OnDeinitialize;
        }

        public virtual void OnDeinitialize(GameObject gameObject)
        {
        }

        public override void Register()
        {
            CustomStructuresHandler.AssetsHandlers[this.AssetType] = this;
            CustomStructuresHandler.AssetsHandlers[this.OtherAssetType] = this;
        }
    }
}
