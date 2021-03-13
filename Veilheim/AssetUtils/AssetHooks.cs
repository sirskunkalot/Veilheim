using HarmonyLib;
using Veilheim.PatchEvents;

namespace Veilheim.AssetUtils
{
   
    public class AssetLoader_Patches: Payload
    {
        [PatchEvent(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB), PatchEventType.Postfix)]
        public static void Postfix(ObjectDB instance)
        {
            AssetManager.AddToObjectDB(instance);
        }

        [PatchEvent(typeof(ObjectDB), nameof(ObjectDB.Awake), PatchEventType.Postfix)]
        public static void Assetloader_AddObjectDB(ObjectDB instance)
        {
            AssetManager.AddToObjectDB(instance);
        }

        [PatchEvent(typeof(ZNetScene), nameof(ZNetScene.Awake), PatchEventType.Prefix)]
        public static void Assetloader_Load(ZNetScene instance)
        {
            AssetManager.AddToZNetScene(instance);
        }
    }
}
