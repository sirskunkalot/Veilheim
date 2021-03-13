using BepInEx;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Veilheim.AssetUtils
{
    //TODO: Make non-static, make Destroyable, dont hardcode prefab and bundle names

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
        /// Load an external <see cref="AssetBundle"/> from either &lt;assembly path&gt;/&lt;plugin name&gt;/$fileName
        /// or &lt;assembly path&gt;/$fileName as a fallback
        /// </summary>
        /// <param name="fileName">Filename of the bundle</param>
        /// <returns></returns>
        public AssetBundle LoadAssetBundleFromFile(string fileName)
        {
            var assetBundlePath = Path.Combine(Paths.PluginPath, VeilheimPlugin.PluginName, fileName);
            if (!File.Exists(assetBundlePath))
            {
                Assembly assembly = typeof(VeilheimPlugin).Assembly;
                assetBundlePath = Path.Combine(Path.GetDirectoryName(assembly.Location), fileName);
                if (!File.Exists(assetBundlePath))
                {
                    Logger.LogError($"AssetBundle file {fileName} not found in filesystem");
                    return null;
                }
            }

            return AssetBundle.LoadFromFile(assetBundlePath);
        }

        /// <summary>
        /// Load an "untyped" prefab from a bundle and register it with this class.<br />
        /// The "untyped" prefabs are added to the <see cref="ZNetScene"/> on initialization.
        /// </summary>
        /// <param name="assetBundle"></param>
        /// <param name="assetName"></param>
        public void LoadPrefab(AssetBundle assetBundle, string assetName)
        {
            var prefab = assetBundle.LoadAsset<GameObject>(assetName);
            AssetManager.AddPrefab(prefab);
        }

        /// <summary>
        /// Load an item prefab from a bundle and register it with this class.<br />
        /// The item prefabs are added to the <see cref="ObjectDB"/> and <see cref="ZNetScene"/> on initialization.<br />
        /// A <see cref="Recipe"/> is created and added automatically, when a <see cref="CraftingStation"/> and the 
        /// <see cref="Piece.Requirement"/>s are defined in the <see cref="ItemDef"/>.
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
        /// Load a piece prefab from a bundle and register it with this class.<br />
        /// The piece prefabs are added to the <see cref="ZNetScene"/> on initialization.<br />
        /// The <see cref="Piece"/> is added to the <see cref="PieceTable"/> defined in <see cref="PieceDef"/> automatically.<br />
        /// When ExtensionStation is defined, the <see cref="Piece"/> is added as a <see cref="StationExtension"/>.
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
