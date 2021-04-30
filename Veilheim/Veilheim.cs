// Veilheim
// a Valheim mod
// 
// File:    Veilheim.cs
// Project: Veilheim

using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using Veilheim.AssetManagers;
using Veilheim.Blueprints;
using Veilheim.UnityWrappers;

namespace Veilheim
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    internal class VeilheimPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "de.sirskunkalot.valheim.veilheim";
        public const string PluginName = "Veilheim";
        public const string PluginVersion = "0.3.5";

        // Static instance needed for Coroutines
        public static VeilheimPlugin Instance = null;

        // Unity GameObject as a root to all managers
        internal static GameObject RootObject;

        // Load order for managers
        private readonly List<Type> managerTypes = new List<Type>()
        {
            typeof(GUIManager),
            typeof(BlueprintManager)
        };
        
        // List of all managers
        private readonly List<Manager> managers = new List<Manager>();

        private void Awake()
        {
            Instance = this;

            CreateConfigBindings();

            // Force load custom Unity assemblies
            Assembly.GetAssembly(typeof(ItemDropWrapper));  //TODO: force load assembly somewhat more elegant

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

            Jotunn.Logger.LogInfo($"{PluginName} v{PluginVersion} loaded");
        }

        private void CreateConfigBindings()
        {
            // Section Map
            Config.Bind("Map", "showPortalsOnMap", false, "Show portals on map");
            Config.Bind("Map", "showPortalSelection", false, "Show portal selection window on portal rename");
            Config.Bind("Map", "showNoMinimap", false, "Play without minimap");

            // Section MapServer
            Config.Bind("MapServer", "shareMapProgression", false,
                new ConfigDescription("With this enabled you will receive the same exploration progression as other players on the server", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
            Config.Bind("MapServer", "exploreRadius", 100f,
                new ConfigDescription("The radius of the map that you explore when moving", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
            Config.Bind("MapServer", "exploreRadiusSailing", 100f,
                new ConfigDescription("The radius of the map that you explore while sailing", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
            Config.Bind("MapServer", "playerPositionPublicOnJoin", false,
                new ConfigDescription("Automatically turn on the Map option to share your position when joining or starting a game", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
            Config.Bind("MapServer", "preventPlayerFromTurningOffPublicPosition", false,
                new ConfigDescription("Prevents you and other people on the server to turn off their map sharing option", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));


            // Section ProductionInputAmount
            Config.Bind("ProductionInputAmounts", "windmillBarleyAmount", 50,
                new ConfigDescription("Max windmill barley amount", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
            Config.Bind("ProductionInputAmounts", "kilnWoodAmount", 25,
                new ConfigDescription("Max wood amount for kiln", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
            Config.Bind("ProductionInputAmounts", "furnaceCoalAmount", 20,
                new ConfigDescription("Max coal amount for furnace", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
            Config.Bind("ProductionInputAmounts", "furnaceOreAmount", 10,
                new ConfigDescription("Max ore amount for furnace", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
            Config.Bind("ProductionInputAmounts", "blastfurnaceCoalAmount", 20,
                new ConfigDescription("Max coal amount for blast furnace", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
            Config.Bind("ProductionInputAmounts", "blastfurnaceOreAmount", 10,
                new ConfigDescription("Max ore amount for blast furnace", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
            Config.Bind("ProductionInputAmounts", "spinningWheelFlachsAmount", 40,
                new ConfigDescription("Max flachs amount for spinning wheel", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
        }
    }
}