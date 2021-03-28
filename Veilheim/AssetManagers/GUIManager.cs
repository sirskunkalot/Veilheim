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
using System;
using Steamworks;

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
                try
                {
                    // Texture Atlas aka Sprite Sheet
                    var textures = Resources.FindObjectsOfTypeAll<Texture2D>();
                    foreach (var tex in textures)
                    {
                        // sactx-2048x2048-Uncompressed-UIAtlas-a5f4e704 is in there two times.
                        // We need the last one, so yeah, loop everything, why not, right?!
                        if (tex.name.StartsWith("sactx-2048x2048-Uncompressed-UIAtlas-a5f4e704"))
                        {
                            TextureAtlas = tex;
                        }
                    }
                    if (TextureAtlas == null)
                    {
                        throw new Exception("Texture atlas not found");
                    }

                    // Fonts
                    var fonts = Resources.FindObjectsOfTypeAll<Font>();
                    AveriaSerif = fonts.FirstOrDefault(x => x.name == "AveriaSerifLibre-Regular");
                    AveriaSerifBold = fonts.FirstOrDefault(x => x.name == "AveriaSerifLibre-Bold");
                    if (AveriaSerifBold == null || AveriaSerif == null)
                    {
                        throw new Exception("Fonts not found");
                    }

                    // GUI components (ouch, my memory hurts... :))
                    var objects = Resources.FindObjectsOfTypeAll(typeof(GameObject));
                    GameObject ingameGui = null;
                    foreach (var obj in objects)
                    {
                        if (obj.name.Equals("IngameGui"))
                        {
                            ingameGui = (GameObject)obj;
                            break;
                        }
                    }
                    if (ingameGui == null)
                    {
                        throw new Exception("GameObjects not found");
                    }

                    // Base prefab for a valheim style button
                    var button = Instantiate(ingameGui.transform.Find("TextInput/panel/OK").gameObject);
                    AddGUIPrefab("BaseButton", button);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                }
                finally
                {
                    needsLoad = false;
                }
            }

            if (PixelFix == null && SceneManager.GetActiveScene().name == "main" && SceneManager.GetActiveScene().isLoaded)
            {
                /*var gamemain = GameObject.Find("_GameMain");
                PixelFix = gamemain.transform.Find("GUI/PixelFix").gameObject;*/
                PixelFix = GameObject.Find("_GameMain/GUI/PixelFix");

                if (PixelFix == null)
                {
                    Logger.LogError("PixelFix not found");
                    needsLoad = false;
                    return;
                }
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

        public void ApplyInputFieldStyle(InputField field)
        {
            GameObject go = field.gameObject;

            go.GetComponent<Image>().sprite = Sprite.Create(TextureAtlas, new Rect(0, 2048 - 156, 139, 36), new Vector2(0.5f, 0.5f), 50f, 0,
                SpriteMeshType.FullRect, new Vector4(5, 5, 5, 5));
            go.transform.Find("Placeholder").GetComponent<Text>().font = AveriaSerifBold;
            go.transform.Find("Text").GetComponent<Text>().font = AveriaSerifBold;
            go.transform.Find("Text").GetComponent<Text>().color = new Color(1, 1, 1, 1);
        }
    }
}
