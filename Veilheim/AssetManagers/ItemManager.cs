// Veilheim
// a Valheim mod
// 
// File:    ItemManager.cs
// Project: Veilheim

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Veilheim.AssetEntities;
using Veilheim.PatchEvents;

namespace Veilheim.AssetManagers
{
    internal class ItemManager : AssetManager, IPatchEventConsumer
    {
        internal static ItemManager Instance { get; private set; }

        internal List<GameObject> Items = new List<GameObject>();
        internal List<RecipeDef> Recipes = new List<RecipeDef>();
        
        private void Awake()
        {
            if (Instance != null)
            {
                Logger.LogError($"Two instances of singleton {GetType()}");
                return;
            }

            Instance = this;
        }

        internal override void Init()
        {
            Logger.LogInfo("Initialized ItemManager");
        }

        internal void AddItem(string itemName)
        {
            AddItem(itemName, null);
        }

        internal void AddItem(string itemName, RecipeDef recipeDef)
        {
            var item = PrefabManager.Instance.GetPrefab(itemName);
            if (item == null)
            {
                Logger.LogError($"Prefab for item {itemName} not found");
                return;
            }

            if (item.layer == 0)
            {
                item.layer = LayerMask.NameToLayer("item");
            }
            Items.Add(item);

            if (recipeDef != null)
            {
                recipeDef.Item = itemName;
                AddRecipe(recipeDef);
            }
        }

        /// <summary>
        /// Registers a new recipe as <see cref="RecipeDef"/>.
        /// </summary>
        /// <param name="recipeDef">Recipe details</param>
        internal void AddRecipe(RecipeDef recipeDef)
        {
            Recipes.Add(recipeDef);
        }

        /// <summary>
        ///     Initialize and register all loaded items to the <see cref="ObjectDB" /> in <see cref="FejdStartup" /> (no recipes
        ///     and pieces needed).<br />
        ///     Has a low priority (1000), so other hooks can register their prefabs before they get added to the game.
        /// </summary>
        [PatchEvent(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB), PatchEventType.Postfix, 1000)]
        public static void AddToObjectDBFejd(ObjectDB instance)
        {
            if (SceneManager.GetActiveScene().name != "start")
            {
                return;
            }

            if (Instance.Items.Count > 0)
            {
                Logger.LogMessage($"Registering custom items in {ObjectDB.instance}");

                Instance.RegisterItems();
            }
        }

        /// <summary>
        ///     Initialize and register all loaded items and pieces to the current instance of the <see cref="ObjectDB" />.<br />
        ///     Has a low priority (1000), so other hooks can register their prefabs before they get added to the game.
        /// </summary>
        [PatchEvent(typeof(ObjectDB), nameof(ObjectDB.Awake), PatchEventType.Postfix, 1000)]
        public static void AddToObjectDB(ObjectDB instance)
        {
            if (SceneManager.GetActiveScene().name != "main")
            {
                return;
            }

            if (Instance.Items.Count > 0)
            {
                Logger.LogMessage($"Registering custom items in {ObjectDB.instance}");

                Instance.RegisterItems();
            }

            if (Instance.Recipes.Count > 0)
            {
                Logger.LogMessage($"Registering custom recpies in {ObjectDB.instance}");

                Instance.RegisterRecipes();
            }
        }

        /// <summary>
        ///     Update Recipes for the local Player if custom prefabs were added.<br />
        ///     Has a low priority (1000), so other hooks can register their prefabs before they get added to the player.
        /// </summary>
        /// <param name="instance"></param>
        [PatchEvent(typeof(Player), nameof(Player.OnSpawned), PatchEventType.Postfix, 1000)]
        public static void UpdateKnownRecipes(Player instance)
        {
            if (Player.m_localPlayer == null)
            {
                return;
            }

            if (Instance.Recipes.Count > 0)
            {
                Logger.LogMessage($"Updating known recipes and pieces for Player {Player.m_localPlayer.GetPlayerName()}");

                Player.m_localPlayer.UpdateKnownRecipesList();
                Player.m_localPlayer.UpdateAvailablePiecesList();
            }
        }

        private void RegisterItems()
        {
            // Go through all registered Items and try to obtain references
            // to the actual objects defined as strings in RecipeDef
            foreach (var prefab in Items)
            {
                Logger.LogInfo($"GameObject: {prefab.name}");

                // Add the item prefab to the ObjectDB if not already in there
                var itemDrop = prefab.GetComponent<ItemDrop>();
                if (itemDrop == null)
                {
                    Logger.LogError($"GameObject {prefab.name} has no ItemDrop attached");
                    continue;
                }

                if (ObjectDB.instance.m_itemByHash.ContainsKey(StringExtensionMethods.GetStableHashCode(prefab.name)))
                {
                    Logger.LogWarning("Item already added to ObjectDB");
                    continue;
                }

                itemDrop.m_itemData.m_dropPrefab = prefab;
                ObjectDB.instance.m_items.Add(prefab);

                Logger.LogInfo($"Registered item {prefab.name}");
            }

            // If we registered items, update their hashes
            if (ObjectDB.instance.m_items.Count > ObjectDB.instance.m_itemByHash.Count)
            {
                Logger.LogInfo("Updating item hashes");
                ObjectDB.instance.UpdateItemHashes();
            }
        }

        private void RegisterRecipes()
        {
            foreach (var recipeDef in Recipes)
            {
                // Create recipe 
                var recipe = recipeDef.GetRecipe();

                // Add the Recipe to the ObjectDB, remove one with the same name first
                var removed = ObjectDB.instance.m_recipes.RemoveAll(x => x.name == recipe.name);
                if (removed > 0)
                {
                    Logger.LogInfo($"Removed recipes ({recipe.name}): {removed}");
                }

                ObjectDB.instance.m_recipes.Add(recipe);
                Logger.LogInfo($"Added recipe: {recipe.name}");
            }
        }
    }
}
