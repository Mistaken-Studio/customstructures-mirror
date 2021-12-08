// -----------------------------------------------------------------------
// <copyright file="SingleAssetHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace Mistaken.CustomStructures.AssetHandlers
{
    internal abstract class SingleAssetHandler : AssetHandler
    {
        protected Asset Asset { get; private set; }

        protected abstract AssetType AssetType { get; }

        protected GameObject GameObject { get; private set; }

        public override void Initialize(Dictionary<AssetType, (GameObject Obj, Asset Asset)> spawned)
        {
            var tmp = spawned[this.AssetType];
            this.Initialize(tmp.Obj, tmp.Asset);
        }

        public virtual void Initialize(GameObject spawned, Asset asset)
        {
            this.Asset = asset;
            this.GameObject = spawned;
            this.GameObject.AddComponent<DestructionInformerScript>().OnDestroyed += this.OnDeinitialize;
        }

        public virtual void OnDeinitialize(GameObject gameObject)
        {
        }

        public override void Register()
        {
            CustomStructuresHandler.AssetsHandlers[this.AssetType] = this;
        }
    }
}
