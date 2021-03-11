using HarmonyLib;

namespace Veilheim.AssetUtils
{

    [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
    public static class ObjectDB_CopyOtherDB_Patch
    {
        public static void Postfix()
        {
            AssetLoader.AddToObjectDB();
        }
    }

    [HarmonyPatch(typeof(ObjectDB), "Awake")]
    public static class ObjectDB_Awake_Patch
    {
        public static void Postfix()
        {
            AssetLoader.AddToObjectDB();
        }
    }

    [HarmonyPatch(typeof(ZNetScene), "Awake")]
    public static class ZNetScene_Awake_Patch
    {
        public static void Prefix(ZNetScene __instance)
        {
            AssetLoader.AddToZNetScene(__instance);
        }
    }
}
