// Veilheim
// a Valheim mod
// 
// File:    GUIManager.cs
// Project: Veilheim

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Veilheim.AssetEntities;
using Veilheim.PatchEvents;

namespace Veilheim.AssetManagers
{
    internal class GUIManager : Manager, IPatchEventConsumer
    {
        internal static GUIManager Instance { get; private set; }
        
        internal static GameObject GUIContainer;

        internal Dictionary<string, GameObject> GUIPrefabs = new Dictionary<string, GameObject>();
        
        internal Texture2D TextureAtlas;

        internal Font AveriaSerif;

        internal Font AveriaSans;

        private bool needsLoad = true;

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
            GUIContainer = new GameObject("GUI");
            GUIContainer.transform.SetParent(VeilheimPlugin.RootObject.transform);
            var canvas = GUIContainer.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            Logger.LogInfo("Initialized GUIManager");
        }

        internal void AddGUIPrefab(string name, GameObject prefab)
        {
            if (GUIPrefabs.ContainsKey(name))
            {
                Logger.LogWarning($"GUIPrefab {name} already exists");
                return;
            }

            prefab.name = name;
            //prefab.transform.parent = GUIContainer.transform;
            prefab.transform.SetParent(GUIContainer.transform, false);
            prefab.SetActive(false);
            GUIPrefabs.Add(name, prefab);
        }

        /// <summary>
        /// Returns an existing prefab with given name, or null if none exist.
        /// </summary>
        /// <param name="name">Name of the prefab to search for</param>
        /// <returns></returns>
        internal GameObject GetGUIPrefab(string name)
        {
            if (GUIPrefabs.ContainsKey(name))
            {
                return GUIPrefabs[name];
            }

            return null;
        }

        private void OnGUI()
        {
            // Load valheim GUI assets
            if (needsLoad && SceneManager.GetActiveScene().name == "start" && SceneManager.GetActiveScene().isLoaded)
            {
                // Texture atlas
                var textures = Resources.FindObjectsOfTypeAll<Texture2D>();
                Texture2D map = null;
                foreach (var tex in textures)
                {
                    if (tex.name.StartsWith("sactx-2048x2048-Uncompressed-UIAtlas"))
                    {
                        map = tex;
                        break;
                    }
                }

                if (map == null)
                {
                    Logger.LogError("Texture atlas not found");
                    needsLoad = false;
                    return;
                }

                TextureAtlas = map;

                // Fonts
                var fonts = Resources.FindObjectsOfTypeAll<Font>();
                Font serif = null;
                Font sans = null;
                foreach (var font in fonts)
                {
                    if (font.name.StartsWith("AveriaSerifLibre-Regular"))
                    {
                        serif = font;
                    }
                    if (font.name.StartsWith("AveriaSansLibre-Regular"))
                    {
                        sans = font;
                    }
                }

                if (serif == null || sans == null)
                {
                    Logger.LogError("Fonts not found");
                    needsLoad = false;
                    return;
                }

                AveriaSerif = serif;
                AveriaSans = sans;

                // GUI components
                var objects = Resources.FindObjectsOfTypeAll<GameObject>();
                GameObject ingameGui = null;
                foreach (var obj in objects)
                {
                    if (obj.name.Equals("IngameGui"))
                    {
                        ingameGui = obj;
                        break;
                    }
                }

                if (ingameGui == null)
                {
                    Logger.LogError("IngameGui not found");
                    needsLoad = false;
                    return;
                }

                // Base prefab for a valheim style button
                var button = Instantiate(ingameGui.transform.Find("TextInput/panel/OK").gameObject);
                AddGUIPrefab("BaseButton", button);

                needsLoad = false;
            }
        }

        internal GameObject CreateButton(string text, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position)
        {
            var baseButton = GetGUIPrefab("BaseButton");
            GameObject newButton = Instantiate(baseButton, parent);
            newButton.GetComponentInChildren<Text>().text = text;
            ((RectTransform)newButton.transform).anchorMin = anchorMin;
            ((RectTransform)newButton.transform).anchorMax = anchorMax;
            ((RectTransform)newButton.transform).anchoredPosition = position;
            return newButton;
        }

        internal Sprite CreateSpriteFromAtlas(Rect rect, Vector2 pivot)
        {
            return Sprite.Create(TextureAtlas, rect, pivot);
        }
    }
}
