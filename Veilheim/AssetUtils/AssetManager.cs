using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Veilheim.AssetUtils
{
    /// <summary>
    /// A wrapper class representing <see cref="Piece.Requirement"/>s as primitives.
    /// Valheim objects are instantiated and referenced at runtime.
    /// </summary>
    internal class RequirementDef
    {
        public string Item;
        public int Amount = 1;
    }

    /// <summary>
    /// A wrapper class representing certain references to Valheim objects for a <see cref="Piece"/>
    /// as primitives. Must be instantiated for every <see cref="Piece"/> from an <see cref="AssetBundle"/>
    /// that you want to register. The actual objects are instantiated and referenced at runtime.
    /// </summary>
    internal class PieceDef
    {
        public string PieceTable = string.Empty;
        public string CraftingStation = string.Empty;
        public string ExtendStation = string.Empty;
        public List<RequirementDef> Resources = new List<RequirementDef>();
    }

    /// <summary>
    /// A wrapper class representing certain references to Valheim objects and attributes of 
    /// <see cref="ItemDrop"/>s and <see cref="Recipe"/>s as primitives. Must be instantiated 
    /// for every item prefab that you want to register. 
    /// The actual objects are instantiated and referenced at runtime.
    /// </summary>
    internal class ItemDef
    {
        public bool Enabled = true;
        public int Amount = 1;
        public int MinStationLevel = 1;
        public string CraftingStation = string.Empty;
        public string RepairStation = string.Empty;
        public List<RequirementDef> Resources = new List<RequirementDef>();
    }

    /// <summary>
    /// Central class for loading and importing custom <see cref="AssetBundle"/>s into Valheim. 
    /// Code inspired by <a href="https://github.com/RandyKnapp/ValheimMods"/>
    /// </summary>
    internal class AssetManager : IDestroyable
    {
        private readonly List<GameObject> RegisteredPrefabs = new List<GameObject>();
        private readonly List<AssetLocalization> RegisteredLocalizations = new List<AssetLocalization>();
        private readonly Dictionary<GameObject, ItemDef> RegisteredItems = new Dictionary<GameObject, ItemDef>();
        private readonly Dictionary<GameObject, PieceDef> RegisteredPieces = new Dictionary<GameObject, PieceDef>();

        private Dictionary<string, CraftingStation> CraftingStations;

        public static AssetManager Instance;

        public AssetManager()
        {
            Instance = this;
        }

        public void Destroy()
        {
            Logger.LogDebug("Destroying AssetManager");

            foreach (var localization in RegisteredLocalizations)
            {
                localization.Destroy();
                Logger.LogDebug($"Localization {localization} destroyed");
            }
            RegisteredLocalizations.Clear();

            foreach (var prefab in RegisteredPrefabs)
            {
                UnityEngine.Object.Destroy(prefab);
                Logger.LogDebug($"Prefab {prefab.name} destroyed");
            }
            RegisteredPrefabs.Clear();
            RegisteredItems.Clear();
            RegisteredPieces.Clear();

            Instance = null;

            Logger.LogDebug("AssetManager destroyed");
        }

        /// <summary>
        /// Register an "untyped" prefab.<br />
        /// The "untyped" prefabs are added to the current <see cref="ZNetScene"/> on initialization.
        /// </summary>
        /// <param name="prefab"></param>
        public static void RegisterPrefab(GameObject prefab)
        {
            if (!Instance.RegisteredPrefabs.Contains(prefab))
            {
                Instance.RegisteredPrefabs.Add(prefab);
            }
        }

        /// <summary>
        /// Register an <see cref="ItemDrop"/> prefab.<br />
        /// The item prefabs are added to the current <see cref="ObjectDB"/> and <see cref="ZNetScene"/> on initialization.<br />
        /// A <see cref="Recipe"/> is created and added automatically, when a <see cref="CraftingStation"/> and the 
        /// <see cref="Piece.Requirement"/>s are defined in the <see cref="ItemDef"/>.
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="itemDef"></param>
        public static void RegisterItemPrefab(GameObject prefab, ItemDef itemDef)
        {
            if (!Instance.RegisteredPrefabs.Contains(prefab))
            {
                Instance.RegisteredPrefabs.Add(prefab);
                Instance.RegisteredItems.Add(prefab, itemDef);
            }
        }

        /// <summary>
        /// Register a <see cref="Piece"/> prefab.<br />
        /// The piece prefabs are added to the current <see cref="ZNetScene"/> on initialization.<br />
        /// The <see cref="Piece"/> is added to the <see cref="PieceTable"/> defined in <see cref="PieceDef"/> automatically.<br />
        /// When ExtensionStation is defined in <see cref="PieceDef"/>, the <see cref="Piece"/> is added as a <see cref="StationExtension"/> to that station.
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="pieceDef"></param>
        public static void RegisterPiecePrefab(GameObject prefab, PieceDef pieceDef)
        {
            if (!Instance.RegisteredPrefabs.Contains(prefab))
            {
                Instance.RegisteredPrefabs.Add(prefab);
                Instance.RegisteredPieces.Add(prefab, pieceDef);
            }
        }

        /// <summary>
        /// Register an <see cref="AssetLocalization"/>.<br />
        /// </summary>
        /// <param name="localization"></param>
        public static void RegisterLocalization(AssetLocalization localization)
        {
            if (!Instance.RegisteredLocalizations.Contains(localization))
            {
                localization.SetupLanguage(Localization.instance.GetSelectedLanguage());
                Instance.RegisteredLocalizations.Add(localization);
            }
        }

        /// <summary>
        /// Add all registered prefabs to the namedPrefabs in <see cref="ZNetScene"/>.
        /// </summary>
        /// <param name="instance"></param>
        public static void AddToZNetScene(ZNetScene instance)
        {
            if (instance == null)
            {
                return;
            }

            Logger.LogMessage("Adding custom prefabs to ZNetScene");

            foreach (var prefab in Instance.RegisteredPrefabs)
            {
                Logger.LogDebug($"GameObject: {prefab.name}");

                if (!instance.m_namedPrefabs.ContainsKey(prefab.name.GetStableHashCode()))
                {
                    instance.m_namedPrefabs.Add(prefab.name.GetStableHashCode(), prefab);
                    Logger.LogInfo($"Added {prefab.name}");
                }
            }
        }

        /// <summary>
        /// Initialize and register all loaded items to the <see cref="ObjectDB"/> in <see cref="FejdStartup"/> (no recipes and pieces needed)
        /// </summary>
        public static void AddToObjectDBFejd(ObjectDB instance)
        {
            if (instance == null || instance.m_items.Count == 0)
            {
                return;
            }

            Instance.TryRegisterItems(instance, false);
        }

        /// <summary>
        /// Initialize and register all loaded items and pieces to the current instance of the <see cref="ObjectDB"/>.
        /// </summary>
        public static void AddToObjectDB(ObjectDB instance)
        {
            if (instance == null || instance.m_items.Count == 0)
            {
                return;
            }

            Instance.TryRegisterItems(instance, true);
            Instance.TryRegisterPieces(instance);
        }

        /// <summary>
        /// Setup languages for all registered <see cref="AssetLocalization"/>s
        /// </summary>
        /// <param name="language"></param>
        public static void SetupLanguage(string language)
        {
            foreach (var localization in Instance.RegisteredLocalizations)
            {
                localization.SetupLanguage(language);
            }
        }

        /// <summary>
        /// Try to translate a string in all registered <see cref="AssetLocalization"/>s. First to translate wins.
        /// </summary>
        /// <param name="word"></param>
        /// <param name="translated"></param>
        public static bool TryTranslate(string word, out string translated)
        {
            bool isTranslated = false;
            string _translated = "";
            
            // first translation wins
            foreach (var localization in Instance.RegisteredLocalizations)
            {
                isTranslated = localization.TryTranslate(word, out _translated);
                if (isTranslated) break;
            }

            translated = isTranslated ? _translated : $"[{word}]";

            return isTranslated;
        }

        private void InitCraftingStations(ObjectDB instance)
        {
            if (CraftingStations == null)
            {
                CraftingStations = new Dictionary<string, CraftingStation>();

                // Collect all current PieceTables from the items in ObjectDB
                var pieceTables = new List<PieceTable>();
                foreach (var itemPrefab in instance.m_items)
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
                    foreach (var station in pieceTable.m_pieces
                        .Where(x => x.GetComponent<CraftingStation>() != null)
                        .Select(x => x.GetComponent<CraftingStation>()))
                    {
                        if (!CraftingStations.ContainsKey(station.name))
                        {
                            CraftingStations.Add(station.name, station);
                        }
                    }
                }

                // Collect all CraftingStations from the recipes in ObjectDB
                foreach (var recipe in instance.m_recipes)
                {
                    if (recipe.m_craftingStation != null && !CraftingStations.ContainsKey(recipe.m_craftingStation.name))
                    {
                        CraftingStations.Add(recipe.m_craftingStation.name, recipe.m_craftingStation);
                    }
                }
            }
        }

        private void TryRegisterItems(ObjectDB instance, bool createRecipes)
        {
            Logger.LogMessage($"Registering custom items in ObjectDB {instance}");

            Instance.InitCraftingStations(instance);
            
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
                    if (instance.m_itemByHash.ContainsKey(prefab.name.GetStableHashCode()))
                    {
                        Logger.LogWarning("Item already added to ObjectDB");
                        continue;
                    }
                    else
                    {
                        itemDrop.m_itemData.m_dropPrefab = prefab;
                        instance.m_items.Add(prefab);
                    }
                }

                Logger.LogInfo($"Registered item {prefab.name}");

                if (createRecipes)
                {
                    // Create the Recipe for this item, defined in ItemDef
                    var recipe = CreateRecipe(instance, prefab, itemDef);

                    // Add the Recipe to the ObjectDB, remove one with the same name first
                    var removed = instance.m_recipes.RemoveAll(x => x.name == recipe.name);
                    if (removed > 0)
                    {
                        Logger.LogDebug($"Removed recipes ({recipe.name}): {removed}");
                    }

                    instance.m_recipes.Add(recipe);
                    Logger.LogInfo($"Added recipe: {recipe.name}");
                }
            }

            // If we registered items, update their hashes
            if (instance.m_items.Count() > instance.m_itemByHash.Count())
            {
                Logger.LogInfo("Updating item hashes");
                instance.UpdateItemHashes();
            }
        }

        /// <summary>
        /// Create a <see cref="Recipe"/> for a prefab based on the <see cref="ItemDef"/> of this custom item
        /// </summary>
        /// <param name="prefab">The item for which the recipe will be created</param>
        /// <param name="itemDef"></param>
        /// <param name="craftingStations">List of stations which are allowed to act as the crafting and repair station for this item</param>
        /// <returns></returns>
        private Recipe CreateRecipe(ObjectDB instance, GameObject prefab, ItemDef itemDef)
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
                var craftingStation = CraftingStations.GetValueSafe(itemDef.CraftingStation);
                if (craftingStation == null)
                {
                    Logger.LogWarning($"Could not find crafting station: {itemDef.CraftingStation}");
                    var stationList = string.Join(", ", CraftingStations.Keys);
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
                var repairStation = CraftingStations.GetValueSafe(itemDef.RepairStation);
                if (repairStation == null)
                {
                    Logger.LogWarning($"Could not find repair station: {itemDef.RepairStation}");
                    var stationList = string.Join(", ", CraftingStations.Keys);
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
                var reqPrefab = instance.GetItemPrefab(requirement.Item);
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
        private void TryRegisterPieces(ObjectDB instance)
        {
            Logger.LogMessage($"Registering custom pieces in ObjectDB {instance}");

            // Get CraftingStations if necessary
            Instance.InitCraftingStations(instance);

            // Collect all current PieceTables from the items in ObjectDB
            var pieceTables = new List<PieceTable>();
            foreach (var itemPrefab in instance.m_items)
            {
                var item = itemPrefab.GetComponent<ItemDrop>().m_itemData;
                if (item.m_shared.m_buildPieces != null && !pieceTables.Contains(item.m_shared.m_buildPieces))
                {
                    pieceTables.Add(item.m_shared.m_buildPieces);
                }
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
                    Logger.LogError("GameObject has no Piece attached");
                    continue;
                }

                if (pieceDef == null)
                {
                    Logger.LogError("No PieceDef available");
                    continue;
                }

                // Assign the piece to the actual PieceTable if not already in there
                var pieceTable = pieceTables.Find(x => x.name == pieceDef.PieceTable);
                if (pieceTable == null)
                {
                    Logger.LogWarning($"Could not find piecetable: {pieceDef.PieceTable}");
                    continue;
                }
                if (pieceTable.m_pieces.Contains(prefab))
                {
                    Logger.LogDebug($"Piece already added to PieceTable {pieceDef.PieceTable}");
                    continue;
                }
                pieceTable.m_pieces.Add(prefab);

                // Assign the CraftingStation for this piece, if needed
                if (!string.IsNullOrEmpty(pieceDef.CraftingStation))
                {
                    var pieceStation = CraftingStations.GetValueSafe(pieceDef.CraftingStation);
                    if (pieceStation == null)
                    {
                        Logger.LogWarning($"Could not find crafting station: {pieceDef.CraftingStation}");
                        var stationList = string.Join(", ", CraftingStations.Keys);
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
                    var resourcePrefab = instance.GetItemPrefab(resource.Item);
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
