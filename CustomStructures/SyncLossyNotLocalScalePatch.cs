// -----------------------------------------------------------------------
// <copyright file="SyncLossyNotLocalScalePatch.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using NorthwoodLib.Pools;
using UnityEngine;

// ReSharper disable UnusedParameter.Global
// ReSharper disable UnusedMember.Global
namespace Mistaken.CustomStructures
{
    [HarmonyPatch(typeof(AdminToys.AdminToyBase), nameof(AdminToys.AdminToyBase.UpdatePositionServer))]
    internal static class SyncLossyNotLocalScalePatch
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Shared.Rent(instructions);

            var index = newInstructions.FindLastIndex(x => x.opcode == OpCodes.Callvirt);
            newInstructions[index] = new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Transform), nameof(Transform.lossyScale)));

            foreach (var item in newInstructions)
                yield return item;

            ListPool<CodeInstruction>.Shared.Return(newInstructions);
        }
    }
}
