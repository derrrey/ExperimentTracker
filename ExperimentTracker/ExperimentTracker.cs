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
        /** modTag to use in every debug message */
        private static string modTag = "[ExperimentTracker]: ";

        /** Used variables */
        private static ApplicationLauncherButton etButton;
        private bool isActive;
        private bool nothingToDo = true;
        private Texture2D onActive;
        private Texture2D onInactive;
        private Vessel curVessel;
        private ExperimentSituations expSituation = 0;
        private string curBiome;

        /** GUI stuff */
        private Rect windowRect = new Rect();
        float windowHeight = 50;
        float windowWidth = 400;
        private int windowID = new System.Random().Next(int.MaxValue);

        private void OnGUI()
        {
            if (isActive)
            {
                clampToScreen();
                windowRect = GUILayout.Window(windowID, windowRect, OnWindow, "ExperimentTracker");
                windowRect.width = windowWidth;
                windowRect.height = windowHeight;
            }
        }

        private void clampToScreen()
        {
            windowRect.x = windowRect.x < 0 ? 0 : windowRect.x;
            windowRect.x = windowRect.x + windowRect.width >= Screen.width ? (Screen.width - 1) - windowRect.width : windowRect.x;
            windowRect.y = windowRect.y < 0 ? 0 : windowRect.y;
            windowRect.y = windowRect.y + windowRect.height >= Screen.height ? (Screen.height - 1) - windowRect.height : windowRect.y;
        }

        private void OnWindow(int id)
        {
            if (isActive)
            {
                if (nothingToDo)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("There are no possible experiments yet");
                    GUILayout.EndHorizontal();
                }
                GUI.DragWindow();
            }
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
                windowRect.x = Screen.width * 0.35f;
                windowRect.y = Screen.height * 0.1f;
            }

            /** Register for events */
            GameEvents.onGUIApplicationLauncherReady.Add(setupButton);
        }

        public void Start()
        {
            debugPrint("Start()");

            /** Load textures */
            onActive = loadTexture("ExperimentTracker/icons/ET_active");
            onInactive = loadTexture("ExperimentTracker/icons/ET_inactive");

            /** Get active vessel */
            curVessel = FlightGlobals.ActiveVessel;
        }

        /** Called on destroy */
        public void OnDestroy()
        {
            debugPrint("OnDestroy()");

            /** Save to config */
            PluginConfiguration config = PluginConfiguration.CreateForType<ExperimentTracker>();
            config.SetValue("isActive", isActive);
            config.SetValue("windowRectX", windowRect.x);
            config.SetValue("windowRectY", windowRect.y);
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
            print(modTag + s);
        }
    }
}
