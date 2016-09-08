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

        /** Called every frame */
        private void mainWindow(int id)
        {
            if (expGUI)
            {
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
                GUILayout.Space(6);
                if (possExperiments.Count > 0)
                {
                    foreach (ModuleScienceExperiment e in possExperiments)
                        if (GUILayout.Button(e.experimentActionName))
                            e.DeployExperiment();
                }
                else
                {
                    GUILayout.Label(Text.NOTHING);
                }
                GUILayout.Space(6);
                if (GUILayout.Button(finGUI ? "\u2191" + "Hide finished experiments" + "\u2191" : "\u2193" + "Show finished experiments" + "\u2193"))
                    finGUI = !finGUI;
                if (finGUI)
                {
                    if (finishedExperiments.Count > 0)
                    {
                        GUILayout.Space(6);
                        foreach (ModuleScienceExperiment e in finishedExperiments)
                            if (GUILayout.Button(e.experimentActionName))
                            {
                                if (Event.current.button == 0)
                                {
                                    e.ReviewData();
                                }
                                else if (Event.current.button == 1)
                                {
                                    e.ResetExperiment();
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
                timeSince = 0f;
                return true;
            }
            return false;
        }

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
            if (experiments.Count() > 0)
                foreach (ModuleScienceExperiment exp in experiments)
                    if (exp.GetData().Length > 0)
                        finishedExperiments.Add(exp);
                    else if (checkExperiment(exp))
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

        /** Checks whether a ModuleScienceExperiment is suitable for the current situation */
        private bool checkExperiment(ModuleScienceExperiment exp)
        {
            bool a = !exp.Inoperable && !exp.Deployed && exp.experiment.IsAvailableWhile(expSituation, lastBody)
                                && ResearchAndDevelopment.GetScienceValue(
                                exp.experiment.baseValue * exp.experiment.dataScale,
                                getExperimentSubject(exp.experiment)) > 1f;
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER && exp.experiment.id == "surfaceSample")
                a = a && checkSurfaceSample();
            return a;
        }

        private bool checkSurfaceSample()
        {
            if (GameVariables.Instance.GetScienceCostLimit(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.ResearchAndDevelopment)) >= 500)
                if (lastBody.bodyName == "Kerbin")
                {
                    return true;
                } else
                {
                    if (GameVariables.Instance.UnlockedEVA(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex)))
                        return true;
                }
            return false;
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
            expGUI = config.GetValue<bool>("expGUI");
            expListRect.x = config.GetValue<int>("expListRectX");
            expListRect.y = config.GetValue<int>("expListRectY");
            infGUI = config.GetValue<bool>("infGUI");
            infRect.x = config.GetValue<int>("infRectX");
            infRect.y = config.GetValue<int>("infRectY");
            if ((expListRect.x == 0) && (expListRect.y == 0))
            {
                expListRect.x = Screen.width * 0.6f;
                expListRect.y = 0;
            }
            if ((infRect.x == 0) && (infRect.y == 0))
            {
                infRect.x = Screen.width * 0.6f;
                infRect.y = 0;
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

        private static void debugPrint(string s)
        {
            print(Text.MODTAG + s);
        }
    }
}