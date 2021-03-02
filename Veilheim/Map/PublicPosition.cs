using HarmonyLib;
using Veilheim.Configurations;

namespace Veilheim.Map
{
    [HarmonyPatch(typeof(ZNet), "Awake")]
    public static class ZNet_Awake_Patch
    {
        private static void Postfix(ref ZNet __instance)
        {
            if (Configuration.Current.MapServer.IsEnabled && Configuration.Current.MapServer.playerPositionPublicOnJoin)
            {
                // Set player position visibility to public by default on server join
                __instance.m_publicReferencePosition = true;
            }
        }
    }

    [HarmonyPatch(typeof(ZNet), "SetPublicReferencePosition")]
    public static class ZNet_SetPublicReferencePosition_Patch
    {
        public static void Postfix()
        {
            if (Configuration.Current.MapServer.IsEnabled && Configuration.Current.MapServer.preventPlayerFromTurningOffPublicPosition)
            {
                ZNet.instance.m_publicReferencePosition = true;
            }
        }
    }

}
