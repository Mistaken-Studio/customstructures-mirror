// -----------------------------------------------------------------------
// <copyright file="LoadCommand.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Mistaken.API.Commands;
using Mistaken.API.Extensions;
using Mistaken.UnityPrefabs;
using UnityEngine;

namespace Mistaken.CustomStructures
{
    // ReSharper disable once UnusedMember.Global
    [CommandSystem.CommandHandler(typeof(CommandSystem.RemoteAdminCommandHandler))]
    internal class LoadCommand : IBetterCommand
    {
        public override string Command => "loadAssets";

        public override string[] Execute(CommandSystem.ICommandSender sender, string[] args, out bool s)
        {
            s = false;
            if (args.Length == 0) return new[] { "Wrong args" };
            if (args[0] == "bring")
            {
                this.toy.transform.position = sender.GetPlayer().Position;
                return new[] { "Bringed" };
            }

            this.toy = new GameObject
            {
                transform =
                {
                    position = sender.GetPlayer().Position,
                },
            };
            CustomStructuresHandler.SpawnAsset((AssetMeta.AssetType)System.Enum.Parse(typeof(AssetMeta.AssetType), args[0]), this.toy.transform);
            s = true;
            return new[] { "Spawned" };
        }

        private GameObject toy;
    }
}
