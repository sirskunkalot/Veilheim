﻿using BepInEx;
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
        public int Amount = 1;
    }

    /// <summary>
    /// A wrapper class representing certain references to Valheim objects for a <see cref="Piece"/>
    /// as primitives. Must be instantiated for every <see cref="Piece"/> from an <see cref="AssetBundle"/>
    /// that you want to register. The actual objects are instantiated and referenced at runtime.
    /// </summary>
    class PieceDef
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
    class ItemDef
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

        /// <summary>
        /// Load an "untyped" prefab from a bundle and register it with this class.<br />
        /// The "untyped" prefabs are added to the <see cref="ZNetScene"/> on initialization.
        /// </summary>
        /// <param name="assetBundle"></param>
        /// <param name="assetName"></param>
        public static void LoadPrefab(AssetBundle assetBundle, string assetName)
        {
            var prefab = assetBundle.LoadAsset<GameObject>(assetName);
            RegisteredPrefabs.Add(prefab);
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
        public static void LoadItemPrefab(AssetBundle assetBundle, string assetName, ItemDef itemDef)
        {
            var prefab = assetBundle.LoadAsset<GameObject>(assetName);
            RegisteredItems.Add(prefab, itemDef);
            RegisteredPrefabs.Add(prefab);
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
        public static void LoadPiecePrefab(AssetBundle assetBundle, string assetName, PieceDef pieceDef)
        {
            var prefab = assetBundle.LoadAsset<GameObject>(assetName);
            RegisteredPieces.Add(prefab, pieceDef);
            RegisteredPrefabs.Add(prefab);
        }

        /// <summary>
        /// Add all loaded prefabs to the namedPrefabs in <see cref="ZNetScene"/>.
        /// </summary>
        /// <param name="instance"></param>
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
        /// <summary>
        /// Initialize and register all loaded items and pieces to the current instance of the <see cref="ObjectDB"/>.
        /// </summary>
        public static void AddToObjectDB(ObjectDB instance)
        {
            if (instance == null || instance.m_items.Count == 0)
            {
                return;
            }

            TryRegisterItems(instance);
            TryRegisterPieces(instance);
        }

        private static void TryRegisterItems(ObjectDB instance)
        {
            Logger.LogMessage($"Registering custom items in ObjectDB {instance}");

            // Collect all current CraftingStations from the recipes in ObjectDB
            var craftingStations = new List<CraftingStation>();
            foreach (var recipe in instance.m_recipes)
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

                // Create the Recipe for this item, defined in ItemDef
                var recipe = CreateRecipe(instance, prefab, itemDef, craftingStations);

                // Add the Recipe to the ObjectDB, remove one with the same name first
                var removed = instance.m_recipes.RemoveAll(x => x.name == recipe.name);
                if (removed > 0)
                {
                    Logger.LogDebug($"Removed recipes ({recipe.name}): {removed}");
                }

                instance.m_recipes.Add(recipe);
                Logger.LogInfo($"Added recipe: {recipe.name}");
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
        private static Recipe CreateRecipe(ObjectDB instance, GameObject prefab, ItemDef itemDef, List<CraftingStation> craftingStations)
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
        private static void TryRegisterPieces(ObjectDB instance)
        {
            Logger.LogMessage($"Registering custom pieces in ObjectDB {instance}");

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
                var pieceTable = pieceTables.Find(x => x.name == pieceDef.PieceTable);
                if (pieceTable.m_pieces.Contains(prefab))
                {
                    Logger.LogDebug($"Piece already added to PieceTable {pieceDef.PieceTable}");
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
