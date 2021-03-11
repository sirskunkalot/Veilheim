using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Veilheim.AssetUtils
{
    /// <summary>
    /// A wrapper class representing <see cref="Piece.Requirement"/>s as primitives.
    /// Valheim objects are instantiated and referenced at runtime.
    /// </summary>
    class RequirementDef
    {
        public string Item;
        public int Amount;
    }

    /// <summary>
    /// A wrapper class representing <see cref="Piece"/>s as primitives.
    /// Valheim objects are instantiated and referenced at runtime.
    /// </summary>
    class PieceDef
    {
        public string Table;
        public string PieceTable;
        public string CraftingStation;
        public string ExtendStation;
        public List<RequirementDef> Resources = new List<RequirementDef>();
    }

    /// <summary>
    /// A wrapper class representing <see cref="ItemDrop"/>s and <see cref="Recipe"/>s as primitives.
    /// Valheim objects are instantiated and referenced at runtime.
    /// </summary>
    class ItemDef
    {
        public int Amount = 1;
        public string CraftingStation;
        public int MinStationLevel = 1;
        public bool Enabled = true;
        public string RepairStation;
        public List<RequirementDef> Resources = new List<RequirementDef>();
    }

    /// <summary>
    /// Central class for loading and importing custom <see cref="AssetBundle"/>s into Valheim. 
    /// Code inspired by <a href="https://github.com/RandyKnapp/ValheimMods"/>
    /// </summary>
    static class AssetLoader
    {
        private static readonly List<GameObject> RegisteredPrefabs = new List<GameObject>();
        private static readonly Dictionary<GameObject, ItemDef> RegisteredItems = new Dictionary<GameObject, ItemDef>();
        private static readonly Dictionary<GameObject, PieceDef> RegisteredPieces = new Dictionary<GameObject, PieceDef>();

        /// <summary>
        /// Load an assembly-embedded <see cref="AssetBundle"/>
        /// </summary>
        /// <param name="bundleName">Name of the bundle</param>
        /// <returns></returns>
        public static AssetBundle LoadAssetBundleFromResources(string bundleName)
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
        public static AssetBundle LoadAssetBundleFromFile(string fileName)
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

        public static void LoadPrefab(AssetBundle assetBundle, string assetName)
        {
            var prefab = assetBundle.LoadAsset<GameObject>(assetName);
            RegisteredPrefabs.Add(prefab);
        }
        public static void LoadItemPrefab(AssetBundle assetBundle, string assetName, ItemDef itemDef)
        {
            var prefab = assetBundle.LoadAsset<GameObject>(assetName);
            RegisteredItems.Add(prefab, itemDef);
            RegisteredPrefabs.Add(prefab);
        }
        public static void LoadPiecePrefab(AssetBundle assetBundle, string assetName, PieceDef pieceDef)
        {
            var prefab = assetBundle.LoadAsset<GameObject>(assetName);
            RegisteredPieces.Add(prefab, pieceDef);
            RegisteredPrefabs.Add(prefab);
        }

        public static void AddToZNetScene(ZNetScene instance)
        {
            if (instance == null)
            {
                return;
            }

            Logger.LogMessage("Adding custom prefabs to ZNetScene");

            foreach (var prefab in RegisteredPrefabs)
            {
                Logger.LogDebug($"GameObject: {prefab.name}");

                if (!instance.m_namedPrefabs.ContainsKey(prefab.name.GetStableHashCode()))
                {
                    instance.m_namedPrefabs.Add(prefab.name.GetStableHashCode(), prefab);
                    Logger.LogInfo($"Added {prefab.name}");
                }
            }
        }
        public static void AddToObjectDB()
        {
            if (ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0)
            {
                return;
            }

            TryRegisterItems();
            TryRegisterPieces();
        }

        private static void TryRegisterItems()
        {
            Logger.LogMessage("Registering custom items");

            // Collect all current CraftingStations from the recipes in ObjectDB
            var craftingStations = new List<CraftingStation>();
            foreach (var recipe in ObjectDB.instance.m_recipes)
            {
                if (recipe.m_craftingStation != null && !craftingStations.Contains(recipe.m_craftingStation))
                {
                    craftingStations.Add(recipe.m_craftingStation);
                }
            }

            // Go through all registered Items and try to obtain references
            // to the actual objects defined as strings in ItemDef
            foreach (var entry in RegisteredItems)
            {
                Logger.LogDebug($"GameObject: {entry.Key.name}");

                var prefab = entry.Key;
                var itemDef = entry.Value;

                // Add the item prefab to the ObjectDB if not already in there
                var itemDrop = prefab.GetComponent<ItemDrop>();
                if (itemDrop == null)
                {
                    Logger.LogError($"GameObject {prefab.name} has no ItemDrop attached");
                    continue;
                }
                else
                {
                    if (ObjectDB.instance.GetItemPrefab(prefab.name.GetStableHashCode()) != null)
                    {
                        Logger.LogWarning("Item already added to ObjectDB");
                        continue;
                    }
                    else
                    {
                        itemDrop.m_itemData.m_dropPrefab = prefab;
                        ObjectDB.instance.m_items.Add(prefab);
                    }
                }

                Logger.LogInfo($"Registered item {prefab.name}");

                // Create the Recipe for this item, defined via ItemDef
                var recipe = CreateRecipe(prefab, itemDef, craftingStations);

                // Add the Recipe to the ObjectDB, remove one with the same name first
                var removed = ObjectDB.instance.m_recipes.RemoveAll(x => x.name == recipe.name);
                if (removed > 0)
                {
                    Logger.LogDebug($"Removed recipes ({recipe.name}): {removed}");
                }

                ObjectDB.instance.m_recipes.Add(recipe);
                Logger.LogInfo($"Added recipe: {recipe.name}");
            }

            // If we tried to register items, update their hashes
            if (RegisteredItems.Count() > 0)
            {
                Logger.LogInfo("Updating item hashes");
                ObjectDB.instance.UpdateItemHashes();
            }
        }

        /// <summary>
        /// Create a <see cref="Recipe"/> for a prefab based on the <see cref="ItemDef"/> of this custom item
        /// </summary>
        /// <param name="prefab">The item for which the recipe will be created</param>
        /// <param name="itemDef"></param>
        /// <param name="craftingStations">List of stations which are allowed to act as the crafting and repair station for this item</param>
        /// <returns></returns>
        private static Recipe CreateRecipe(GameObject prefab, ItemDef itemDef, List<CraftingStation> craftingStations)
        {
            var newRecipe = ScriptableObject.CreateInstance<Recipe>();
            newRecipe.name = $"Recipe_{prefab.name}";
            newRecipe.m_amount = itemDef.Amount;
            newRecipe.m_minStationLevel = itemDef.MinStationLevel;
            newRecipe.m_item = prefab.GetComponent<ItemDrop>();
            newRecipe.m_enabled = itemDef.Enabled;

            // Assign the crafting station for this Recipe if defined in ItemDef
            if (!string.IsNullOrEmpty(itemDef.CraftingStation))
            {
                var craftingStation = craftingStations.Find(x => x.name == itemDef.CraftingStation);
                if (craftingStation == null)
                {
                    Logger.LogWarning($"Could not find crafting station: {itemDef.CraftingStation}");
                    var stationList = string.Join(", ", craftingStations);
                    Logger.LogDebug($"Available Stations: {stationList}");
                }
                else
                {
                    newRecipe.m_craftingStation = craftingStation;
                }
            }

            // Assign the repair station for this recipe if defined in ItemDef
            if (!string.IsNullOrEmpty(itemDef.RepairStation))
            {
                var repairStation = craftingStations.Find(x => x.name == itemDef.RepairStation);
                if (repairStation == null)
                {
                    Logger.LogWarning($"Could not find repair station: {itemDef.RepairStation}");
                    var stationList = string.Join(", ", craftingStations);
                    Logger.LogDebug($"Available Stations: {stationList}");
                }
                else
                {
                    newRecipe.m_repairStation = repairStation;
                }
            }

            // Create a requirement list and assign instances of the requirement prefabs to it
            var reqs = new List<Piece.Requirement>();
            foreach (var requirement in itemDef.Resources)
            {
                var reqPrefab = ObjectDB.instance.GetItemPrefab(requirement.Item);
                if (reqPrefab == null)
                {
                    Logger.LogError($"Could not load requirement item: {requirement.Item}");
                    continue;
                }

                reqs.Add(new Piece.Requirement()
                {
                    m_amount = requirement.Amount,
                    m_resItem = reqPrefab.GetComponent<ItemDrop>()
                });
            }
            newRecipe.m_resources = reqs.ToArray();

            return newRecipe;
        }

        /// <summary>
        /// Register our custom building pieces to their respective ingame items or stations
        /// </summary>
        private static void TryRegisterPieces()
        {
            Logger.LogMessage("Registering custom pieces");

            // Collect all current PieceTables from the items in ObjectDB
            var pieceTables = new List<PieceTable>();
            foreach (var itemPrefab in ObjectDB.instance.m_items)
            {
                var item = itemPrefab.GetComponent<ItemDrop>().m_itemData;
                if (item.m_shared.m_buildPieces != null && !pieceTables.Contains(item.m_shared.m_buildPieces))
                {
                    pieceTables.Add(item.m_shared.m_buildPieces);
                }
            }

            // Collect all possible CraftingStations from the collected PieceTables
            var craftingStations = new List<CraftingStation>();
            foreach (var pieceTable in pieceTables)
            {
                craftingStations.AddRange(pieceTable.m_pieces
                    .Where(x => x.GetComponent<CraftingStation>() != null)
                    .Select(x => x.GetComponent<CraftingStation>()));
            }

            // Go through all registered Pieces and try to obtain references
            // to the actual objects defined as strings in PieceDef
            foreach (var entry in RegisteredPieces)
            {
                Logger.LogDebug($"GameObject: {entry.Key.name}");

                var prefab = entry.Key;
                var pieceDef = entry.Value;

                var piece = prefab.GetComponent<Piece>();

                if (piece == null)
                {
                    Logger.LogError($"GameObject {prefab} has no Piece attached");
                    continue;
                }

                // Assign the piece to the actual PieceTable if not already in there
                var pieceTable = pieceTables.Find(x => x.name == pieceDef.Table);
                if (pieceTable.m_pieces.Contains(prefab))
                {
                    Logger.LogDebug($"Piece already added to PieceTable {pieceDef.Table}");
                    continue;
                }
                pieceTable.m_pieces.Add(prefab);

                // Assign the needed CraftingStation for this piece, if needed
                if (!string.IsNullOrEmpty(pieceDef.CraftingStation))
                {
                    var pieceStation = craftingStations.Find(x => x.name == pieceDef.CraftingStation);
                    if (pieceStation == null)
                    {
                        Logger.LogWarning($"Could not find crafting station: {pieceDef.CraftingStation}");
                        var stationList = string.Join(", ", craftingStations);
                        Logger.LogDebug($"Available Stations: {stationList}");
                    }
                    else
                    {
                        piece.m_craftingStation = pieceStation;
                    }
                }

                // Assign all needed resources for this piece
                var resources = new List<Piece.Requirement>();
                foreach (var resource in pieceDef.Resources)
                {
                    var resourcePrefab = ObjectDB.instance.GetItemPrefab(resource.Item);
                    if (resourcePrefab == null)
                    {
                        Logger.LogError($"Could not load requirement item: {resource.Item}");
                        continue;
                    }

                    resources.Add(new Piece.Requirement()
                    {
                        m_resItem = resourcePrefab.GetComponent<ItemDrop>(),
                        m_amount = resource.Amount
                    });
                }
                piece.m_resources = resources.ToArray();

                // Try to assign the effect prefabs of another extension defined in ExtendStation
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
                // Otherwise just copy the effect prefabs of any piece within the table
                else
                {
                    var otherPiece = pieceTable.m_pieces.Find(x => x.GetComponent<Piece>() != null).GetComponent<Piece>();
                    piece.m_placeEffect.m_effectPrefabs.AddRangeToArray(otherPiece.m_placeEffect.m_effectPrefabs);
                }

                Logger.LogInfo($"Registered Piece {prefab.name}");
            }
        }
    }
}
