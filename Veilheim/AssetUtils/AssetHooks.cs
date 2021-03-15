using Veilheim.PatchEvents;

namespace Veilheim.AssetUtils
{
   
    public class AssetHooks: Payload
    {
        [PatchEvent(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB), PatchEventType.Postfix)]
        public static void AssetManager_AddObjectDBFejd(ObjectDB instance)
        {
            AssetManager.AddToObjectDBFejd(instance);
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

        [PatchEvent(typeof(Localization), nameof(Localization.SetupLanguage), PatchEventType.Postfix)]
        public static void AssetManager_SetupLanguage(string language)
        {
            AssetManager.SetupLanguage(language);
        }

        [PatchEvent(typeof(Localization), nameof(Localization.Translate), PatchEventType.Postfix)]
        public static bool AssetManager_Translate(string word, out string translated)
        {
            return AssetManager.TryTranslate(word, out translated);
        }
    }
}
