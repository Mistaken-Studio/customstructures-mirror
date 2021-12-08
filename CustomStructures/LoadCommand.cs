// -----------------------------------------------------------------------
// <copyright file="LoadCommand.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.IO;
using AdminToys;
using CommandSystem;
using Exiled.API.Features;
using Mistaken.API.Commands;
using Mistaken.API.Extensions;
using UnityEngine;

namespace Mistaken.CustomStructures
{
    [CommandSystem.CommandHandler(typeof(CommandSystem.RemoteAdminCommandHandler))]
    internal class LoadCommand : IBetterCommand
    {
        public override string Command => "loadAssets";

        public override string[] Execute(ICommandSender sender, string[] args, out bool s)
        {
            s = false;
            if (args.Length == 0) return new string[] { "Wrong args" };
            if (args[0] == "bring")
            {
                this.toy.transform.position = sender.GetPlayer().Position;
                return new string[] { "Bringed" };
            }

            this.toy = new GameObject();
            this.toy.transform.position = sender.GetPlayer().Position;
            CustomStructuresHandler.SpawnAsset(string.Join(" ", args), this.toy.transform);
            s = true;
            return new string[] { "Spawned" };
        }

        private GameObject toy;
    }
}
