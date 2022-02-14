﻿// -----------------------------------------------------------------------
// <copyright file="PathLightHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using MEC;
using Mirror;
using Mistaken.API.Diagnostics;
using Mistaken.UnityPrefabs;
using Mistaken.UnityPrefabs.PathLights;
using UnityEngine;

namespace Mistaken.CustomStructures
{
    /// <inheritdoc/>
    public class PathLightHandler : Module
    {
        /// <inheritdoc cref="Module.Module(IPlugin{IConfig})"/>
        public PathLightHandler(IPlugin<IConfig> plugin)
            : base(plugin)
        {
        }

        /// <inheritdoc/>
        public override string Name => "PathLightHandler";

        /// <inheritdoc/>
        public override void OnDisable()
        {
            Exiled.Events.Handlers.Server.RoundStarted -= this.Server_RoundStarted;
            Exiled.Events.Handlers.Map.AnnouncingDecontamination -= this.Map_AnnouncingDecontamination;
            Exiled.Events.Handlers.Warhead.Starting -= this.Warhead_Starting;
            Exiled.Events.Handlers.Warhead.Stopping -= this.Warhead_Stopping;
            Exiled.Events.Handlers.Warhead.Detonated -= this.Warhead_Detonated;
        }

        /// <inheritdoc/>
        public override void OnEnable()
        {
            Exiled.Events.Handlers.Server.RoundStarted += this.Server_RoundStarted;
            Exiled.Events.Handlers.Map.AnnouncingDecontamination += this.Map_AnnouncingDecontamination;
            Exiled.Events.Handlers.Warhead.Starting += this.Warhead_Starting;
            Exiled.Events.Handlers.Warhead.Stopping += this.Warhead_Stopping;
            Exiled.Events.Handlers.Warhead.Detonated += this.Warhead_Detonated;
        }

        private bool lockPath_Decontamination = false;
        private bool lockPath_Warhead = false;

        private void Warhead_Detonated()
        {
            this.lockPath_Warhead = false;
            this.RemovePathLightsFrom(ZoneType.LightContainment, ZoneType.HeavyContainment, ZoneType.Entrance);
        }

        private void Warhead_Stopping(Exiled.Events.EventArgs.StoppingEventArgs ev)
        {
            this.lockPath_Warhead = false;
            this.ClearPath();
            if (this.lockPath_Decontamination)
                this.Map_AnnouncingDecontamination(new Exiled.Events.EventArgs.AnnouncingDecontaminationEventArgs(3, true));
        }

        private void Warhead_Starting(Exiled.Events.EventArgs.StartingEventArgs ev)
        {
            this.lockPath_Warhead = true;
            this.GeneratePath(Map.Rooms.Where(x => x.Type == RoomType.LczChkpA || x.Type == RoomType.LczChkpB || x.Type == RoomType.EzGateA || x.Type == RoomType.EzGateB).ToArray());
        }

        private void Map_AnnouncingDecontamination(Exiled.Events.EventArgs.AnnouncingDecontaminationEventArgs ev)
        {
            if (ev.Id == 3)
            {
                this.lockPath_Decontamination = true;
                if (!this.lockPath_Warhead)
                    this.GeneratePath(Map.Rooms.Where(x => x.Type == RoomType.LczChkpA || x.Type == RoomType.LczChkpB).ToArray());
            }
            else if (ev.Id == 6)
            {
                if (!this.lockPath_Warhead)
                    this.ClearPath();
                this.lockPath_Decontamination = false;

                MEC.Timing.CallDelayed(30, () => this.RemovePathLightsFrom(ZoneType.LightContainment));
            }
        }

        private void Server_RoundStarted()
        {
            // this.GeneratePath(Map.Rooms.Where(x => x.Type == RoomType.LczChkpA || x.Type == RoomType.LczChkpB || x.Type == RoomType.EzGateA || x.Type == RoomType.EzGateB).ToArray());
            this.IniPath();
        }

        private void RemovePathLightsFrom(params ZoneType[] zones)
        {
            foreach (var item in GameObject.FindObjectsOfType<PathLightController>())
            {
                var zone = item.GetComponentInParent<Room>()?.Zone;
                if (zone is null)
                    continue;

                if (zones.Any(x => x == zone))
                {
                    foreach (var nid in item.GetComponentsInChildren<NetworkIdentity>())
                        NetworkServer.Destroy(nid.gameObject);

                    GameObject.Destroy(item.gameObject);
                }
            }
        }

        private void IniPath()
        {
            foreach (var item in GameObject.FindObjectsOfType<PathLightController>().Where(x => !(x.GetComponentInParent<Room>() is null)))
                item.StartCoroutine(item.DoAnimation());
        }

        private void ClearPath()
        {
            foreach (var item in GameObject.FindObjectsOfType<PathLightController>())
                item.SetTargetSide(PathLightController.Side.NONE);
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
                    lights.SetTargetSide(PathLightController.Side.SPECIAL);
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
                        }

                        childRooms.Add(room, room.Neighbors);
                    }
                }
            }
        }
    }
}
