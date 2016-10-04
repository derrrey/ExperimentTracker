﻿using System;
using System.Collections.Generic;
using System.Linq;
using KSP.UI.Screens;
using KSP.IO;

using UnityEngine;

namespace ExperimentTracker
{
    /** Load in Flight scene once */
    [KSPAddon(KSPAddon.Startup.Flight, false)]

    public class ExperimentTracker : MonoBehaviour
    {
        /** Used variables */
        private static ApplicationLauncherButton etButton;
        private bool expGUI;
        private bool infGUI;
        private bool finGUI;
        private float updateTime = 1f;
        private float timeSince = 0f;
        private Texture2D onActive;
        private Texture2D onInactive;
        private Texture2D onReady;
        private Vessel curVessel;
        private CelestialBody lastBody;
        private List<ModuleScienceExperiment> experiments;
        private List<ModuleScienceExperiment> possExperiments;
        private List<ModuleScienceExperiment> finishedExperiments;
        private List<IETExperiment> activators;
        private IETExperiment stockScience;
        private ExperimentSituations expSituation;
        private string curBiome;

        /** GUI stuff */
        private static float windowHeight = 0;
        private static float windowWidth = Screen.height / 5;
        private Rect expListRect = new Rect(0, 0, windowWidth, windowHeight);
        private Rect infRect = new Rect(0, 0, windowWidth, windowHeight);

        private void OnGUI()
        {
            if (expGUI)
                expListRect = GUILayout.Window(42, expListRect, mainWindow, Text.MODNAME);
            if (infGUI)
                infRect = GUILayout.Window(1337, infRect, infWindow, Text.INFO);
        }

        /** The info UI */
        private void infWindow(int id)
        {
            if (infGUI)
            {
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                if (GUILayout.Button("Close"))
                    infGUI = false;
                GUILayout.Label("Biome: " + curBiome);
                GUILayout.Label("Situation: " + expSituation);
                GUILayout.Label("Body: " + lastBody);
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUI.DragWindow();
            }
        }

        /** The main UI */
        private void mainWindow(int id)
        {
            if (expGUI)
            {
                bool hasPoss = possExperiments.Count > 0;
                bool hasFin = finishedExperiments.Count > 0;

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Info"))
                    infGUI = !infGUI;
                if (GUILayout.Button("Close"))
                    expGUI = false;
                if (GUILayout.Button("Close all"))
                {
                    expGUI = false;
                    infGUI = false;
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                if (hasPoss)
                    if (GUILayout.Button("Deploy all"))
                        foreach (ModuleScienceExperiment e in possExperiments)
                            deploy(e);
                GUILayout.Space(6);
                if (hasPoss)
                {
                    foreach (ModuleScienceExperiment e in possExperiments)
                        if (GUILayout.Button(e.experimentActionName))
                            deploy(e);
                }
                else
                {
                    GUILayout.Label(Text.NOTHING);
                }
                if (hasFin)
                {
                    GUILayout.Space(6);
                    if (GUILayout.Button(finGUI ? "\u2191" + "Hide finished experiments" + "\u2191" : "\u2193" + "Show finished experiments" + "\u2193"))
                        finGUI = !finGUI;
                }
                if (finGUI)
                {
                    if (hasFin)
                    {
                        GUILayout.Space(6);
                        foreach (ModuleScienceExperiment e in finishedExperiments)
                            if (GUILayout.Button(e.experimentActionName))
                            {
                                if (Event.current.button == 0)
                                {
                                    review(e);
                                }
                                else if (Event.current.button == 1)
                                {
                                    reset(e);
                                }
                            }
                    }
                }
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUI.DragWindow();
            }
        }

        /** Finds the current biome string */
        private string currentBiome()
        {
            if (curVessel != null)
                if (curVessel.mainBody.BiomeMap != null)
                    return !string.IsNullOrEmpty(curVessel.landedAt)
                        ? Vessel.GetLandedAtString(curVessel.landedAt)
                        : ScienceUtil.GetExperimentBiome(curVessel.mainBody,
                            curVessel.latitude, curVessel.longitude);
            return string.Empty;
        }

        /** Determines whether the status of the vessel has changed */
        private bool statusHasChanged()
        {
            return FlightGlobals.ActiveVessel.loaded && (curVessel != FlightGlobals.ActiveVessel || curBiome != currentBiome() ||
                expSituation != ScienceUtil.GetExperimentSituation(curVessel) || lastBody != curVessel.mainBody);
        }

        private bool timeIsUp()
        {
            if ((timeSince += Time.deltaTime) >= updateTime)
            {
                timeSince = 0f;
                return true;
            }
            return false;
        }

        /** Checks type for an experiment and returns suitable IETExperiment */
        private IETExperiment checkType(ModuleScienceExperiment exp)
        {
            foreach (IETExperiment act in activators)
            {
                try
                {
                    if (exp.GetType() == act.getType() || exp.GetType().IsSubclassOf(act.getType()))
                        return act;
                }
                catch (Exception)
                {
                    continue;
                }
            }
            return null;
        }

        /** Deploys an experiment */
        private void deploy(ModuleScienceExperiment exp)
        {
            IETExperiment activator = checkType(exp);
            if (activator != null)
                activator.deployExperiment(exp);
        }

        /** Resets an experiment */
        private void reset(ModuleScienceExperiment exp)
        {
            IETExperiment activator = checkType(exp);
            if (activator != null)
                activator.resetExperiment(exp);
        }

        /** Opens the reviewData dialog for an experiment */
        private void review(ModuleScienceExperiment exp)
        {
            IETExperiment activator = checkType(exp);
            if (activator != null)
                activator.reviewData(exp);
        }

        /** Updates experiments and other fields */
        private void statusUpdate()
        {
            timeSince = 0f;
            curVessel = FlightGlobals.ActiveVessel;
            curBiome = currentBiome();
            expSituation = ScienceUtil.GetExperimentSituation(curVessel);
            lastBody = curVessel.mainBody;
            experiments = getExperiments();
            possExperiments = new List<ModuleScienceExperiment>();
            finishedExperiments = new List<ModuleScienceExperiment>();
            IETExperiment activator;
            if (experiments.Count() > 0)
            {
                foreach (ModuleScienceExperiment exp in experiments)
                {
                    activator = checkType(exp);
                    if (activator != null)
                    {
                        if (activator.hasData(exp))
                        {
                            finishedExperiments.Add(exp);
                        }
                        else if (activator.checkExperiment(exp, expSituation, lastBody, curBiome))
                        {
                            possExperiments.Add(exp);
                        }
                    }
                }
            }
        }

        /** Called every frame */
        public void FixedUpdate()
        {
            if (statusHasChanged() || timeIsUp())
                statusUpdate();
            if (possExperiments.Count > 0 && etButton != null && !expGUI)
                etButton.SetTexture(onReady);
            else if (etButton != null)
                etButton.SetTexture(getButtonTexture());
            expListRect.width = windowWidth;
            expListRect.height = windowHeight;

            /** Comment out the following code pre release!!! */
            /**
            if (Input.GetKeyDown(KeyCode.End))
            {
                ResearchAndDevelopment.Instance.CheatAddScience(100000);
                Reputation.Instance.AddReputation(100000, TransactionReasons.Cheating);
                Funding.Instance.AddFunds(100000, TransactionReasons.Cheating);
            }
            */
        }

        /** Gets all science experiments */
        private List<ModuleScienceExperiment> getExperiments()
        {
            return FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceExperiment>();
        }

        /** Resets window positions if necessary */
        private void resetWindowPos()
        {
            if ((expListRect.x <= 0 || expListRect.y <= 0) || (expListRect.x >= Screen.width || expListRect.y >= Screen.height))
            {
                expListRect.x = Screen.width * 0.6f;
                expListRect.y = 0;
            }
            if ((infRect.x <= 0 || infRect.y <= 0) || (infRect.x >= Screen.width || infRect.y >= Screen.height))
            {
                infRect.x = Screen.width * 0.6f;
                infRect.y = 0;
            }
        }

        /** Called once at startup */
        public void Awake()
        {
            debugPrint("Awake()");

            /** Config loading setup */
            PluginConfiguration config = PluginConfiguration.CreateForType<ExperimentTracker>();
            config.load();
            expGUI = config.GetValue<bool>("expGUI");
            expListRect.x = config.GetValue<int>("expListRectX");
            expListRect.y = config.GetValue<int>("expListRectY");
            infGUI = config.GetValue<bool>("infGUI");
            infRect.x = config.GetValue<int>("infRectX");
            infRect.y = config.GetValue<int>("infRectY");
            resetWindowPos();

            /** Register for events */
            GameEvents.onGUIApplicationLauncherReady.Add(setupButton);
        }

        /** Called after Awake */
        public void Start()
        {
            debugPrint("Start()");

            /** Load textures */
            onActive = loadTexture("ExperimentTracker/icons/ET_active");
            onInactive = loadTexture("ExperimentTracker/icons/ET_inactive");
            onReady = loadTexture("ExperimentTracker/icons/ET_ready");

            /** Initialize activators and add to activators list */
            activators = new List<IETExperiment>();
            activators.Add(new OrbitalScience());
            activators.Add(new StockScience());
        }

        /** Called on destroy */
        public void OnDestroy()
        {
            debugPrint("OnDestroy()");

            /** Save to config */
            PluginConfiguration config = PluginConfiguration.CreateForType<ExperimentTracker>();
            config.SetValue("expGUI", expGUI);
            config.SetValue("expListRectX", (int)expListRect.x);
            config.SetValue("expListRectY", (int)expListRect.y);
            config.SetValue("infGUI", infGUI);
            config.SetValue("infRectX", (int)infRect.x);
            config.SetValue("infRectY", (int)infRect.y);
            config.save();

            /** Unregister for events */
            GameEvents.onGUIApplicationLauncherReady.Remove(setupButton);
        }

        /** Set up for the toolbar-button */
        private void setupButton()
        {
            if (ApplicationLauncher.Ready)
            {
                if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX)
                {
                    if (etButton == null)
                    {
                        debugPrint("Setting up button");
                        ApplicationLauncher instance = ApplicationLauncher.Instance;
                        etButton = instance.AddModApplication(toggleActive, toggleActive, null, null, null, null, ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW, getButtonTexture());
                    }
                    else
                    {
                        debugPrint("Button already set up");
                        etButton.onTrue = toggleActive;
                        etButton.onFalse = toggleActive;
                    }
                } else
                {
                    if (etButton != null)
                        ApplicationLauncher.Instance.RemoveModApplication(etButton);
                    expGUI = false;
                    infGUI = false;
                }
            }
        }

        /** Get correct button texture */
        private Texture2D getButtonTexture()
        {
            return expGUI ? onActive : onInactive;
        }

        /** Called when button is pressed */
        private void toggleActive()
        {
            debugPrint("toggleAction()");
            expGUI = infGUI ? false : !expGUI;
            infGUI = false;
            etButton.SetTexture(getButtonTexture());
        }

        /** Load and return a texture */
        private static Texture2D loadTexture(string path)
        {
            debugPrint("Loading (Texture): " + path);
            return GameDatabase.Instance.GetTexture(path, false);
        }

        /** Prints message to console */
        private static void debugPrint(string s)
        {
            print(Text.MODTAG + s);
        }
    }
}