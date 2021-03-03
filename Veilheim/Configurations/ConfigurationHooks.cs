using HarmonyLib;

namespace Veilheim.Configurations
{
    [HarmonyPatch(typeof(ZNet), "Awake")]
    public static class ZNet_Awake_Patch
    {
        private static void Postfix()
        {
            string msg = $"Loading {ZNet.instance.GetInstanceType()} configuration";
            Logger.LogInfo(msg);

            if (!Configuration.LoadConfiguration())
            {
                Logger.LogError("Error while loading configuration");
            }
            else
            {
                Logger.LogInfo("Configuration loaded succesfully");
            }
        }
    }

    [HarmonyPatch(typeof(ZNet), "RPC_Save")]
    public static class ZNet_RPCSave_Patch
    {
        public static void Postfix()
        {
            // Just save configuration after a save command is issued
            // Server side only
            Logger.LogInfo("Saving configuration via RPC_Save");
            Configuration.Current.SaveConfiguration();
        }
    }

    [HarmonyPatch(typeof(ZNet), "OnDestroy")]
    public static class ZNet_OnDestroy_Patch
    {
        private static void Prefix()
        {
            //ZLog.Log("Saving local configuration");
            Logger.LogInfo("Saving configuration via OnDestroy");
            Configuration.Current.SaveConfiguration();
        }
    }

}