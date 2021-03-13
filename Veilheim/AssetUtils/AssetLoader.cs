using BepInEx;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Veilheim.AssetUtils
{
    //TODO: dont hardcode prefab and bundle names

    internal class AssetLoader : IDestroyable
    {
        public static AssetLoader Instance;

        public AssetLoader()
        {
            Instance = this;
        }

        public void Destroy()
        {
            Logger.LogDebug("Destroying AssetLoader");
        }
        
        public void LoadAssets()
        {
            var assetBundle = LoadAssetBundleFromResources("veilheim");
            LoadItemPrefab(assetBundle, "SkunkAxe", new ItemDef
            {
                CraftingStation = "piece_workbench",
                RepairStation = "piece_workbench",
                Resources = new List<RequirementDef>
                {
                    new RequirementDef { Item = "Wood", Amount = 1 }
                }
            });
            LoadItemPrefab(assetBundle, "SkunkHammer", new ItemDef()
            {
                CraftingStation = "piece_workbench",
                RepairStation = "piece_workbench",
                Resources = new List<RequirementDef>
                {
                    new RequirementDef { Item = "Wood", Amount = 1 }
                }
            });
            LoadPiecePrefab(assetBundle, "piece_trashcan", new PieceDef()
            {
                PieceTable = "_HammerPieceTable",
                //CraftingStation = "piece_workbench",  // no need to have a station?
                Resources = new List<RequirementDef>
                {
                    new RequirementDef { Item = "Stone", Amount = 1 }
                }
            });
            assetBundle.Unload(false);

            assetBundle = LoadAssetBundleFromResources("skunkitems");
            LoadItemPrefab(assetBundle, "SkunkBroadFireSword", new ItemDef()
            {
                Amount = 1,
                CraftingStation = "piece_workbench",
                RepairStation = "piece_workbench",
                Resources = new List<RequirementDef>
                {
                    new RequirementDef { Item = "Wood", Amount = 1 }
                }
            });
            LoadItemPrefab(assetBundle, "SkunkSword", new ItemDef()
            {
                Amount = 1,
                CraftingStation = "piece_workbench",
                RepairStation = "piece_workbench",
                Resources = new List<RequirementDef>
                {
                    new RequirementDef { Item = "Wood", Amount = 1 }
                }
            });
            LoadPiecePrefab(assetBundle, "Terrain", new PieceDef()
            {
                PieceTable = "_HoePieceTable"
            });
            assetBundle.Unload(false);
        }

        /// <summary>
        /// Try to get a path to <paramref name="fileName"/> from either &lt;assembly_path&gt;/&lt;plugin_name&gt;/
        /// or &lt;assembly_path&gt;/ as a fallback
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private string GetFilePath(string fileName)
        {
            var filePath = Path.Combine(Paths.PluginPath, VeilheimPlugin.PluginName, fileName);
            if (!File.Exists(filePath))
            {
                Assembly assembly = typeof(VeilheimPlugin).Assembly;
                filePath = Path.Combine(Path.GetDirectoryName(assembly.Location), fileName);
                if (!File.Exists(filePath))
                {
                    Logger.LogError($"Asset file {fileName} not found in filesystem");
                    return null;
                }
            }

            return filePath;
        }

        public Sprite LoadSpriteFromFile(string spritePath)
        {
            byte[] fileData = File.ReadAllBytes(GetFilePath(spritePath));
            Texture2D tex = new Texture2D(20, 20);
            if (tex.LoadImage(fileData))
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(), 100);
            }

            return null;
        }

        /// <summary>
        /// Load an assembly-embedded <see cref="AssetBundle"/>
        /// </summary>
        /// <param name="bundleName">Name of the bundle</param>
        /// <returns></returns>
        public AssetBundle LoadAssetBundleFromResources(string bundleName)
        {
            var execAssembly = Assembly.GetExecutingAssembly();

            var resourceName = execAssembly.GetManifestResourceNames()
                .Single(str => str.EndsWith(bundleName));

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
        /// Load an external <see cref="AssetBundle"/> 
        /// </summary>
        /// <param name="fileName">Filename of the bundle</param>
        /// <returns></returns>
        public AssetBundle LoadAssetBundleFromFile(string fileName)
        {
            var assetBundlePath = GetFilePath(fileName);
            return AssetBundle.LoadFromFile(assetBundlePath);
        }

        /// <summary>
        /// Load an "untyped" prefab from a bundle and register it in <see cref="AssetManager"/>.
        /// </summary>
        /// <param name="assetBundle"></param>
        /// <param name="assetName"></param>
        public void LoadPrefab(AssetBundle assetBundle, string assetName)
        {
            var prefab = assetBundle.LoadAsset<GameObject>(assetName);
            AssetManager.AddPrefab(prefab);
        }

        /// <summary>
        /// Load an item prefab from a bundle and register it in <see cref="AssetManager"/>.
        /// </summary>
        /// <param name="assetBundle"></param>
        /// <param name="assetName"></param>
        /// <param name="itemDef"></param>
        public void LoadItemPrefab(AssetBundle assetBundle, string assetName, ItemDef itemDef)
        {
            var prefab = assetBundle.LoadAsset<GameObject>(assetName);
            AssetManager.AddtemPrefab(prefab, itemDef);
            AssetManager.AddPrefab(prefab);
        }

        /// <summary>
        /// Load a piece prefab from a bundle and register it in <see cref="AssetManager"/>.
        /// </summary>
        /// <param name="assetBundle"></param>
        /// <param name="assetName"></param>
        /// <param name="pieceDef"></param>
        public void LoadPiecePrefab(AssetBundle assetBundle, string assetName, PieceDef pieceDef)
        {
            var prefab = assetBundle.LoadAsset<GameObject>(assetName);
            AssetManager.AddPiecePrefab(prefab, pieceDef);
            AssetManager.AddPrefab(prefab);
        }
    }
}
