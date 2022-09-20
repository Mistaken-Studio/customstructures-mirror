// -----------------------------------------------------------------------
// <copyright file="SynchronizerScript.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using AdminToys;
using Exiled.API.Extensions;
using Exiled.API.Features;
using UnityEngine;

#pragma warning disable SA1116 // Split parameters should start on line after declaration

// ReSharper disable UnusedMember.Local
// ReSharper disable CompareOfFloatsByEqualityOperator
namespace Mistaken.CustomStructures.Optimization
{
    internal abstract class SynchronizerScript : MonoBehaviour
    {
        static SynchronizerScript()
        {
            SynchronizerScript.MakeCustomSyncWriter = typeof(MirrorExtensions)
                .GetMethod("MakeCustomSyncWriter",
                    BindingFlags.NonPublic | BindingFlags.Static);
        }

        internal RoomSynchronizerScript Controller { get; set; }

        internal AdminToyBase Toy { get; set; }

        internal abstract void UpdateSubscriber(Player player);

        protected static readonly MethodInfo MakeCustomSyncWriter;
    }
}
