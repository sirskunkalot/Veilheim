// Veilheim
// a Valheim mod
// 
// File:    AssetLoader.cs
// Project: Veilheim

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using UnityEngine;

namespace Veilheim.AssetUtils
{
    //TODO: dont hardcode prefab and bundle names

    internal static class AssetLoader
    {
        public static void LoadAssets()
        {
            AssetBundle assetBundle;

            // AssetBundle for the blueprint rune. Loading the rune itself loads the referenced PieceTable and Pieces
            assetBundle = LoadAssetBundleFromResources("blueprintrune");
            LoadItemPrefab(assetBundle, "BlueprintRune",
                new ItemDef
                {
                    Amount = 1, 
                    //CraftingStation = "piece_workbench", 
                    Resources = new List<RequirementDef> {
                        new RequirementDef {Item = "Stone", Amount = 1}
                    }
                });
            LoadLocalization(assetBundle);
            assetBundle.Unload(false);
        }

        /// <summary>
        ///     Try to get a path to <paramref name="fileName" /> from either &lt;assembly_path&gt;/&lt;plugin_name&gt;/
        ///     or &lt;assembly_path&gt;/ as a fallback
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static string GetFilePath(string fileName)
        {
            var filePath = Path.Combine(Paths.PluginPath, VeilheimPlugin.PluginName, fileName);
            if (!File.Exists(filePath))
            {
                var assembly = typeof(VeilheimPlugin).Assembly;
                filePath = Path.Combine(Path.GetDirectoryName(assembly.Location), fileName);
                if (!File.Exists(filePath))
                {
                    Logger.LogError($"Asset file {fileName} not found in filesystem");
                    return null;
                }
            }

            return filePath;
        }

        public static Sprite LoadSpriteFromFile(string spritePath)
        {
            var fileData = File.ReadAllBytes(GetFilePath(spritePath));
            var tex = new Texture2D(20, 20);
            if (tex.LoadImage(fileData))
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(), 100);
            }

            return null;
        }

        /// <summary>
        ///     Load an assembly-embedded <see cref="AssetBundle" />
        /// </summary>
        /// <param name="bundleName">Name of the bundle</param>
        /// <returns></returns>
        public static AssetBundle LoadAssetBundleFromResources(string bundleName)
        {
            var execAssembly = Assembly.GetExecutingAssembly();

            var resourceName = execAssembly.GetManifestResourceNames().Single(str => str.EndsWith(bundleName));

            if (resourceName == null)
            {
                Logger.LogError($"AssetBundle {bundleName} not found in assembly manifest");
                return null;
            }

            AssetBundle ret;
            using (var stream = execAssembly.GetManifestResourceStream(resourceName))
            {
                ret = AssetBundle.LoadFromStream(stream);
            }

            return ret;
        }

        /// <summary>
        ///     Load an external <see cref="AssetBundle" />
        /// </summary>
        /// <param name="fileName">Filename of the bundle</param>
        /// <returns></returns>
        public static AssetBundle LoadAssetBundleFromFile(string fileName)
        {
            var assetBundlePath = GetFilePath(fileName);
            return AssetBundle.LoadFromFile(assetBundlePath);
        }

        /// <summary>
        ///     Load an "untyped" prefab from a bundle and register it in <see cref="AssetManager" />.
        /// </summary>
        /// <param name="assetBundle"></param>
        /// <param name="assetName"></param>
        public static void LoadPrefab(AssetBundle assetBundle, string assetName)
        {
            var prefab = assetBundle.LoadAsset<GameObject>(assetName);
            AssetManager.RegisterPrefab(prefab);
        }

        /// <summary>
        ///     Load an item prefab from a bundle and register it in <see cref="AssetManager" />.
        /// </summary>
        /// <param name="assetBundle"></param>
        /// <param name="assetName"></param>
        /// <param name="itemDef"></param>
        public static void LoadItemPrefab(AssetBundle assetBundle, string assetName, ItemDef itemDef)
        {
            var prefab = assetBundle.LoadAsset<GameObject>(assetName);
            AssetManager.RegisterItemPrefab(prefab, itemDef);
        }

        /// <summary>
        ///     Load a piece prefab from a bundle and register it in <see cref="AssetManager" />.
        /// </summary>
        /// <param name="assetBundle"></param>
        /// <param name="assetName"></param>
        /// <param name="pieceDef"></param>
        public static void LoadPiecePrefab(AssetBundle assetBundle, string assetName, PieceDef pieceDef)
        {
            var prefab = assetBundle.LoadAsset<GameObject>(assetName);
            AssetManager.RegisterPiecePrefab(prefab, pieceDef);
        }

        /// <summary>
        ///     Load the localization <see cref="TextAsset" /> from a bundle. Asset name must be "localization".
        /// </summary>
        /// <param name="assetBundle"></param>
        public static void LoadLocalization(AssetBundle assetBundle)
        {
            var asset = assetBundle.LoadAsset<TextAsset>("localization");

            if (asset != null)
            {
                var localization = new AssetLocalization(assetBundle.name, asset);
                AssetManager.RegisterLocalization(localization);
            }
        }
    }
}