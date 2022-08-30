// -----------------------------------------------------------------------
// <copyright file="PathLightHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using AdminToys;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using MEC;
using Mirror;
using Mistaken.API;
using Mistaken.API.Diagnostics;
using Mistaken.UnityPrefabs.PathLights;
using UnityEngine;

#pragma warning disable SA1118 // Parameter should not span multiple lines

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

        private readonly HashSet<PathLightController> runningControllers = new HashSet<PathLightController>();
        private bool enabled = false;
        private bool lockPath_Decontamination = false;
        private bool lockPath_Warhead = false;

        private Dictionary<PathLightController, PathLightController.Side> nukePath = null;
        private Dictionary<PathLightController, PathLightController.Side> decontaminationPath = null;

        private void Warhead_Detonated()
        {
            if (!this.enabled)
                return;
            this.lockPath_Warhead = false;
            this.RemovePathLightsFrom(ZoneType.LightContainment, ZoneType.HeavyContainment, ZoneType.Entrance);
        }

        private void Warhead_Stopping(Exiled.Events.EventArgs.StoppingEventArgs ev)
        {
            if (!this.enabled)
                return;
            this.lockPath_Warhead = false;
            this.ClearPath();
            if (this.lockPath_Decontamination)
                this.Map_AnnouncingDecontamination(new Exiled.Events.EventArgs.AnnouncingDecontaminationEventArgs(3, true));
        }

        private void Warhead_Starting(Exiled.Events.EventArgs.StartingEventArgs ev)
        {
            if (!this.enabled)
                return;
            this.lockPath_Warhead = true;
            this.EnablePath(this.nukePath);
        }

        private void Map_AnnouncingDecontamination(Exiled.Events.EventArgs.AnnouncingDecontaminationEventArgs ev)
        {
            if (!this.enabled)
                return;
            if (ev.Id == 3)
            {
                this.lockPath_Decontamination = true;
                if (!this.lockPath_Warhead)
                    this.EnablePath(this.decontaminationPath);
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

            foreach (var item in this.Controllers)
            {
                foreach (var script in item.Value.GetComponentsInChildren<LightSyncronizerScript>().ToArray())
                {
                    script.gameObject.AddComponent<PathLightSyncronizerScript>().Toy = script.Toy;
                    GameObject.Destroy(script);
                }

                this.Syncronizers[item.Key] = item.Key.gameObject.AddComponent<RoomPathLightSyncronizerScript>();
            }

            this.RunCoroutine(this.SynchronizationHandler(), nameof(this.SynchronizationHandler));

            this.EnablePath(this.nukePath);
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

        private void ClearPath()
        {
            this.runningControllers.Clear();
            foreach (var controller in this.Controllers.Values)
            {
                controller.StopAllCoroutines();
                controller.SetTargetSide(PathLightController.Side.NONE);
                controller.State = 0;
            }
        }

        private readonly Dictionary<Room, PathLightController> Controllers = new Dictionary<Room, PathLightController>();

        private void PreparePathLightDict()
        {
            this.Controllers.Clear();

            foreach (var room in Room.List)
            {
                var controller = room.GetComponentInChildren<PathLightController>();
                if (controller is null)
                    continue;
                this.Controllers[room] = controller;
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

                if (this.Controllers.TryGetValue(item, out var lights))
                {
                    tor[lights] = PathLightController.Side.SPECIAL;
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

                        if (this.Controllers.TryGetValue(room.ExiledRoom, out var lights))
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
            foreach (var controller in this.Controllers.Values)
                this.runningControllers.Remove(controller);

            MEC.Timing.CallDelayed(2.1f, () =>
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

        private readonly Dictionary<Room, RoomPathLightSyncronizerScript> Syncronizers = new Dictionary<Room, RoomPathLightSyncronizerScript>();

        private IEnumerator<float> SynchronizationHandler()
        {
            yield return Timing.WaitForSeconds(1f);

            while (Round.IsStarted)
            {
                yield return Timing.WaitForSeconds(1f);

                foreach (var player in RealPlayers.List)
                {
                    var room = API.Utilities.Room.Get(player.CurrentRoom);

                    if (room == null)
                    {
                        foreach (var item in this.Syncronizers.Values)
                            item.RemoveSubscriber(player);

                        continue;
                    }

                    // ToDo: sprawdziæ czy pomieszczenie siê woglê zmieni³o od ostatniego razu i nie aktualizowaæ jak tak
                    var otherRooms = room.FarNeighbors;

                    List<RoomPathLightSyncronizerScript> sync = new List<RoomPathLightSyncronizerScript>();
                    if (this.Syncronizers.TryGetValue(room.ExiledRoom, out var script))
                        sync.Add(script);
                    foreach (var item in otherRooms)
                    {
                        if (this.Syncronizers.TryGetValue(item.ExiledRoom, out script))
                            sync.Add(script);
                    }

                    foreach (var item in this.Syncronizers.Values.Where(x => !sync.Contains(x)))
                    {
                        item.RemoveSubscriber(player);
                    }

                    foreach (var item in sync)
                    {
                        item.AddSubscriber(player);
                    }
                }
            }
        }
    }

    internal class PathLightSyncronizerScript : MonoBehaviour
    {
        internal RoomPathLightSyncronizerScript controller;

        internal LightSourceToy Toy { get; set; }

        private Light light;

        internal readonly Dictionary<Player, float> LastStates = new Dictionary<Player, float>();

        private float lastState;

        private void Awake()
        {
            this.light = this.GetComponent<Light>();
        }

        private void LateUpdate()
        {
            if (this.Toy == null)
                return;

            if (this.Toy.NetworkLightColor != this.light.color)
            {
                this.Toy.NetworkLightColor = this.light.color;
                this.Toy.NetworkLightIntensity = this.light.intensity;
            }
            else if (this.light.intensity != this.lastState)
            {
                this.lastState = this.light.intensity;

                foreach (var item in this.controller.Subscribers)
                    this.UpdateSubscriber(item);
            }
        }

        internal void UpdateSubscriber(Player player)
        {
            if (this.LastStates[player] == this.lastState)
                return;

            this.SyncFor(player);

            this.LastStates[player] = this.lastState;
        }

        private void SyncFor(Player player)
        {
            PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
            PooledNetworkWriter writer2 = NetworkWriterPool.GetWriter();
            typeof(MirrorExtensions)
                .GetMethod("MakeCustomSyncWriter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .Invoke(null, new object[]
                {
                        this.Toy.netIdentity,
                        typeof(LightSourceToy),
                        null,
                        (Action<NetworkWriter>)CustomSyncVarGenerator,
                        writer,
                        writer2,
                });
            player.ReferenceHub.networkIdentity.connectionToClient.Send(new UpdateVarsMessage
            {
                netId = this.Toy.netIdentity.netId,
                payload = writer.ToArraySegment(),
            });
            NetworkWriterPool.Recycle(writer);
            NetworkWriterPool.Recycle(writer2);
            void CustomSyncVarGenerator(NetworkWriter targetWriter)
            {
                targetWriter.WriteUInt64(0UL);
                targetWriter.WriteUInt64(16UL);
                targetWriter.WriteSingle(this.lastState);
            }
        }
    }

    internal class RoomPathLightSyncronizerScript : MonoBehaviour
    {
        private PathLightSyncronizerScript[] lights = null;

        internal readonly HashSet<Player> Subscribers = new HashSet<Player>();

        public void AddSubscriber(Player player)
        {
            if (this.Subscribers.Contains(player))
                return;

            this.Subscribers.Add(player);
            foreach (var light in this.lights)
            {
                if (!light.LastStates.ContainsKey(player))
                    light.LastStates[player] = 0;
                light.UpdateSubscriber(player);
            }
        }

        public void RemoveSubscriber(Player player)
        {
            this.Subscribers.Remove(player);
        }

        private void Awake()
        {
            this.lights = this.GetComponentsInChildren<PathLightSyncronizerScript>();

            foreach (var item in this.lights)
                item.controller = this;
        }
    }
}
