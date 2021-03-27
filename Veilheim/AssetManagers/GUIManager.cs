// Veilheim
// a Valheim mod
// 
// File:    GUIManager.cs
// Project: Veilheim

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Veilheim.PatchEvents;

namespace Veilheim.AssetManagers
{
    internal class GUIManager : Manager, IPatchEventConsumer, IPointerClickHandler
    {
        internal static GUIManager Instance { get; private set; }
        
        internal static GameObject GUIContainer;
        
        internal static GameObject PixelFix;

        internal Dictionary<string, GameObject> GUIPrefabs = new Dictionary<string, GameObject>();
        
        internal Texture2D TextureAtlas;

        internal Font AveriaSerif;

        internal Font AveriaSerifBold;

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
            GUIContainer.AddComponent<GraphicRaycaster>();

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
                    // sactx-2048x2048-Uncompressed-UIAtlas-a5f4e704 is in there two times. we need the last one, so just loop everything...
                    if (tex.name.StartsWith("sactx-2048x2048-Uncompressed-UIAtlas-a5f4e704"))
                    {
                        map = tex;
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

                AveriaSerif = fonts.FirstOrDefault(x => x.name == "AveriaSerifLibre-Regular");
                AveriaSerifBold = fonts.FirstOrDefault(x => x.name == "AveriaSerifLibre-Bold");
                if (AveriaSerifBold == null || AveriaSerif == null)
                {
                    Logger.LogError("Fonts not found");
                    needsLoad = false;
                    return;
                }

                // GUI components (ouch, my memory hurts... :))
                var objects = Resources.FindObjectsOfTypeAll<GameObject>();
                GameObject ingameGui = null;
                //GameObject pixelFix = null;
                foreach (var obj in objects)
                {
                    if (obj.name.Equals("IngameGui"))
                    {
                        ingameGui = obj;
                    }
                    
                    if (ingameGui != null)
                    {
                        break;
                    }

                    /*if (obj.name.Equals("_GameMain"))
                    {
                        pixelFix = obj.transform.Find("GUI/PixelFix").gameObject;
                    }*/

                    /*if (ingameGui != null && pixelFix != null)
                    {
                        break;
                    }*/
                }

                /*if (ingameGui == null || pixelFix == null)
                {
                    Logger.LogError("GameObjects not found");
                    needsLoad = false;
                    return;
                }

                // reference to PixelFix for High DPI displays
                PixelFix = pixelFix;*/
                
                if (ingameGui == null)
                {
                    Logger.LogError("GameObjects not found");
                    needsLoad = false;
                    return;
                }

                // Base prefab for a valheim style button
                var button = Instantiate(ingameGui.transform.Find("TextInput/panel/OK").gameObject);
                AddGUIPrefab("BaseButton", button);

                needsLoad = false;
            }

            if (PixelFix == null && SceneManager.GetActiveScene().name == "main" && SceneManager.GetActiveScene().isLoaded)
            {
                var gamemain = GameObject.Find("_GameMain");
                PixelFix = gamemain.transform.Find("GUI/PixelFix").gameObject;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Logger.LogMessage(eventData.GetObjectString());
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
