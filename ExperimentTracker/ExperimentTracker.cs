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
        private bool isActive;
        private bool nothingToDo;
        private Texture2D onActive;
        private Texture2D onInactive;
        private Vessel curVessel;
        private List<ModuleScienceExperiment> experiments;
        private List<ScienceExperiment> possExperiments;
        private ExperimentSituations expSituation = 0;
        private string curBiome;

        /** GUI stuff */
        private Rect windowRect = new Rect(0, 0, 400, 50);
        private int windowID = new System.Random().Next(int.MaxValue);

        private void OnGUI()
        {
            if (isActive)
            {
                clampToScreen();
                windowRect = GUILayout.Window(windowID, windowRect, OnWindow, Text.MODNAME);
            }
        }

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
            if (isActive)
            {
                if (nothingToDo)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(Text.NOTHING);
                    GUILayout.EndHorizontal();
                } else
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.BeginVertical();
                    foreach (ScienceExperiment e in possExperiments)
                    {
                        GUILayout.Button(e.experimentTitle);
                    }
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                }
                GUI.DragWindow();
            }
        }

        public void FixedUpdate()
        {
            possExperiments.Clear();
            if (experiments.Count() > 0)
            {
                foreach (ModuleScienceExperiment exp in experiments)
                {
                    if (!possExperiments.Contains(exp.experiment))
                    {
                        possExperiments.Add(exp.experiment);
                    }
                }
            }
            nothingToDo = possExperiments.Count > 0 ? false : true;
        }

        /** Gets all science experiments */
        private List<ModuleScienceExperiment> getExperiments()
        {
            return FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceExperiment>();
        }

        /** Gets the science container to store all science data */
        private ModuleScienceContainer getScienceContainer()
        {
            return FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceContainer>().FirstOrDefault();
        }

        /** Checks if there is a scientist on board to rerun experiments */
        private bool isScientistOnBoard()
        {
            foreach (ProtoCrewMember m in curVessel.GetVesselCrew())
                if (m.experienceTrait.Title == Text.SCIENTIST) return true;
            return false;
        }

        /** Called once at startup */
        public void Awake()
        {
            debugPrint("Awake()");

            /** Config loading setup */
            PluginConfiguration config = PluginConfiguration.CreateForType<ExperimentTracker>();
            config.load();
            isActive = config.GetValue<bool>("isActive");
            windowRect.x = config.GetValue<int>("windowRectX");
            windowRect.y = config.GetValue<int>("windowRectY");
            if ((windowRect.x == 0) && (windowRect.y == 0))
            {
                windowRect.x = Screen.width * 0.2f;
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

            /** Get active vessel */
            curVessel = FlightGlobals.ActiveVessel;

            /** Initialize lists */
            experiments = getExperiments();
            possExperiments = new List<ScienceExperiment>();
        }

        /** Called on destroy */
        public void OnDestroy()
        {
            debugPrint("OnDestroy()");

            /** Save to config */
            PluginConfiguration config = PluginConfiguration.CreateForType<ExperimentTracker>();
            config.SetValue("isActive", isActive);
            config.SetValue("windowRectX", (int)windowRect.x);
            config.SetValue("windowRectY", (int)windowRect.y);
            config.save();

            /** Unregister for events */
            GameEvents.onGUIApplicationLauncherReady.Remove(setupButton);
        }

        /** Set up for the toolbar-button */
        public void setupButton()
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
            return isActive ? onActive : onInactive;
        }

        /** Called when button is pressed */
        private void toggleActive()
        {
            debugPrint("toggleAction()");
            isActive = !isActive;
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