// -----------------------------------------------------------------------
// <copyright file="AssetHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace Mistaken.CustomStructures.AssetHandlers
{
    internal abstract class AssetHandler
    {
        public abstract void Initialize(Dictionary<AssetType, (GameObject Obj, Asset Asset)> spawned);

        public virtual bool IsColliding(AssetType[] assets) => false;

        public abstract void Register();
    }
}
