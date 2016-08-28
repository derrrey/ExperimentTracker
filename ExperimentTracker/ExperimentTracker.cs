using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.Linq;
using System.Text;
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
        private float updateTime = 2f;
        private float timeSince = 0f;
        private Texture2D onActive;
        private Texture2D onInactive;
        private Texture2D onReady;
        private Vessel curVessel;
        private CelestialBody lastBody;
        private List<ModuleScienceExperiment> experiments;
        private List<ModuleScienceExperiment> possExperiments;
        private ExperimentSituations expSituation;
        private string curBiome;

        /** GUI stuff */
        private static float windowHeight = 0;
        private static float windowWidth = Screen.height / 5;
        private Rect windowRect = new Rect(0, 0, windowWidth, windowHeight);
        private int windowID = new System.Random().Next(int.MaxValue);

        private void OnGUI()
        {
            if (expGUI)
            {
                clampToScreen();
                windowRect = GUILayout.Window(windowID, windowRect, OnWindow, Text.MODNAME);
            }
        }

        /** Clamps GUI to window size */
        private void clampToScreen()
        {
            windowRect.x = windowRect.x < 0 ? 0 : windowRect.x;
            windowRect.x = windowRect.x + windowRect.width >= Screen.width ? (Screen.width - 1) - windowRect.width : windowRect.x;
            windowRect.y = windowRect.y < 0 ? 0 : windowRect.y;
            windowRect.y = windowRect.y + windowRect.height >= Screen.height ? (Screen.height - 1) - windowRect.height : windowRect.y;
        }

        /** Called every frame */
        private void OnWindow(int id)
        {
            if (expGUI)
            {
                if (possExperiments.Count == 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(Text.NOTHING);
                    GUILayout.EndHorizontal();
                } else
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.BeginVertical();
                    foreach (ModuleScienceExperiment e in possExperiments)
                    {
                        if (GUILayout.Button(e.experimentActionName))
                        {
                            e.DeployExperiment();
                        }
                    }
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                }
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

        /** Returns the ScienceSubject to a given ScienceExperiment */
        private ScienceSubject getExperimentSubject(ScienceExperiment exp)
        {
            string biome = string.Empty;
            if (exp.BiomeIsRelevantWhile(expSituation))
                biome = currentBiome();
            return ResearchAndDevelopment.GetExperimentSubject(exp, expSituation, lastBody, biome);
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
                timeSince = 0;
                return true;
            }
            return false;
        }

        private void statusUpdate()
        {
            curVessel = FlightGlobals.ActiveVessel;
            curBiome = currentBiome();
            expSituation = ScienceUtil.GetExperimentSituation(curVessel);
            lastBody = curVessel.mainBody;
            experiments = getExperiments();
            possExperiments = new List<ModuleScienceExperiment>();
            if (experiments.Count() > 0)
                foreach (ModuleScienceExperiment exp in experiments)
                    if (checkExperiment(exp))
                        possExperiments.Add(exp);
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
            windowRect.width = windowWidth;
            windowRect.height = windowHeight;
        }

        /** Checks whether a ModuleScienceExperiment is suitable for the current situation */
        private bool checkExperiment(ModuleScienceExperiment exp)
        {
            return !exp.Inoperable && !exp.Deployed && ResearchAndDevelopment.GetScienceValue(
                                exp.experiment.baseValue * exp.experiment.dataScale,
                                getExperimentSubject(exp.experiment)) > 1f;
        }

        /** Gets all science experiments */
        private List<ModuleScienceExperiment> getExperiments()
        {
            return FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceExperiment>();
        }

        /** Called once at startup */
        public void Awake()
        {
            debugPrint("Awake()");

            /** Config loading setup */
            PluginConfiguration config = PluginConfiguration.CreateForType<ExperimentTracker>();
            config.load();
            expGUI = config.GetValue<bool>("isActive");
            windowRect.x = config.GetValue<int>("windowRectX");
            windowRect.y = config.GetValue<int>("windowRectY");
            if ((windowRect.x == 0) && (windowRect.y == 0))
            {
                windowRect.x = Screen.width * 0.6f;
                windowRect.y = 0;
            }

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
        }

        /** Called on destroy */
        public void OnDestroy()
        {
            debugPrint("OnDestroy()");

            /** Save to config */
            PluginConfiguration config = PluginConfiguration.CreateForType<ExperimentTracker>();
            config.SetValue("isActive", expGUI);
            config.SetValue("windowRectX", (int)windowRect.x);
            config.SetValue("windowRectY", (int)windowRect.y);
            config.save();

            /** Unregister for events */
            GameEvents.onGUIApplicationLauncherReady.Remove(setupButton);
        }

        /** Set up for the toolbar-button */
        private void setupButton()
        {
            if (ApplicationLauncher.Ready)
            {
                if (etButton == null)
                {
                    debugPrint("Setting up button");
                    ApplicationLauncher instance = ApplicationLauncher.Instance;
                    etButton = instance.AddModApplication(toggleActive, toggleActive, null, null, null, null, ApplicationLauncher.AppScenes.FLIGHT, getButtonTexture());
                } else
                {
                    etButton.onTrue = toggleActive;
                    etButton.onFalse = toggleActive;
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
            expGUI = !expGUI;
            etButton.SetTexture(getButtonTexture());
        }

        /** Load and return a texture */
        private static Texture2D loadTexture(string path)
        {
            debugPrint("Loading (Texture): " + path);
            return GameDatabase.Instance.GetTexture(path, false);
        }

        private static void debugPrint(string s)
        {
            print(Text.MODTAG + s);
        }
    }
}