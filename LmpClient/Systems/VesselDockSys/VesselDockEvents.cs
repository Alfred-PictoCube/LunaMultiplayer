﻿using System;
using System.Collections;
using System.Linq;
using LmpClient.Base;
using LmpClient.Systems.Lock;
using LmpClient.Systems.VesselProtoSys;
using LmpClient.Systems.VesselRemoveSys;
using LmpClient.Systems.Warp;
using LmpClient.Utilities;
using LmpClient.VesselUtilities;
using LmpCommon.Time;
using UnityEngine;

namespace LmpClient.Systems.VesselDockSys
{
    public class VesselDockEvents : SubSystem<VesselDockSystem>
    {
        private static bool _ownDominantVessel;
        
        /// <summary>
        /// Called just before the docking sequence starts
        /// </summary>
        public void OnVesselDocking(uint vessel1PersistentId, uint vessel2PersistentId)
        {
            if (!FlightGlobals.PersistentVesselIds.TryGetValue(vessel1PersistentId, out var vessel1) ||
                !FlightGlobals.PersistentVesselIds.TryGetValue(vessel2PersistentId, out var vessel2))
                return;

            if (vessel1.isEVA || vessel2.isEVA) return;

            CurrentDockEvent.DockingTime = LunaNetworkTime.UtcNow;

            var dominantVessel = Vessel.GetDominantVessel(vessel1, vessel2);
            CurrentDockEvent.DominantVesselId = dominantVessel.id;

            var weakVessel = dominantVessel == vessel1 ? vessel2 : vessel1;
            CurrentDockEvent.WeakVesselId = weakVessel.id;

            _ownDominantVessel = FlightGlobals.ActiveVessel == dominantVessel;
        }

        public void OnDockingComplete(GameEvents.FromToAction<Part, Part> data)
        {
            LunaLog.Log(_ownDominantVessel ? $"[LMP]: Docking finished! We own the dominant vessel {CurrentDockEvent.DominantVesselId}" :
                $"[LMP]: Docking finished! We DON'T own the dominant vessel {CurrentDockEvent.DominantVesselId}");

            JumpIfVesselOwnerIsInFuture(CurrentDockEvent.DominantVesselId);

            if (_ownDominantVessel)
            {
                System.MessageSender.SendDockInformation(CurrentDockEvent.WeakVesselId, FlightGlobals.ActiveVessel, WarpSystem.Singleton.CurrentSubspace);
                VesselProtoSystem.Singleton.MessageSender.SendVesselMessage(FlightGlobals.ActiveVessel);
            }
            else
            {
                CoroutineUtil.StartDelayedRoutine("OnDockingComplete", () => System.MessageSender.SendDockInformation(CurrentDockEvent.WeakVesselId,
                            FlightGlobals.ActiveVessel, WarpSystem.Singleton.CurrentSubspace), 3);
            }

            VesselRemoveSystem.Singleton.MessageSender.SendVesselRemove(CurrentDockEvent.WeakVesselId, false);
        }

        /// <summary>
        /// Event called just when the undocking starts
        /// </summary>
        public void UndockingStart(Part undockingPart)
        {
            CurrentUndockEvent.UndockingVesselId = undockingPart.vessel.id;
        }

        /// <summary>
        /// Event called after the undocking is completed and we have the 2 final vessels
        /// </summary>
        public void UndockingComplete(Vessel vessel1, Vessel vessel2)
        {
            if (VesselCommon.IsSpectating) return;

            LunaLog.Log("Undock detected!");

            VesselProtoSystem.Singleton.MessageSender.SendVesselMessage(vessel1);
            VesselProtoSystem.Singleton.MessageSender.SendVesselMessage(vessel2);

            //Release the locks of the vessel we are not in
            var crewToReleaseLocks = FlightGlobals.ActiveVessel == vessel1
                ? vessel2.GetVesselCrew().Select(c => c.name)
                : vessel1.GetVesselCrew().Select(c => c.name);

            var vesselToRelease = FlightGlobals.ActiveVessel == vessel1 ? vessel2 : vessel1;
            LockSystem.Singleton.ReleaseAllVesselLocks(crewToReleaseLocks, vesselToRelease.id);

            LunaLog.Log($"Undocking finished. Vessels: {vessel1.id} and {vessel2.id}");
            CurrentDockEvent.Reset();
        }

        #region Private

        /// <summary>
        /// Jumps to the subspace of the controller vessel in case he is more advanced in time
        /// </summary>
        private static void JumpIfVesselOwnerIsInFuture(Guid vesselId)
        {
            var dominantVesselOwner = LockSystem.LockQuery.GetControlLockOwner(vesselId);
            if (dominantVesselOwner != null)
            {
                var dominantVesselOwnerSubspace = WarpSystem.Singleton.GetPlayerSubspace(dominantVesselOwner);
                WarpSystem.Singleton.WarpIfSubspaceIsMoreAdvanced(dominantVesselOwnerSubspace);
            }
        }

        /// <summary>
        /// Here we wait until we fully switched to the dominant vessel and THEN we send the vessel dock information.
        /// We wait 5 seconds before sending the data to give time to the dominant vessel to detect the dock
        /// </summary>
        private static IEnumerator WaitUntilWeSwitchedThenSendDockInfo(Guid weakId, uint weakPersistantId, uint dominantPersistentId, int secondsToWait = 5)
        {
            var start = LunaComputerTime.UtcNow;
            var currentSubspaceId = WarpSystem.Singleton.CurrentSubspace;
            var waitInterval = new WaitForSeconds(0.5f);

            while (FlightGlobals.ActiveVessel && FlightGlobals.ActiveVessel.persistentId != dominantPersistentId && LunaComputerTime.UtcNow - start < TimeSpan.FromSeconds(30))
            {
                yield return waitInterval;
            }

            if (FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.persistentId == dominantPersistentId)
            {
                /* We are NOT the dominant vessel so wait 5 seconds so the dominant vessel detects the docking.
                 * If we send the vessel definition BEFORE the dominant detects it, then the dominant won't be able
                 * to undock properly as he will think that he is the weak vessel.
                 */

                yield return new WaitForSeconds(secondsToWait);

                FlightGlobals.ActiveVessel.BackupVessel();
                LunaLog.Log($"[LMP]: Sending dock info to the server! Final dominant vessel parts {FlightGlobals.ActiveVessel.protoVessel.protoPartSnapshots.Count}");

                System.MessageSender.SendDockInformation(weakId, FlightGlobals.ActiveVessel, currentSubspaceId, FlightGlobals.ActiveVessel.protoVessel);
            }
        }

        #endregion

    }
}
