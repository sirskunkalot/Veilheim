// Veilheim
// a Valheim mod
// 
// File:    Veilheim.cs
// Project: Veilheim

using BepInEx;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using UnityEngine;
using Veilheim.AssetManagers;
using Veilheim.Blueprints;

namespace Veilheim
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    internal class VeilheimPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "de.sirskunkalot.valheim.veilheim";
        public const string PluginName = "Veilheim";
        public const string PluginVersion = "0.3.17";

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

            // Done
            Jotunn.Logger.LogInfo($"{PluginName} v{PluginVersion} loaded");
        }

        private void CreateConfigBindings()
        {
            string section;

            // Section Map
            section = "Map";
            Config.Bind(section, "showPortalsOnMap", false, "Show portals on map");
            Config.Bind(section, "showPortalSelection", false, "Show portal selection window on portal rename");
            Config.Bind(section, "showNoMinimap", false, "Play without minimap");

            // Section MapServer
            section = "MapServer";
            Config.Bind(section, "shareMapProgression", false,
                new ConfigDescription("With this enabled you will receive the same exploration progression as other players on the server", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
            Config.Bind(section, "exploreRadius", 100f,
                new ConfigDescription("The radius of the map that you explore when moving", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
            Config.Bind(section, "exploreRadiusSailing", 100f,
                new ConfigDescription("The radius of the map that you explore while sailing", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
            Config.Bind(section, "playerPositionPublicOnJoin", false,
                new ConfigDescription("Automatically turn on the Map option to share your position when joining or starting a game", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
            Config.Bind(section, "preventPlayerFromTurningOffPublicPosition", false,
                new ConfigDescription("Prevents you and other people on the server to turn off their map sharing option", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));

            // Section Blueprints
            section = "Blueprints";
            Config.Bind(section, "allowPlacementWithoutMaterial", true,
                new ConfigDescription("Allow placement of blueprints without materials", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));

            ShaderHelper.showRealTexturesConfig = Config.Bind("Blueprints", "showRealTextures", false, new ConfigDescription("Show real textures on planned pieces"));
            ShaderHelper.unsupportedColorConfig = Config.Bind("Blueprints", "unsupportedColor", new Color(1f, 1f, 1f, 0.1f), new ConfigDescription("Color of unsupported blueprint pieces"));
            ShaderHelper.supportedColorConfig = Config.Bind("Blueprints", "supportedColor", new Color(1f, 1f, 1f, 0.5f), new ConfigDescription("Color of supported blueprint pieces"));
            ShaderHelper.transparencyConfig = Config.Bind("Blueprints", "transparency", 0.30f, new ConfigDescription("Additional transparency for blueprint pieces", new AcceptableValueRange<float>(0f, 1f)));

            ShaderHelper.showRealTexturesConfig.SettingChanged += ShaderHelper.UpdateAllTextures;
            ShaderHelper.unsupportedColorConfig.SettingChanged += ShaderHelper.UpdateAllTextures;
            ShaderHelper.supportedColorConfig.SettingChanged += ShaderHelper.UpdateAllTextures;
            ShaderHelper.transparencyConfig.SettingChanged += ShaderHelper.UpdateAllTextures;

            // Section ProductionInputAmount
            section = "ProductionInputAmounts";
            Config.Bind(section, "windmillBarleyAmount", 50,
                new ConfigDescription("Max windmill barley amount", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
            Config.Bind(section, "kilnWoodAmount", 25,
                new ConfigDescription("Max wood amount for kiln", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
            Config.Bind(section, "furnaceCoalAmount", 20,
                new ConfigDescription("Max coal amount for furnace", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
            Config.Bind(section, "furnaceOreAmount", 10,
                new ConfigDescription("Max ore amount for furnace", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
            Config.Bind(section, "blastfurnaceCoalAmount", 20,
                new ConfigDescription("Max coal amount for blast furnace", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
            Config.Bind(section, "blastfurnaceOreAmount", 10,
                new ConfigDescription("Max ore amount for blast furnace", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
            Config.Bind(section, "spinningWheelFlachsAmount", 40,
                new ConfigDescription("Max flachs amount for spinning wheel", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
        }
    }
}
