﻿// <copyright file="Log.cs" company="Redforce04#4091">
// Copyright (c) Redforce04. All rights reserved.
// </copyright>
// -----------------------------------------
//    Solution:         Scp3114Mods
//    Project:          Scp3114Mods
//    FileName:         StranglingPlayerArgs.cs
//    Author:           Redforce04#4091
//    Revision Date:    11/01/2023 1:06 PM
//    Created Date:     11/01/2023 1:06 PM
// -----------------------------------------

using PluginAPI.Core;

namespace Scp3114Mods.API.EventArgs;

public sealed class StranglingPlayerArgs
{
    /// <summary>
    /// Creates a new instance of <see cref="StranglingPlayerArgs"/>.
    /// </summary>
    /// <param name="attacker">The player who is Scp3114.</param>
    /// <param name="target">The player being strangled.</param>
    /// <param name="isAllowed">Is the event allowed to occur.</param>
    public StranglingPlayerArgs(Player attacker, Player target, bool isAllowed = true)
    {
        Attacker = attacker;
        Target = target;
        IsAllowed = isAllowed;
    }
		
    /// <summary>
    /// The player who is Scp3114.
    /// </summary>
    public Player Attacker { get; set; }
		
    /// <summary>
    /// The player being strangled.
    /// </summary>
    public Player Target { get; set; }
		
    /// <summary>
    /// Is the event allowed to occur.
    /// </summary>
    public bool IsAllowed { get; set; }
}