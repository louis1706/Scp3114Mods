﻿// <copyright file="Log.cs" company="Redforce04#4091">
// Copyright (c) Redforce04. All rights reserved.
// </copyright>
// -----------------------------------------
//    Solution:         Scp3114Mods
//    Project:          Scp3114Mods
//    FileName:         EventHandlers.cs
//    Author:           Redforce04#4091
//    Revision Date:    10/29/2023 5:24 PM
//    Created Date:     10/29/2023 5:24 PM
// -----------------------------------------

using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Usables;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp3114;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Events;
using UnityEngine;

namespace Scp3114Mods;

public class EventHandlers
{

    [PluginEvent(ServerEventType.PlayerDeath)]
    internal void OnDeath(PlayerDeathEvent ev)
    {
        if (ev.DamageHandler is not Scp3114DamageHandler damageHandler)
            return;
        Scp3114Mods.Singleton.AddCooldownForPlayer(Player.Get(damageHandler.Attacker.PlayerId), false);
    }

    [PluginEvent(ServerEventType.PlayerChangeItem)]
    internal void ShowMessage(PlayerChangeItemEvent ev)
    {
        try
        {
            if (ev.Player.Role is RoleTypeId.Scp3114)
            {
                var item = ev.Player.Items.FirstOrDefault(x => x.ItemSerial == ev.NewItem);
                if (item is null)
                    return;
                if (Scp3114Mods.Singleton.Config.FakeFiringAllowed && item is Firearm)
                {
                    ev.Player.ReceiveHint("You can fake shoot weapons by pressing [T].", 4f);
                    return;
                }

                if (Scp3114Mods.Singleton.Config.FakeUsableInteractions && item is UsableItem)
                {
                    ev.Player.ReceiveHint("You can fake use items by right clicking with your mouse", 4f);
                    return;
                }
            }
        }
        catch (Exception e)
        {
            Log.Debug("An error has occured.");
        }
    }
    [PluginEvent(ServerEventType.RoundStart)]
    internal void OnRoundStart()
    {
        Scp3114Mods.Singleton._clearCooldownList();
    }

    public void OnPlayerThrowItem(Player ply , ushort itemSerial, bool tryThrow)
    {
        if (!Scp3114Mods.Singleton.Config.FakeFiringAllowed)
            return;
        if (ply.Role == RoleTypeId.Scp3114)
        {
            if (!ply.ReferenceHub.inventory.UserInventory.Items.ContainsKey(itemSerial))
                return;
            var item = ply.ReferenceHub.inventory.UserInventory.Items[itemSerial];
            if (item is Firearm firearm)
            {
                if (firearm.Status.Ammo == 0)
                    processDryFiring(firearm);
                else
                    processFiring(firearm);
            }
            
            return;
        }

        return;
    }

    private void processDryFiring(Firearm firearm)
    {
        if (Scp3114Mods.Singleton.Config.Debug)
            Log.Debug("Fake dry firing gun.");
        // Dry Fire
        switch (firearm.ActionModule)
        {
            case AutomaticAction automatic:
                firearm.ServerSendAudioMessage((byte)(automatic)._dryfireClip);
                break;
            case PumpAction pump:
                firearm.ServerSendAudioMessage((byte)(pump)._dryfireClip);
                break;
            case DoubleAction doubleAction:
                firearm.ServerSendAudioMessage((byte)(doubleAction)._dryfireClip);
                break;
        }
    }
    private void processFiring(Firearm firearm)
    {
        if (Scp3114Mods.Singleton.Config.Debug)
            Log.Debug("Fake firing gun.");
        switch (firearm.ActionModule)
        {
            case AutomaticAction automatic:
                _fakeFireAutomatic(firearm, automatic);
                break;
            case PumpAction pump:
                _fakeFirePump(firearm, pump);
                break;
            case DisruptorAction disruptor:
                _fakeFireDisruptor(firearm, disruptor);
                break;
            case DoubleAction doubleAction:
                _fakeFireDoubleAction(firearm, doubleAction);
                break;
        }
    }

    private void _fakeFirePump(Firearm firearm, PumpAction pump)
    {
        if (firearm.Owner.HasBlock(BlockedInteraction.ItemPrimaryAction))
        {
            return;
        }
        if (pump.ChamberedRounds == 0 || firearm.Status.Ammo == 0)
        {
            pump.ServerResync();
            return;
        }
        if (pump._lastShotStopwatch.Elapsed.TotalSeconds < (double)pump.TimeBetweenShots || pump._pumpStopwatch.Elapsed.TotalSeconds < (double)pump.PumpingTime)
        {
            return;
        }
        pump.LastFiredAmount = 0;
        int num = pump.AmmoUsage;
        while (num > 0 && pump.ChamberedRounds > 0 && pump._firearm.Status.Ammo > 0)
        {
            num--;
            pump.ChamberedRounds--;
            pump.CockedHammers--;
            pump.LastFiredAmount++;
            if (pump.ChamberedRounds > 0)
            {
                pump._lastShotStopwatch.Restart();
            }
            firearm.Status = new FirearmStatus((byte)(firearm.Status.Ammo - 1), firearm.Status.Flags, firearm.Status.Attachments);
            firearm.ServerSendAudioMessage((byte)(pump.ShotSoundId + pump.ChamberedRounds));
            firearm.ServerSideAnimator.Play(FirearmAnimatorHashes.Fire);
            if (pump.ChamberedRounds == 0 && firearm.Status.Ammo > 0 && !firearm.IsLocalPlayer)
            {
                pump._pumpStopwatch.Restart();
                firearm.AnimSetTrigger(pump._pumpAnimHash);
                break;
            }
        }
    }
    private void _fakeFireDoubleAction(Firearm firearm, DoubleAction doubleA)
    {
        if (firearm.Owner.HasBlock(BlockedInteraction.ItemPrimaryAction))
        {
            return;
        }
        if ((doubleA.ServerTriggerReady || firearm.IsLocalPlayer) && firearm.Status.Ammo > 0)
        {
            if (Scp3114Mods.Singleton.Config.FakeFiringUsesAmmo)
            {
                firearm.Status = new FirearmStatus((byte)(firearm.Status.Ammo - 1), firearm.Status.Flags, firearm.Status.Attachments);
            } 
            doubleA._nextAllowedShot = Time.timeSinceLevelLoad + doubleA._cooldownAfterShot;
            firearm.ServerSendAudioMessage((byte)firearm.AttachmentsValue(AttachmentParam.ShotClipIdOverride));
            firearm.ServerSideAnimator.Play(FirearmAnimatorHashes.Fire);
            return;
        }
    }

    private void _fakeFireAutomatic(Firearm firearm, AutomaticAction automatic)
    {
        if (firearm.Owner.HasBlock(BlockedInteraction.ItemPrimaryAction))
            return;
        if (firearm.Status.Ammo < automatic._ammoConsumption)
            return;
        if (!automatic.ServerCheckFirerate())
            return;
        if (!automatic.ModulesReady)
        {
            firearm.Owner.gameConsoleTransmission.SendToClient(
                $"Shot rejected, ammoManager={firearm.AmmoManagerModule.Standby}, equipperModule={firearm.EquipperModule.Standby}, adsModule={firearm.AdsModule.Standby}",
                "gray");
            return;
        }

        FirearmStatusFlags firearmStatusFlags = firearm.Status.Flags;
        if (Scp3114Mods.Singleton.Config.FakeFiringUsesAmmo)
        {
            if (firearm.Status.Ammo - automatic._ammoConsumption < automatic._ammoConsumption &&
                automatic._boltTravelTime == 0f)
                firearmStatusFlags &= ~FirearmStatusFlags.Chambered;
            firearm.Status = new FirearmStatus((byte)(firearm.Status.Ammo - automatic._ammoConsumption), firearmStatusFlags, firearm.Status.Attachments);
        }

        firearm.ServerSendAudioMessage(automatic.ShotClipId);
        firearm.ServerSideAnimator.Play(FirearmAnimatorHashes.Fire);
        return;
    }

    private void _fakeFireDisruptor(Firearm firearm, DisruptorAction disruptor)
    {
        if (!firearm.IsLocalPlayer && disruptor.TimeSinceLastShot < 1.5f)
            return;
        if (!disruptor.ModulesReady)
        {
            firearm.Owner.gameConsoleTransmission.SendToClient(
                $"Shot rejected, ammoManager={firearm.AmmoManagerModule.Standby}, equipperModule={firearm.EquipperModule.Standby}, adsModule={firearm.AdsModule.Standby}",
                "gray");
            return;
        }

        firearm.Status = new FirearmStatus((byte)(firearm.Status.Ammo - 1), firearm.Status.Flags,
            firearm.Status.Attachments);
        firearm.ServerSendAudioMessage(0);
        if (!firearm.IsLocalPlayer)
            disruptor._lastShotTime = disruptor.CurTime;
        firearm.ServerSideAnimator.Play(FirearmAnimatorHashes.Fire, 0, disruptor.ShotDelay / 2.2667f);
    }
}