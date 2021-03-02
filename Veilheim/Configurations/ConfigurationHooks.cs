using HarmonyLib;

namespace Veilheim.Configurations
{
    [HarmonyPatch(typeof(ZNet), "RPC_Save")]
    public static class ZNet_RPCSave_Patch
    {
        public static void Postfix()
        {
            // Just save configuration after a save command is issued
            // Server side only
            Configuration.Current.SaveConfiguration();
        }
    }

    [HarmonyPatch(typeof(ZNet), "OnDestroy")]
    public static class ZNet_OnDestroy_Patch
    {
        private static void Prefix()
        {
            //ZLog.Log("Saving local configuration");
            Logger.LogInfo("Saving local configuration");
            Configuration.Current.SaveConfiguration();
        }
    }
}