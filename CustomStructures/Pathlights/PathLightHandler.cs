// -----------------------------------------------------------------------
// <copyright file="PathLightHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using MEC;
using Mirror;
using Mistaken.API;
using Mistaken.API.Diagnostics;
using Mistaken.UnityPrefabs.PathLights;
using UnityEngine;

// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable once IdentifierTypo
namespace Mistaken.CustomStructures.Pathlights
{
    /// <inheritdoc/>
    public class PathLightHandler : Module
    {
        /// <inheritdoc cref="Module"/>
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

        private readonly HashSet<PathLightController> runningControllers = new HashSet<PathLightController>();

        // ReSharper disable once IdentifierTypo
        private readonly Dictionary<Room, RoomPathLightSynchronizerScript> synchronizers = new Dictionary<Room, RoomPathLightSynchronizerScript>();
        private readonly Dictionary<Room, PathLightController> controllers = new Dictionary<Room, PathLightController>();

        private bool enabled;
        private bool lockPathDecontamination;
        private bool lockPathWarhead;

        private Dictionary<PathLightController, PathLightController.Side> nukePath;
        private Dictionary<PathLightController, PathLightController.Side> decontaminationPath;

        private void Warhead_Detonated()
        {
            if (!this.enabled)
                return;
            this.lockPathWarhead = false;
            this.RemovePathLightsFrom(ZoneType.LightContainment, ZoneType.HeavyContainment, ZoneType.Entrance);
        }

        private void Warhead_Stopping(Exiled.Events.EventArgs.StoppingEventArgs ev)
        {
            if (!this.enabled)
                return;
            this.lockPathWarhead = false;
            this.ClearPath();
            if (this.lockPathDecontamination)
                this.Map_AnnouncingDecontamination(new Exiled.Events.EventArgs.AnnouncingDecontaminationEventArgs(3, true));
        }

        private void Warhead_Starting(Exiled.Events.EventArgs.StartingEventArgs ev)
        {
            if (!this.enabled)
                return;
            this.lockPathWarhead = true;
            this.EnablePath(this.nukePath);
        }

        private void Map_AnnouncingDecontamination(Exiled.Events.EventArgs.AnnouncingDecontaminationEventArgs ev)
        {
            if (!this.enabled)
                return;

            switch (ev.Id)
            {
                case 3:
                    this.lockPathDecontamination = true;
                    if (!this.lockPathWarhead)
                        this.EnablePath(this.decontaminationPath);
                    break;

                case 6:
                    if (!this.lockPathWarhead)
                        this.ClearPath();
                    this.lockPathDecontamination = false;

                    Timing.CallDelayed(30, () => this.RemovePathLightsFrom(ZoneType.LightContainment));
                    break;
            }
        }

        private void Server_RoundStarted()
        {
            if (CustomStructuresHandler.UnknownAssets.Any(x => !(x.Meta.GetComponentInChildren<PathLightController>() is null)))
                this.enabled = true;
            else
            {
                this.enabled = false;
                return;
            }

            this.PreparePathLightDict();
            this.ClearPath();

            this.decontaminationPath = this.PreGeneratePath(Room.List.Where(x => x.Type == RoomType.LczChkpA || x.Type == RoomType.LczChkpB).ToArray());
            this.nukePath = this.PreGeneratePath(Room.List.Where(x => x.Type == RoomType.LczChkpA || x.Type == RoomType.LczChkpB || x.Type == RoomType.EzGateA || x.Type == RoomType.EzGateB).ToArray());

            foreach (var item in this.controllers)
            {
                foreach (var script in item.Value.GetComponentsInChildren<LightSynchronizerScript>().ToArray())
                {
                    script.gameObject.AddComponent<PathLightSynchronizerScript>().Toy = script.Toy;
                    Object.Destroy(script);
                }

                this.synchronizers[item.Key] = item.Key.gameObject.AddComponent<RoomPathLightSynchronizerScript>();
            }

            this.RunCoroutine(this.SynchronizationHandler(), nameof(this.SynchronizationHandler));

            // this.EnablePath(this.nukePath);
        }

        private void RemovePathLightsFrom(params ZoneType[] zones)
        {
            foreach (var item in Object.FindObjectsOfType<PathLightController>())
            {
                var room = item.GetComponentInParent<Room>();

                if (room is null)
                    continue;

                var zone = room.Zone;

                // ReSharper disable once SimplifyLinqExpressionUseAll
                if (!zones.Any(x => x == zone))
                    continue;

                foreach (var nid in item.GetComponentsInChildren<NetworkIdentity>())
                    NetworkServer.Destroy(nid.gameObject);

                this.synchronizers.Remove(room);
                this.controllers.Remove(room);

                Object.Destroy(item.gameObject);
            }
        }

        private void ClearPath()
        {
            this.runningControllers.Clear();
            foreach (var controller in this.controllers.Values)
            {
                controller.StopAllCoroutines();
                controller.SetTargetSide(PathLightController.Side.NONE);
                controller.State = 0;
            }
        }

        private void PreparePathLightDict()
        {
            this.controllers.Clear();

            foreach (var room in Room.List)
            {
                var controller = room.GetComponentInChildren<PathLightController>();
                if (controller is null)
                    continue;
                this.controllers[room] = controller;
            }
        }

        private Dictionary<PathLightController, PathLightController.Side> PreGeneratePath(params Room[] rooms)
        {
            var tor = new Dictionary<PathLightController, PathLightController.Side>();

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

                if (this.controllers.TryGetValue(item, out var lights))
                {
                    tor[lights] = PathLightController.Side.SPECIAL;
                }
            }

            while (checkRooms.Any(x => x.Value.Count != 0))
            {
                foreach (var checkRoom in checkRooms)
                {
                    // var targetRoom = checkRoom.Key;
                    var childRooms = checkRoom.Value;

                    var childRoomsCurrent = childRooms.ToArray();
                    childRooms.Clear();

                    foreach (var (parent, room) in childRoomsCurrent.SelectMany(x => x.Value.Select(value => (x.Key, value))))
                    {
                        if (checkedRooms.Contains(room))
                            continue;

                        checkedRooms.Add(room);

                        if (this.controllers.TryGetValue(room.ExiledRoom, out var lights))
                        {
                            if (room.ExiledRoom.Type == RoomType.HczChkpA || room.ExiledRoom.Type == RoomType.HczChkpB)
                                tor[lights] = PathLightController.Side.SPECIAL;
                            else
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
                                    tor[lights] = PathLightController.Side.MINUS_X;
                                else if (myX < hisX)
                                    tor[lights] = PathLightController.Side.PLUS_X;
                                else if (myZ > hisZ)
                                    tor[lights] = PathLightController.Side.MINUS_Z;
                                else if (myZ < hisZ)
                                    tor[lights] = PathLightController.Side.PLUS_Z;
                            }
                        }

                        childRooms.Add(room, room.Neighbors);
                    }
                }
            }

            return tor;
        }

        private void EnablePath(Dictionary<PathLightController, PathLightController.Side> path)
        {
            foreach (var controller in this.controllers.Values)
                this.runningControllers.Remove(controller);

            Timing.CallDelayed(2.1f, () =>
            {
                foreach (var data in path.ToArray())
                {
                    var controller = data.Key;
                    if (controller.gameObject == null)
                    {
                        path.Remove(controller);
                        continue;
                    }

                    controller.State = 0;
                    controller.SetTargetSide(data.Value);
                    if (controller.TargetSide != PathLightController.Side.NONE)
                    {
                        this.runningControllers.Add(controller);
                        Timing.RunCoroutine(this.DoAnimation(controller));
                    }
                }
            });
        }

        private IEnumerator<float> DoAnimation(PathLightController me)
        {
            yield return Timing.WaitForSeconds(1f);

            while (this.runningControllers.Contains(me))
                yield return Timing.WaitForSeconds(me.DoAnimationSingleCycle());
        }

        private IEnumerator<float> SynchronizationHandler()
        {
            yield return Timing.WaitForSeconds(1f);

            Dictionary<Player, Room> lastRooms = new Dictionary<Player, Room>();

            while (Round.IsStarted)
            {
                yield return Timing.WaitForSeconds(1f);

                foreach (var player in RealPlayers.List)
                {
                    var curRoom = player.CurrentRoom;

                    if (lastRooms.TryGetValue(player, out var lastRoom) && lastRoom == curRoom)
                        continue; // Skip, room didn't change since last update

                    lastRooms[player] = curRoom;

                    var room = API.Utilities.Room.Get(curRoom);

                    // Don't even check for Surface, Other because there are no lights
                    if (room == null ||
                        room.ExiledRoom.Zone == ZoneType.Surface ||
                        room.ExiledRoom.Zone == ZoneType.Unspecified)
                    {
                        foreach (var item in this.synchronizers.Values)
                            item.RemoveSubscriber(player);

                        continue;
                    }

                    var otherRooms = room.FarNeighbors;

                    HashSet<RoomPathLightSynchronizerScript> toSync = NorthwoodLib.Pools.HashSetPool<RoomPathLightSynchronizerScript>.Shared.Rent();

                    if (this.synchronizers.TryGetValue(room.ExiledRoom, out var script))
                        toSync.Add(script);

                    // ReSharper disable once LoopCanBeConvertedToQuery
                    foreach (var item in otherRooms)
                    {
                        if (this.synchronizers.TryGetValue(item.ExiledRoom, out script))
                            toSync.Add(script);
                    }

                    foreach (var item in this.synchronizers.Values.Where(x => !toSync.Contains(x)))
                        item.RemoveSubscriber(player);

                    foreach (var item in toSync)
                        item.AddSubscriber(player);

                    NorthwoodLib.Pools.HashSetPool<RoomPathLightSynchronizerScript>.Shared.Return(toSync);
                }
            }
        }
    }
}
