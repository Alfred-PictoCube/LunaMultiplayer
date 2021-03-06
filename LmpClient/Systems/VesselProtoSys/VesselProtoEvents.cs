﻿using LmpClient.Base;
using LmpClient.Systems.Lock;
using LmpClient.Systems.SettingsSys;
using LmpClient.Systems.ShareScienceSubject;
using LmpClient.Systems.VesselDockSys;
using LmpClient.Systems.VesselRemoveSys;
using LmpClient.Utilities;
using LmpClient.VesselUtilities;
using System;
using System.Collections.Generic;

namespace LmpClient.Systems.VesselProtoSys
{
    public class VesselProtoEvents : SubSystem<VesselProtoSystem>
    {
        public static readonly List<Guid> QueuedVessels = new List<Guid>();

        /// <summary>
        /// Sends our vessel just when we start the flight
        /// </summary>
        public void FlightReady()
        {
            if (VesselCommon.IsSpectating || FlightGlobals.ActiveVessel == null || FlightGlobals.ActiveVessel.id == Guid.Empty)
                return;
            
            System.MessageSender.SendVesselMessage(FlightGlobals.ActiveVessel);
        }

        /// <summary>
        /// Called when a vessel is initiated.
        /// </summary>
        public void VesselInitialized(Vessel vessel, bool fromShipAssembly)
        {
            if (vessel == null) return;

            //The vessel is being created by the loader
            if (VesselLoader.CurrentlyLoadingVesselId == vessel.id || fromShipAssembly)
                return;
            
            //This happens when the vessel you're spectating crashes
            if (VesselCommon.IsSpectating)
            {
                VesselRemoveSystem.Singleton.KillVessel(vessel.id, true, "Tried to create a new vessel while spectating");
                return;
            }

            //It's a debris vessel that we made it
            if (!LockSystem.LockQuery.UnloadedUpdateLockExists(vessel.id))
            {
                //We delay it a bit because we must wait until the vessel is named correctly and so on.
                CoroutineUtil.StartDelayedRoutine("VesselInitialized", ()=> System.MessageSender.SendVesselMessage(vessel), 0.5f);
                LockSystem.Singleton.AcquireUnloadedUpdateLock(vessel.id, true);
            }
        }

        /// <summary>
        /// Event called when switching scene and before reaching the other scene
        /// </summary>
        internal void OnSceneRequested(GameScenes requestedScene)
        {
            if (HighLogic.LoadedSceneIsFlight && requestedScene != GameScenes.FLIGHT && !VesselCommon.IsSpectating)
            {
                //When quitting flight send the vessel one last time
                VesselProtoSystem.Singleton.MessageSender.SendVesselMessage(FlightGlobals.ActiveVessel);
            }
        }

        /// <summary>
        /// Triggered when the vessel parts change.
        /// CAUTION!: When staging this method can be called a lot of times!
        /// Also, this method is called when docking/undocking and when reloading a vessel
        /// </summary>
        public void VesselPartCountChanged(Vessel vessel)
        {
            if (vessel == null) return;

            //The vessel is being created by the loader
            if (VesselLoader.CurrentlyLoadingVesselId == vessel.id)
                return;
            
            //When a vessel docks it's part count changes but we must not relay it as it's handled by the vessel dock system
            if (CurrentDockEvent.DominantVesselId == vessel.id || CurrentUndockEvent.UndockingVesselId == vessel.id)
                return;

            //This event is called when the vessel is being created and we don't want to send protos of vessels we don't own or while our vessel is not 100% loaded (FlightReady)
            if (vessel.vesselSpawning) return;

            //Vessel is scheduled to be killed so ignore this
            if (VesselRemoveSystem.Singleton.VesselWillBeKilled(vessel.id)) return;

            //We are spectating and the vessel has been modified so trigger a reload
            if (VesselCommon.IsSpectating && FlightGlobals.ActiveVessel&& FlightGlobals.ActiveVessel.id == vessel.id && vessel.protoVessel.protoPartSnapshots.Count != FlightGlobals.ActiveVessel.Parts.Count)
            {
                VesselLoader.LoadVessel(vessel.protoVessel);
                return;
            }

            if (!LockSystem.LockQuery.UpdateLockExists(vessel.id))
            {
                LockSystem.Singleton.AcquireUnloadedUpdateLock(vessel.id, true);
                VesselProtoSystem.Singleton.MessageSender.SendVesselMessage(vessel);
            }

            if (LockSystem.LockQuery.UpdateLockBelongsToPlayer(vessel.id, SettingsSystem.CurrentSettings.PlayerName))
            {                
                //This method can be called a lot of times during staging (for every part that decouples)
                //For this reason we wait 0.5 seconds so we send all the changes at once.
                if (QueuedVessels.Contains(vessel.id)) return;

                QueuedVessels.Add(vessel.id);
                CoroutineUtil.StartDelayedRoutine("QueueVesselMessageAsPartsChanged", () => QueueNewVesselChange(vessel), 0.5f);
                VesselProtoSystem.Singleton.MessageSender.SendVesselMessage(vessel);
            }
        }

        private static void QueueNewVesselChange(Vessel vessel)
        {
            if (vessel == null) return;
            QueuedVessels.Remove(vessel.id);
            VesselProtoSystem.Singleton.MessageSender.SendVesselMessage(vessel);
        }
        
        /// <summary>
        /// Triggered when transmitting science. Science experiment is stored in the vessel so send the definition to the server
        /// </summary>
        public void TriggeredDataTransmission(ScienceData science, Vessel vessel, bool data)
        {
            if (FlightGlobals.ActiveVessel != null && !VesselCommon.IsSpectating)
            {                
                //We must send the science subject aswell!
                var subject = ResearchAndDevelopment.GetSubjectByID(science.subjectID);
                if (subject != null)
                {
                    LunaLog.Log("Detected a experiment transmission. Sending vessel definition to the server");
                    System.MessageSender.SendVesselMessage(FlightGlobals.ActiveVessel);

                    ShareScienceSubjectSystem.Singleton.MessageSender.SendScienceSubjectMessage(subject);
                }
            }
        }

        /// <summary>
        /// Triggered when storing science. Science experiment is stored in the vessel so send the definition to the server
        /// </summary>
        public void ExperimentStored(ScienceData science)
        {
            if (FlightGlobals.ActiveVessel != null && !VesselCommon.IsSpectating)
            {
                //We must send the science subject aswell!
                var subject = ResearchAndDevelopment.GetSubjectByID(science.subjectID);
                if (subject != null)
                {
                    LunaLog.Log("Detected a experiment stored. Sending vessel definition to the server");
                    System.MessageSender.SendVesselMessage(FlightGlobals.ActiveVessel);

                    ShareScienceSubjectSystem.Singleton.MessageSender.SendScienceSubjectMessage(subject);
                }
            }
        }

        /// <summary>
        /// Triggered when resetting a experiment. Science experiment is stored in the vessel so send the definition to the server
        /// </summary>
        public void ExperimentReset(Vessel data)
        {
            if (FlightGlobals.ActiveVessel != null && !VesselCommon.IsSpectating)
            {
                LunaLog.Log("Detected a experiment reset. Sending vessel definition to the server");
                System.MessageSender.SendVesselMessage(FlightGlobals.ActiveVessel);
            }
        }
    }
}
