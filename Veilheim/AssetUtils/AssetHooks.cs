using HarmonyLib;
using Veilheim.PatchEvents;

namespace Veilheim.AssetUtils
{
   
    public class AssetLoader_Patches: Payload
    {
        [PatchEvent(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB), PatchEventType.Postfix)]
        public static void AssetManager_AddObjectDBFejd(ObjectDB instance)
        {
            AssetManager.AddToObjectDB(instance);
        }

        [PatchEvent(typeof(ObjectDB), nameof(ObjectDB.Awake), PatchEventType.Postfix)]
        public static void AssetManager_AddObjectDB(ObjectDB instance)
        {
            AssetManager.AddToObjectDB(instance);
        }

        [PatchEvent(typeof(ZNetScene), nameof(ZNetScene.Awake), PatchEventType.Prefix)]
        public static void AssetManager_AddZNetScene(ZNetScene instance)
        {
            AssetManager.AddToZNetScene(instance);
        }
    }
}
