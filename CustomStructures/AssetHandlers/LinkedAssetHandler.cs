// -----------------------------------------------------------------------
// <copyright file="LinkedAssetHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
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
            Exiled.API.Features.Log.Debug("hm");
            var tmp = spawned[this.AssetType];
            var tmp2 = spawned[this.OtherAssetType];
            if (tmp == default)
                throw new ArgumentNullException("tmp");
            if (tmp2 == default)
                throw new ArgumentNullException("tmp2");
            Exiled.API.Features.Log.Debug("Yes");
            if (tmp.Obj == null)
                throw new ArgumentNullException("tmp.Obj");
            if (tmp2.Obj == null)
                throw new ArgumentNullException("tmp2.Obj");
            Exiled.API.Features.Log.Debug("Yes2");

            if (tmp.Asset == null)
                throw new ArgumentNullException("tmp.Asset");
            if (tmp2.Asset == null)
                throw new ArgumentNullException("tmp2.Asset");
            Exiled.API.Features.Log.Debug("Yes3");

            this.Initialize(tmp.Obj, tmp.Asset, tmp2.Obj, tmp2.Asset);
        }

        public virtual void Initialize(GameObject spawned, Asset asset, GameObject otherSpawned, Asset otherAsset)
        {
            this.GameObject = spawned;
            this.Asset = asset;
            var tmp = this.GameObject.AddComponent<DestructionInformerScript>();
            if (tmp == null)
            {
                tmp = this.GameObject.GetComponent<DestructionInformerScript>();
                if (tmp == null)
                {
                    throw new ArgumentNullException("tmp ims null");
                }
                else
                    Exiled.API.Features.Log.Error("tmp was null ;/");
            }

            try
            {
                tmp.OnDestroyed += this.OnDeinitialize;
            }
            catch (Exception)
            {
                throw new ArgumentNullException("tmp.OnDestroyed");
            }

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
