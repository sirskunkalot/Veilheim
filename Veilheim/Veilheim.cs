// Veilheim
// a Valheim mod
// 
// File:    Veilheim.cs
// Project: Veilheim

using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Veilheim.AssetManagers;
using Veilheim.Blueprints;
using Veilheim.Configurations;
using Veilheim.Configurations.GUI;
using Veilheim.PatchEvents;
using Veilheim.UnityWrappers;

namespace Veilheim
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    internal class VeilheimPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "de.sirskunkalot.valheim.veilheim";
        public const string PluginName = "Veilheim";
        public const string PluginVersion = "0.1.0";

        // Static instance needed for Coroutines
        public static VeilheimPlugin Instance = null;

        // Load order for managers
        private readonly List<Type> managerTypes = new List<Type>()
        {
            typeof(LocalizationManager),
            typeof(PrefabManager),
            typeof(PieceManager),
            typeof(ItemManager),
            typeof(GUIManager),
            typeof(PatchManager),
            typeof(BlueprintManager)
        };

        private readonly List<Manager> managers = new List<Manager>();

        internal static GameObject RootObject;

        private Harmony m_harmony;

        private void Awake()
        {
            // Force load custom Unity assemblies
            Assembly.GetAssembly(typeof(ItemDropWrapper));  //TODO: force load assembly somewhat more elegant

            // Create harmony patches
            m_harmony = new Harmony(PluginGUID);
            m_harmony.PatchAll();

            // Initialize Logger
            Veilheim.Logger.Init();

            // Root GameObject for all plugin components
            RootObject = new GameObject("_VeilheimPlugin");
            DontDestroyOnLoad(RootObject);

            // Create and initialize all managers
            foreach (Type managerType in managerTypes)
            {
                managers.Add((Manager)RootObject.AddComponent(managerType));
            }

            foreach (Manager manager in managers)
            {
                manager.Init();
            }

            //TODO: load assets with events from manager
            AssetUtils.AssetLoader.LoadAssets();

            Veilheim.Logger.LogInfo($"{PluginName} v{PluginVersion} loaded");
            Instance = this;
        }


        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F6))
            { // Set a breakpoint here to break on F6 key press
                ConfigurationGUI.CreateConfigurationGUIRoot();
                GameCamera.instance.m_mouseCapture = !ConfigurationGUI.ToggleGUI();
                GameCamera.instance.UpdateMouseCapture();
            }

        }


        private void OnDestroy()
        {
            Veilheim.Logger.LogInfo($"Destroying {PluginName} v{PluginVersion}");

            //TODO: destroy managers, no need for an interface anymore

            Veilheim.Logger.Destroy();

            m_harmony.UnpatchAll(PluginGUID);
        }

        private void OnGUI()
        {
            // Display version in main menu
            if (SceneManager.GetActiveScene().name == "start")
            {
                GUI.Label(new Rect(Screen.width - PluginName.Length * 12, 5, PluginName.Length * 12, 25), $"{PluginName} v{PluginVersion}");
            }
        }

        internal void EnableConfigGui()
        {
            ConfigurationGUI.EnableEntries();
        }
    }
}