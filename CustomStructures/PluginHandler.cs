// -----------------------------------------------------------------------
// <copyright file="PluginHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using HarmonyLib;
using Mistaken.API;
using Mistaken.UnityPrefabs;
using UnityEngine;

namespace Mistaken.CustomStructures
{
    /// <inheritdoc/>
    internal class PluginHandler : Plugin<Config>
    {
        /// <inheritdoc/>
        public override string Author => "Mistaken Devs";

        /// <inheritdoc/>
        public override string Name => "CustomStructures";

        /// <inheritdoc/>
        public override string Prefix => "MCustomStructures";

        /// <inheritdoc/>
        public override PluginPriority Priority => PluginPriority.Default;

        /// <inheritdoc/>
        public override Version RequiredExiledVersion => new Version(4, 1, 2);

        /// <inheritdoc/>
        public override void OnEnabled()
        {
            Instance = this;

            this.harmony = new HarmonyLib.Harmony("mistaken.customstructures");
            this.harmony.PatchAll();

            new CustomStructuresHandler(this);

            CustomStructuresHandler.AssetsHandlers[AssetMeta.AssetType.SURFACE_GATEA_TOWER_ELEVATOR] = typeof(AssetHandlers.SurfaceGateATowerElevatorHandler);
            CustomStructuresHandler.AssetsHandlers[AssetMeta.AssetType.WARHEAD_TIMER] = typeof(AssetHandlers.WarheadTimerHandler);

            Exiled.Events.Handlers.Server.RoundStarted += Server_RoundStarted;

            API.Diagnostics.Module.OnEnable(this);

            base.OnEnabled();
        }

        private void Server_RoundStarted()
        {
            this.GeneratePath(Map.Rooms.Where(x => x.Type == RoomType.LczChkpA || x.Type == RoomType.LczChkpB || x.Type == RoomType.EzGateA || x.Type == RoomType.EzGateB).ToArray());
        }

        private void GeneratePath(params Room[] rooms)
        {
            HashSet<API.Utilities.Room> checkedRooms = new HashSet<API.Utilities.Room>();

            Dictionary<API.Utilities.Room, Dictionary<API.Utilities.Room, API.Utilities.Room[]>> checkRooms = new Dictionary<API.Utilities.Room, Dictionary<API.Utilities.Room, API.Utilities.Room[]>>();

            foreach (var item in rooms)
            {
                var itemRoom = API.Utilities.Room.Get(item);
                checkedRooms.Add(itemRoom);
                checkRooms[itemRoom] = new Dictionary<API.Utilities.Room, API.Utilities.Room[]>()
                {
                    { itemRoom, itemRoom.Neighbors },
                };

                var lights = item.gameObject.GetComponentInChildren<PathLightController>();
                if (lights != null)
                {
                    lights.SetTargetSide(PathLightController.Side.CENTER);

                    lights.StartCoroutine(lights.DoAnimation());
                }
            }

            while (checkRooms.Any(x => x.Value.Count != 0))
            {
                foreach (var checkRoom in checkRooms)
                {
                    var targetRoom = checkRoom.Key;
                    var childRooms = checkRoom.Value;

                    var childRoomsCurrent = childRooms.ToArray();
                    childRooms.Clear();

                    foreach (var item in childRoomsCurrent.SelectMany(x => x.Value.Select(value => (x.Key, value))))
                    {
                        var parent = item.Key;
                        var room = item.value;
                        if (checkedRooms.Contains(room))
                            continue;
                        checkedRooms.Add(room);

                        var lights = room.ExiledRoom.gameObject.GetComponentInChildren<PathLightController>();
                        if (lights != null)
                        {
                            var myX = room.ExiledRoom.Position.x;
                            myX -= myX % 5;
                            var myZ = room.ExiledRoom.Position.z;
                            myZ -= myZ % 5;

                            var hisX = parent.ExiledRoom.Position.x;
                            hisX -= hisX % 5;
                            var hisZ = parent.ExiledRoom.Position.z;
                            hisZ -= hisZ % 5;

                            if (myX > hisX)
                                lights.SetTargetSide(PathLightController.Side.MINUS_X);
                            else if (myX < hisX)
                                lights.SetTargetSide(PathLightController.Side.PLUS_X);
                            else if (myZ > hisZ)
                                lights.SetTargetSide(PathLightController.Side.MINUS_Z);
                            else if (myZ < hisZ)
                                lights.SetTargetSide(PathLightController.Side.PLUS_Z);

                            lights.StartCoroutine(lights.DoAnimation());
                        }

                        childRooms.Add(room, room.Neighbors);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override void OnDisabled()
        {
            API.Diagnostics.Module.OnDisable(this);

            base.OnDisabled();
        }

        internal static PluginHandler Instance { get; private set; }

        private Harmony harmony;
    }
}
