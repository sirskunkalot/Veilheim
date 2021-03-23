// Veilheim
// a Valheim mod
// 
// File:    AssetManager.cs
// Project: Veilheim

using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using Veilheim.AssetEntities;
using Veilheim.PatchEvents;

namespace Veilheim.AssetUtils
{

    /// <summary>
    ///     Central class for loading and importing custom <see cref="AssetBundle" />s into Valheim.
    /// </summary>
    internal class AssetManager : IPatchEventConsumer, IDestroyable
    {
        internal static AssetManager Instance { get; private set; }
        private readonly Dictionary<string, GameObject> RegisteredPrefabs = new Dictionary<string, GameObject>();
        private readonly Dictionary<GameObject, RecipeDef> RegisteredItems = new Dictionary<GameObject, RecipeDef>();
        private readonly Dictionary<GameObject, PieceDef> RegisteredPieces = new Dictionary<GameObject, PieceDef>();
        private readonly List<AssetLocalization> RegisteredLocalizations = new List<AssetLocalization>();

        private Dictionary<string, CraftingStation> CraftingStations = new Dictionary<string, CraftingStation>();
        private Dictionary<string, PieceTable> PieceTables = new Dictionary<string, PieceTable>();

        public static void Init()
        {
            if (Instance == null)
            {
                Instance = new AssetManager();
            }
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
                Object.Destroy(prefab.Value);
                Logger.LogDebug($"Prefab {prefab.Key} destroyed");
            }

            RegisteredPrefabs.Clear();
            RegisteredItems.Clear();
            RegisteredPieces.Clear();

            Instance = null;

            Logger.LogDebug("AssetManager destroyed");
        }

        /// <summary>
        ///     Register an "untyped" prefab.<br />
        ///     The "untyped" prefabs are added to the current <see cref="ZNetScene" /> on initialization.
        /// </summary>
        /// <param name="prefab"></param>
        public void RegisterPrefab(GameObject prefab)
        {
            if (!RegisteredPrefabs.ContainsKey(prefab.name))
            {
                RegisteredPrefabs.Add(prefab.name, prefab);
            }
        }

        /// <summary>
        ///     <br />
        ///     A <see cref="Recipe" /> is created and added automatically, when a <see cref="CraftingStation" /> and the
        ///     <see cref="Piece.Requirement" />s are defined in the <see cref="recipeDef" />.
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="RecipeDef"></param>
        public void RegisterItemPrefab(GameObject prefab, RecipeDef RecipeDef)
        {
            if (!RegisteredPrefabs.ContainsKey(prefab.name))
            {
                RegisteredPrefabs.Add(prefab.name, prefab);
                RegisteredItems.Add(prefab, RecipeDef);
            }
        }

        /// <summary>
        ///     Register a <see cref="Piece" /> prefab.<br />
        ///     The piece prefabs are added to the current <see cref="ZNetScene" /> on initialization.<br />
        ///     The <see cref="Piece" /> is added to the <see cref="PieceTable" /> defined in <see cref="PieceDef" />
        ///     automatically.<br />
        ///     When ExtensionStation is defined in <see cref="PieceDef" />, the <see cref="Piece" /> is added as a
        ///     <see cref="StationExtension" /> to that station.
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="pieceDef"></param>
        public void RegisterPiecePrefab(GameObject prefab, PieceDef pieceDef)
        {
            if (!RegisteredPrefabs.ContainsKey(prefab.name))
            {
                RegisteredPrefabs.Add(prefab.name, prefab);
                RegisteredPieces.Add(prefab, pieceDef);
            }
        }
/*
        /// <summary>
        ///     Initialize dictionaries of <see cref="CraftingStation"/>s and <see cref="PieceTable"/>s.<br />
        ///     Has the highest priority (0), so other hooks can use the dictionaries.
        /// </summary>
        /// <param name="instance"></param>
        [PatchEvent(typeof(ObjectDB), nameof(ObjectDB.Awake), PatchEventType.Postfix, 0)]
        public static void InitBeforeObjectDB(ObjectDB instance)
        {
            if (instance == null || instance.m_items.Count() == 0)
            {
                return;
            }

            Instance.InitPieceTables(instance);
            Instance.InitCraftingStations(instance);
        }
*/
        private void InitPieceTables(ObjectDB instance)
        {
            // Collect all current PieceTables from the items in ObjectDB
            foreach (var itemPrefab in instance.m_items)
            {
                var item = itemPrefab.GetComponent<ItemDrop>().m_itemData;
                if (item.m_shared.m_buildPieces != null && !PieceTables.ContainsKey(item.m_shared.m_buildPieces.name))
                {
                    PieceTables.Add(item.m_shared.m_buildPieces.name, item.m_shared.m_buildPieces);
                }
            }

            // Collect all PieceTables from our RegisteredPrefabs
            foreach (var itemPrefab in RegisteredPrefabs.Values)
            {
                var item = itemPrefab.GetComponent<ItemDrop>().m_itemData;
                if (item.m_shared.m_buildPieces != null && !PieceTables.ContainsKey(item.m_shared.m_buildPieces.name))
                {
                    PieceTables.Add(item.m_shared.m_buildPieces.name, item.m_shared.m_buildPieces);
                }
            }
        }

        private void InitCraftingStations(ObjectDB instance)
        {
            // Collect all possible CraftingStations from PieceTables
            foreach (var station in PieceTables.Where(x => x.Value.GetComponent<CraftingStation>() != null)
                    .Select(x => x.Value.GetComponent<CraftingStation>()))
            {
                if (!CraftingStations.ContainsKey(station.name))
                {
                    CraftingStations.Add(station.name, station);
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
}