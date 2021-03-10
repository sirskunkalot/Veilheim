using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Veilheim.Util
{
    class PieceDef
    {
        public string Table;
        public string PieceTable;
        public string CraftingStation;
        public string ExtendStation;
        public List<RecipeRequirementConfig> Resources = new List<RecipeRequirementConfig>();
    }

    /// <summary>
    /// Central class for loading and importing custom <see cref="AssetBundle"/>s into Valheim. 
    /// Code inspired from https://github.com/RandyKnapp/ValheimMods
    /// and https://github.com/Valheim-Modding/ValheimLib
    /// </summary>
    static class AssetLoader
    {
        public static readonly List<GameObject> RegisteredPrefabs = new List<GameObject>();
        public static readonly List<GameObject> RegisteredItemPrefabs = new List<GameObject>();
        public static readonly Dictionary<GameObject, PieceDef> RegisteredPieces = new Dictionary<GameObject, PieceDef>();

        public static void TryRegisterPrefabs(ZNetScene zNetScene)
        {
            if (zNetScene == null)
            {
                return;
            }

            foreach (var prefab in RegisteredPrefabs)
            {
                if (!zNetScene.m_prefabs.Contains(prefab))
                {
                    zNetScene.m_prefabs.Add(prefab);
                }
            }
        }

        public static void TryRegisterItems()
        {
            if (ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0)
            {
                return;
            }

            foreach (var prefab in RegisteredItemPrefabs)
            {
                var itemDrop = prefab.GetComponent<ItemDrop>();
                if (itemDrop != null)
                {
                    if (ObjectDB.instance.GetItemPrefab(prefab.name.GetStableHashCode()) == null)
                    {
                        ObjectDB.instance.m_items.Add(prefab);
                    }
                }
            }

            var pieceTables = new List<PieceTable>();
            foreach (var itemPrefab in ObjectDB.instance.m_items)
            {
                var item = itemPrefab.GetComponent<ItemDrop>().m_itemData;
                if (item.m_shared.m_buildPieces != null && !pieceTables.Contains(item.m_shared.m_buildPieces))
                {
                    pieceTables.Add(item.m_shared.m_buildPieces);
                }
            }

            var craftingStations = new List<CraftingStation>();
            foreach (var pieceTable in pieceTables)
            {
                craftingStations.AddRange(pieceTable.m_pieces
                    .Where(x => x.GetComponent<CraftingStation>() != null)
                    .Select(x => x.GetComponent<CraftingStation>()));
            }

            TryRegisterPieces(pieceTables, craftingStations);
        }

        public static void TryRegisterPieces(List<PieceTable> pieceTables, List<CraftingStation> craftingStations)
        {
            foreach (var entry in RegisteredPieces)
            {
                var prefab = entry.Key;
                var pieceDef = entry.Value;

                var piece = prefab.GetComponent<Piece>();

                var pieceTable = pieceTables.Find(x => x.name == pieceDef.Table);
                if (pieceTable.m_pieces.Contains(prefab))
                {
                    continue;
                }
                pieceTable.m_pieces.Add(prefab);

                var pieceStation = craftingStations.Find(x => x.name == pieceDef.CraftingStation);
                piece.m_craftingStation = pieceStation;

                var resources = new List<Piece.Requirement>();
                foreach (var resource in pieceDef.Resources)
                {
                    var resourcePrefab = ObjectDB.instance.GetItemPrefab(resource.item);
                    resources.Add(new Piece.Requirement()
                    {
                        m_resItem = resourcePrefab.GetComponent<ItemDrop>(),
                        m_amount = resource.amount
                    });
                }
                piece.m_resources = resources.ToArray();

                var stationExt = prefab.GetComponent<StationExtension>();
                if (stationExt != null && !string.IsNullOrEmpty(pieceDef.ExtendStation))
                {
                    var stationPrefab = pieceTable.m_pieces.Find(x => x.name == pieceDef.ExtendStation);
                    if (stationPrefab != null)
                    {
                        var station = stationPrefab.GetComponent<CraftingStation>();
                        stationExt.m_craftingStation = station;
                    }

                    var otherExt = pieceTable.m_pieces.Find(x => x.GetComponent<StationExtension>() != null);
                    if (otherExt != null)
                    {
                        var otherStationExt = otherExt.GetComponent<StationExtension>();
                        var otherPiece = otherExt.GetComponent<Piece>();

                        stationExt.m_connectionPrefab = otherStationExt.m_connectionPrefab;
                        piece.m_placeEffect.m_effectPrefabs = otherPiece.m_placeEffect.m_effectPrefabs.ToArray();
                    }
                }
                else
                {
                    var otherPiece = pieceTable.m_pieces.Find(x => x.GetComponent<Piece>() != null).GetComponent<Piece>();
                    piece.m_placeEffect.m_effectPrefabs.AddRangeToArray(otherPiece.m_placeEffect.m_effectPrefabs);
                }
            }
        }

        public static AssetBundle LoadAssetBundleFromResources(string fileName)
        {
            var execAssembly = Assembly.GetExecutingAssembly();

            var resourceName = execAssembly.GetManifestResourceNames()
                .Single(str => str.EndsWith(fileName));

            AssetBundle ret;
            using (var stream = execAssembly.GetManifestResourceStream(resourceName))
            {
                ret = AssetBundle.LoadFromStream(stream);
            }

            return ret;
        }

        public static AssetBundle LoadAssetBundleFromFile(string fileName)
        {
            var assetBundlePath = GetAssetPath(fileName);
            if (!string.IsNullOrEmpty(assetBundlePath))
            {
                return AssetBundle.LoadFromFile(assetBundlePath);
            }

            return null;
        }

        private static string GetAssetPath(string assetName, bool ignoreErrors = false)
        {
            var assetFileName = Path.Combine(Paths.PluginPath, nameof(VeilheimPlugin), assetName);
            if (!File.Exists(assetFileName))
            {
                Assembly assembly = typeof(VeilheimPlugin).Assembly;
                assetFileName = Path.Combine(Path.GetDirectoryName(assembly.Location), assetName);
                if (!File.Exists(assetFileName))
                {
                    Logger.LogError($"Could not find asset ({assetName})");
                    return null;
                }
            }

            return assetFileName;
        }

        public static void LoadItemPrefab(AssetBundle assetBundle, string assetName)
        {
            var prefab = assetBundle.LoadAsset<GameObject>(assetName);
            RegisteredItemPrefabs.Add(prefab);
        }
        public static void LoadPiecePrefab(AssetBundle assetBundle, string assetName, PieceDef pieceDef)
        {
            var prefab = assetBundle.LoadAsset<GameObject>(assetName);
            RegisteredPieces.Add(prefab, pieceDef);
            RegisteredPrefabs.Add(prefab);
        }
    }

    [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
    public static class ObjectDB_CopyOtherDB_Patch
    {
        public static void Postfix()
        {
            AssetLoader.TryRegisterItems();
            ObjectDB.instance.UpdateItemHashes();
        }
    }

    [HarmonyPatch(typeof(ObjectDB), "Awake")]
    public static class ObjectDB_Awake_Patch
    {
        public static void Postfix()
        {
            AssetLoader.TryRegisterItems();
            ObjectDB.instance.UpdateItemHashes();
        }
    }

    [HarmonyPatch(typeof(ZNetScene), "Awake")]
    public static class ZNetScene_Awake_Patch
    {
        public static bool Prefix(ZNetScene __instance)
        {
            AssetLoader.TryRegisterPrefabs(__instance);
            return true;
        }
    }
}
