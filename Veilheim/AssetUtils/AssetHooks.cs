using HarmonyLib;

namespace Veilheim.AssetUtils
{
    [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB", typeof(ObjectDB))]
    public static class ObjectDB_CopyOtherDB_Patch
    {
        public static void Postfix(ObjectDB __instance)
        {
            AssetManager.AddToObjectDB(__instance);
        }
    }

    [HarmonyPatch(typeof(ObjectDB), "Awake")]
    public static class ObjectDB_Awake_Patch
    {
        public static void Postfix(ObjectDB __instance)
        {
            AssetManager.AddToObjectDB(__instance);
        }
    }

    [HarmonyPatch(typeof(ZNetScene), "Awake")]
    public static class ZNetScene_Awake_Patch
    {
        public static void Prefix(ZNetScene __instance)
        {
            AssetManager.AddToZNetScene(__instance);
        }
    }
}
