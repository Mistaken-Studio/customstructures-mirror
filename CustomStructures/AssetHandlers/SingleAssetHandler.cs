// -----------------------------------------------------------------------
// <copyright file="SingleAssetHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Mistaken.UnityPrefabs;
using UnityEngine;

namespace Mistaken.CustomStructures.AssetHandlers
{
    internal abstract class SingleAssetHandler : AssetHandler
    {
        protected Asset Asset { get; private set; }

        protected abstract AssetMeta.AssetType AssetType { get; }

        protected GameObject GameObject { get; private set; }

        public override void Initialize(GameObject spawned, Asset asset)
        {
            Exiled.API.Features.Log.Debug("hm");
            if (spawned == null)
                throw new ArgumentNullException("spawned");
            Exiled.API.Features.Log.Debug("Yes");

            if (asset == null)
                throw new ArgumentNullException("asset");
            Exiled.API.Features.Log.Debug("Yes3");

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
        }

        public virtual void OnDeinitialize(GameObject gameObject)
        {
        }
    }
}
