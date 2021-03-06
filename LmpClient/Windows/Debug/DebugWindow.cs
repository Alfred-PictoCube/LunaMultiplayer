﻿using LmpClient.Base;
using LmpClient.Network;
using LmpClient.Systems.SafetyBubble;
using LmpClient.Systems.TimeSync;
using LmpClient.Systems.VesselPositionSys;
using LmpClient.Systems.Warp;
using LmpCommon.Enums;
using LmpCommon.Time;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LmpClient.Windows.Debug
{
    public partial class DebugWindow : Window<DebugWindow>
    {
        #region Fields

        private static readonly StringBuilder StringBuilder = new StringBuilder();
        private static readonly List<Tuple<Guid, string>> VesselProtoStoreData = new List<Tuple<Guid, string>>();

        private const float DisplayUpdateInterval = .2f;
        private const float WindowHeight = 400;
        private const float WindowWidth = 650;

        private static bool _displayFast;
        private static string _vectorText;
        private static string _positionText;
        private static string _positionVesselsText;
        private static string _orbitText;
        private static string _orbitVesselsText;
        private static string _subspaceText;
        private static string _timeText;
        private static string _connectionText;
        private static string _interpolationText;
        private static float _lastUpdateTime;

        private static bool _displayVectors;
        private static bool _displayPositions;
        private static bool _displayVesselsPositions;
        private static bool _displayOrbit;
        private static bool _displayVesselsOrbit;
        private static bool _displaySubspace;
        private static bool _displayTimes;
        private static bool _displayConnectionQueue;
        private static bool _displayInterpolationData;

        private static bool _display;
        public override bool Display
        {
            get => base.Display && _display && MainSystem.NetworkState >= ClientState.Running && HighLogic.LoadedScene >= GameScenes.SPACECENTER;
            set => base.Display = _display = value;
        }

        #endregion

        public override void Update()
        {
            base.Update();
            if (Display && Time.realtimeSinceStartup - _lastUpdateTime > DisplayUpdateInterval || _displayFast)
            {
                _lastUpdateTime = Time.realtimeSinceStartup;
                //Vector text

                if (_displayVectors)
                {
                    if (HighLogic.LoadedScene == GameScenes.FLIGHT && FlightGlobals.ready && FlightGlobals.ActiveVessel != null)
                    {
                        var ourVessel = FlightGlobals.ActiveVessel;

                        StringBuilder.AppendLine($"Id: {ourVessel.id}");
                        StringBuilder.AppendLine($"Forward vector: {ourVessel.GetFwdVector()}");
                        StringBuilder.AppendLine($"Up vector: {ourVessel.upAxis}");
                        StringBuilder.AppendLine($"Srf Rotation: {ourVessel.srfRelRotation}");
                        StringBuilder.AppendLine($"Vessel Rotation: {ourVessel.transform.rotation}");
                        StringBuilder.AppendLine($"Vessel Local Rotation: {ourVessel.transform.localRotation}");
                        StringBuilder.AppendLine($"mainBody Rotation: {ourVessel.mainBody.rotation}");
                        StringBuilder.AppendLine($"mainBody Transform Rotation: {ourVessel.mainBody.bodyTransform.rotation}");
                        StringBuilder.AppendLine($"Surface Velocity: {ourVessel.GetSrfVelocity()}, |v|: {ourVessel.GetSrfVelocity().magnitude}");
                        StringBuilder.AppendLine($"Orbital Velocity: {ourVessel.GetObtVelocity()}, |v|: {ourVessel.GetObtVelocity().magnitude}");
                        if (ourVessel.orbitDriver != null && ourVessel.orbitDriver.orbit != null)
                            StringBuilder.AppendLine($"Frame Velocity: {ourVessel.orbitDriver.orbit.GetFrameVel()}, |v|: {ourVessel.orbitDriver.orbit.GetFrameVel().magnitude}");
                        StringBuilder.AppendLine($"CoM offset vector: {ourVessel.CoM}\n");
                        StringBuilder.AppendLine($"Angular Velocity: {ourVessel.angularVelocity}, |v|: {ourVessel.angularVelocity.magnitude}");

                        _vectorText = StringBuilder.ToString();
                        StringBuilder.Length = 0;
                    }
                    else
                    {
                        _vectorText = "You have to be in flight";
                    }
                }

                if (_displayPositions)
                {
                    if (HighLogic.LoadedScene == GameScenes.FLIGHT && FlightGlobals.ready && FlightGlobals.ActiveVessel != null)
                    {
                        var ourVessel = FlightGlobals.ActiveVessel;

                        StringBuilder.AppendLine($"Id: {ourVessel.id}");
                        StringBuilder.AppendLine($"Situation: {ourVessel.situation}");
                        StringBuilder.AppendLine($"Orbit Pos: {ourVessel.orbit.pos}");
                        StringBuilder.AppendLine($"Transform Pos: {ourVessel.vesselTransform.position}");
                        StringBuilder.AppendLine($"Com Pos: {ourVessel.CoM}");
                        StringBuilder.AppendLine($"ComD Pos: {ourVessel.CoMD}");
                        StringBuilder.AppendLine($"Lat,Lon,Alt: {ourVessel.latitude},{ourVessel.longitude},{ourVessel.altitude}");

                        ourVessel.mainBody.GetLatLonAlt(ourVessel.vesselTransform.position, out var lat, out var lon, out var alt);
                        StringBuilder.AppendLine($"Current Lat,Lon,Alt: {lat},{lon},{alt}");
                        ourVessel.mainBody.GetLatLonAltOrbital(ourVessel.orbit.pos, out lat, out lon, out alt);
                        StringBuilder.AppendLine($"Orbital Lat,Lon,Alt: {lat},{lon},{alt}");

                        StringBuilder.AppendLine($"Inside safety bubble: {SafetyBubbleSystem.Singleton.IsInSafetyBubble(ourVessel)}");

                        _positionText = StringBuilder.ToString();
                        StringBuilder.Length = 0;
                    }
                    else
                    {
                        _positionText = "You have to be in flight";
                    }
                }
                if (_displayVesselsPositions)
                {
                    foreach (var vessel in FlightGlobals.Vessels)
                    {
                        if (vessel == null || FlightGlobals.ActiveVessel == null) continue;

                        if (vessel.id != FlightGlobals.ActiveVessel.id)
                        {
                            StringBuilder.AppendLine($"Id: {vessel.id}");
                            StringBuilder.AppendLine($"Situation: {vessel.situation}");
                            StringBuilder.AppendLine($"Orbit Pos: {vessel.orbit.pos}");
                            StringBuilder.AppendLine($"Transform Pos: {vessel.vesselTransform.position}");
                            StringBuilder.AppendLine($"Com Pos: {vessel.CoM}");
                            StringBuilder.AppendLine($"ComD Pos: {vessel.CoMD}");
                            StringBuilder.AppendLine($"Lat,Lon,Alt: {vessel.latitude},{vessel.longitude},{vessel.altitude}");

                            vessel.mainBody.GetLatLonAlt(vessel.vesselTransform.position, out var lat, out var lon, out var alt);
                            StringBuilder.AppendLine($"Current Lat,Lon,Alt: {lat},{lon},{alt}");
                            vessel.mainBody.GetLatLonAltOrbital(vessel.orbit.pos, out lat, out lon, out alt);
                            StringBuilder.AppendLine($"Orbital Lat,Lon,Alt: {lat},{lon},{alt}");

                            StringBuilder.AppendLine($"Inside safety bubble: {SafetyBubbleSystem.Singleton.IsInSafetyBubble(vessel)}");
                            StringBuilder.AppendLine();
                        }
                    }

                    _positionVesselsText = StringBuilder.ToString();
                    StringBuilder.Length = 0;
                }
                if (_displayOrbit)
                {
                    if (HighLogic.LoadedScene == GameScenes.FLIGHT && FlightGlobals.ready && FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.orbitDriver != null && FlightGlobals.ActiveVessel.orbitDriver.orbit != null)
                    {
                        var ourVessel = FlightGlobals.ActiveVessel;

                        StringBuilder.AppendLine($"Id: {ourVessel.id}");
                        StringBuilder.AppendLine($"Mode: {ourVessel.orbitDriver.updateMode}");
                        StringBuilder.AppendLine($"Semi major axis: {ourVessel.orbit.semiMajorAxis}");
                        StringBuilder.AppendLine($"Eccentricity: {ourVessel.orbit.eccentricity}");
                        StringBuilder.AppendLine($"Inclination: {ourVessel.orbit.inclination}");
                        StringBuilder.AppendLine($"LAN: {ourVessel.orbit.LAN}");
                        StringBuilder.AppendLine($"Arg Periapsis: {ourVessel.orbit.argumentOfPeriapsis}");
                        StringBuilder.AppendLine($"Mean anomaly: {ourVessel.orbit.meanAnomaly}");
                        StringBuilder.AppendLine($"Mean anomaly at Epoch: {ourVessel.orbit.meanAnomalyAtEpoch}");
                        StringBuilder.AppendLine($"Epoch: {ourVessel.orbit.epoch}");
                        StringBuilder.AppendLine($"ObT: {ourVessel.orbit.ObT}");
                        StringBuilder.AppendLine($"Obt Speed : {ourVessel.orbit.GetRelativeVel()}, |v|: {ourVessel.orbit.GetRelativeVel().magnitude}");

                        _orbitText = StringBuilder.ToString();
                        StringBuilder.Length = 0;
                    }
                    else
                    {
                        _orbitText = "You have to be in flight and with an active vessel";
                    }
                }

                if (_displayVesselsOrbit)
                {
                    foreach (var vessel in FlightGlobals.Vessels)
                    {
                        if (vessel == null || FlightGlobals.ActiveVessel == null || vessel.orbitDriver == null || vessel.orbitDriver.orbit == null) continue;

                        if (vessel.id != FlightGlobals.ActiveVessel.id)
                        {
                            StringBuilder.AppendLine($"Id: {vessel.id}");
                            StringBuilder.AppendLine($"Mode: {vessel.orbitDriver.updateMode}");
                            StringBuilder.AppendLine($"Semi major axis: {vessel.orbit.semiMajorAxis}");
                            StringBuilder.AppendLine($"Eccentricity: {vessel.orbit.eccentricity}");
                            StringBuilder.AppendLine($"Inclination: {vessel.orbit.inclination}");
                            StringBuilder.AppendLine($"LAN: {vessel.orbit.LAN}");
                            StringBuilder.AppendLine($"Arg Periapsis: {vessel.orbit.argumentOfPeriapsis}");
                            StringBuilder.AppendLine($"Mean anomaly: {vessel.orbit.meanAnomaly}");
                            StringBuilder.AppendLine($"Mean anomaly at Epoch: {vessel.orbit.meanAnomalyAtEpoch}");
                            StringBuilder.AppendLine($"Epoch: {vessel.orbit.epoch}");
                            StringBuilder.AppendLine($"ObT: {vessel.orbit.ObT}");
                            StringBuilder.AppendLine($"Obt Speed : {vessel.orbit.GetRelativeVel()}, |v|: {vessel.orbit.GetRelativeVel().magnitude}");
                            StringBuilder.AppendLine();
                        }
                    }

                    _orbitVesselsText = StringBuilder.ToString();
                    StringBuilder.Length = 0;
                }

                if (_displaySubspace)
                {
                    StringBuilder.AppendLine($"Warp rate: {Math.Round(Time.timeScale, 3)}x.");
                    StringBuilder.AppendLine($"Current subspace: {WarpSystem.Singleton.CurrentSubspace}.");
                    StringBuilder.AppendLine($"Current subspace time: {WarpSystem.Singleton.CurrentSubspaceTime}s.");
                    StringBuilder.AppendLine($"Current subspace time difference: {WarpSystem.Singleton.CurrentSubspaceTimeDifference}s.");
                    StringBuilder.AppendLine($"Current Error: {Math.Round(TimeSyncSystem.CurrentErrorSec * 1000, 0)}ms.");
                    StringBuilder.AppendLine($"Current universe time: {Math.Round(TimeSyncSystem.UniversalTime, 3)} UT");

                    _subspaceText = StringBuilder.ToString();
                    StringBuilder.Length = 0;
                }

                if (_displayTimes)
                {
                    StringBuilder.AppendLine($"Server start time: {new DateTime(TimeSyncSystem.ServerStartTime):yyyy-MM-dd HH-mm-ss.ffff}");

                    StringBuilder.AppendLine($"Computer clock time (UTC): {DateTime.UtcNow:HH:mm:ss.fff}");
                    StringBuilder.AppendLine($"Computer clock offset (minutes): {LunaComputerTime.SimulatedMinutesTimeOffset}");
                    StringBuilder.AppendLine($"Computer clock time + offset: {LunaComputerTime.UtcNow:HH:mm:ss.fff}");
                    
                    StringBuilder.AppendLine($"Computer <-> NTP clock difference: {LunaNetworkTime.TimeDifference.TotalMilliseconds}ms.");
                    StringBuilder.AppendLine($"NTP clock offset: {LunaNetworkTime.SimulatedMsTimeOffset}ms.");
                    StringBuilder.AppendLine($"Total Difference: {LunaNetworkTime.TimeDifference.TotalMilliseconds + LunaNetworkTime.SimulatedMsTimeOffset}ms.");

                    StringBuilder.AppendLine($"NTP clock time (UTP): {LunaNetworkTime.UtcNow:HH:mm:ss.fff}");

                    _timeText = StringBuilder.ToString();
                    StringBuilder.Length = 0;
                }

                if (_displayConnectionQueue)
                {
                    StringBuilder.AppendLine($"Ping: {NetworkStatistics.GetStatistics("Ping")}ms.");
                    StringBuilder.AppendLine($"Latency: {NetworkStatistics.GetStatistics("Latency")}s.");
                    StringBuilder.AppendLine($"TimeOffset: {TimeSpan.FromTicks(NetworkStatistics.GetStatistics("TimeOffset")).TotalMilliseconds}ms.");
                    StringBuilder.AppendLine($"Last send time: {NetworkStatistics.GetStatistics("LastSendTime")}ms ago.");
                    StringBuilder.AppendLine($"Last receive time: {NetworkStatistics.GetStatistics("LastReceiveTime")}ms ago.");
                    StringBuilder.AppendLine($"Messages in cache: {NetworkStatistics.GetStatistics("MessagesInCache")}.");
                    StringBuilder.AppendLine($"Message data in cache: {NetworkStatistics.GetStatistics("MessageDataInCache")}.");
                    StringBuilder.AppendLine($"Sent bytes: {NetworkStatistics.GetStatistics("SentBytes")}.");
                    StringBuilder.AppendLine($"Received bytes: {NetworkStatistics.GetStatistics("ReceivedBytes")}.\n");
                    _connectionText = StringBuilder.ToString();
                    StringBuilder.Length = 0;
                }

                if (_displayInterpolationData)
                {
                    if (VesselPositionSystem.TargetVesselUpdateQueue.Any())
                    {
                        StringBuilder.Append("Cached: ").AppendLine(PositionUpdateQueue.CacheSize.ToString());
                        foreach (var keyVal in VesselPositionSystem.TargetVesselUpdateQueue)
                        {
                            if (VesselPositionSystem.CurrentVesselUpdate.TryGetValue(keyVal.Key, out var current) && current.Target != null){

                                var perc = current.LerpPercentage * 100;
                                var duration = TimeSpan.FromSeconds(current.InterpolationDuration).TotalMilliseconds;
                                var extraInterpolationTime = TimeSpan.FromSeconds(current.ExtraInterpolationTime).TotalMilliseconds;
                                var timeDiff = TimeSpan.FromSeconds(current.TimeDifference).TotalMilliseconds;
                                StringBuilder.Append(keyVal.Key).Append(": ").Append($" Amt: {keyVal.Value.Count}")
                                    .Append($" Dur: {duration:F0}ms").Append($" TimeDiff: {timeDiff:F0}ms").Append($" T+: {extraInterpolationTime:F0}ms").AppendLine($" Perc: {perc:F0}%");
                                StringBuilder.AppendLine();
                            }
                        }
                    }

                    _interpolationText = StringBuilder.ToString();
                    StringBuilder.Length = 0;
                }
            }
        }

        protected override void DrawGui()
        {
            GUI.skin = DefaultSkin;
            WindowRect = FixWindowPos(GUILayout.Window(6705 + MainSystem.WindowOffset, WindowRect, DrawContent, "Debug", LayoutOptions));
        }

        public override void SetStyles()
        {
            WindowRect = new Rect(Screen.width - (WindowWidth + 50), Screen.height / 2f - WindowHeight / 2f, WindowWidth,
                WindowHeight);
            MoveRect = new Rect(0, 0, int.MaxValue, TitleHeight);

            LayoutOptions = new GUILayoutOption[4];
            LayoutOptions[0] = GUILayout.MinWidth(WindowWidth);
            LayoutOptions[1] = GUILayout.MaxWidth(WindowWidth);
            LayoutOptions[2] = GUILayout.MinHeight(WindowHeight);
            LayoutOptions[3] = GUILayout.MaxHeight(WindowHeight);

            TextAreaOptions = new GUILayoutOption[1];
            TextAreaOptions[0] = GUILayout.ExpandWidth(true);
        }

        public override void RemoveWindowLock()
        {
            if (IsWindowLocked)
            {
                IsWindowLocked = false;
                InputLockManager.RemoveControlLock("LMP_DebugLock");
            }
        }

        public override void CheckWindowLock()
        {
            if (Display)
            {
                if (MainSystem.NetworkState < ClientState.Running || HighLogic.LoadedSceneIsFlight)
                {
                    RemoveWindowLock();
                    return;
                }

                Vector2 mousePos = Input.mousePosition;
                mousePos.y = Screen.height - mousePos.y;

                var shouldLock = WindowRect.Contains(mousePos);

                if (shouldLock && !IsWindowLocked)
                {
                    InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS, "LMP_DebugLock");
                    IsWindowLocked = true;
                }
                if (!shouldLock && IsWindowLocked)
                    RemoveWindowLock();
            }

            if (!Display && IsWindowLocked)
                RemoveWindowLock();
        }
    }
}
