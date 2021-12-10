// -----------------------------------------------------------------------
// <copyright file="MapModType.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Mistaken.CustomStructures
{
    [Flags]
    internal enum MapModType : ulong
    {
        SURFACE_GATEB_BRIDGE_FORWARD = 1ul << 0,
        SURFACE_GATEB_BRIDGE_LEFT = 1ul << 1,
        SURFACE_GATEB_BRIDGE_LEFT_BUNKER = 1ul << 2,
        SURFACE_GATEA_STAIRS_LOCK = 1ul << 4,
        SURFACE_GATEA_TUNNEL_CI_DOOR = 1ul << 5,
        SURFACE_GATEA_TUNNEL_CI_DOOR_LOCKED = 1ul << 6,
        SURFACE_GATEA_TUNNEL_ELEVATOR_DOOR = 1ul << 7,
        SURFACE_GATEA_MIDDLE_TOWER = 1ul << 8,

        // SURFACE_GATEA_TOWER_ARMORY = 1ul << 9,
    }
}
