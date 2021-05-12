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
using System;
using Logger = Jotunn.Logger;

namespace Veilheim.AssetManagers
{
    internal class GUIManager : Manager, IPointerClickHandler
    {
        internal static GUIManager Instance { get; private set; }

        internal static GameObject GUIContainer;

        internal Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();

        internal Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        internal static GameObject PixelFix { get; private set; }

        internal Texture2D TextureAtlas { get; private set; }

        internal Texture2D TextureAtlas2 { get; private set; }

        internal Font AveriaSerif { get; private set; }

        internal Font AveriaSerifBold { get; private set; }

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

        internal void AddPrefab(string name, GameObject prefab)
        {
            if (Prefabs.ContainsKey(name))
            {
                Logger.LogWarning($"GUIPrefab {name} already exists");
                return;
            }

            prefab.name = name;
            prefab.transform.SetParent(GUIContainer.transform, false);
            prefab.SetActive(false);
            Prefabs.Add(name, prefab);
        }

        /// <summary>
        /// Returns an existing prefab with given name, or null if none exist.
        /// </summary>
        /// <param name="name">Name of the prefab to search for</param>
        /// <returns></returns>
        internal GameObject GetPrefab(string name)
        {
            if (Prefabs.ContainsKey(name))
            {
                return Prefabs[name];
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
                    TextureAtlas = textures.LastOrDefault(x => x.name.StartsWith("sactx-2048x2048-Uncompressed-UIAtlas-"));
                    TextureAtlas2 = textures.FirstOrDefault(x => x.name.StartsWith("sactx-2048x2048-Uncompressed-UIAtlas-"));
                    if (TextureAtlas == null || TextureAtlas2 == null)
                    {
                        throw new Exception("Texture atlas not found");
                    }

                    // Sprites
                    string[] spriteNames = new string[]
                    {
                        "checkbox", "checkbox_marker", "woodpanel_trophys"
                    };
                    var sprites = Resources.FindObjectsOfTypeAll<Sprite>();
                    foreach (var spriteName in spriteNames)
                    {
                        Sprites.Add(spriteName, sprites.FirstOrDefault(x => x.name == spriteName));
                    }
                    if (Sprites.Count(x => x.Value == null) > 0)
                    {
                        throw new Exception("Sprites not found");
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
                    AddPrefab("BaseButton", button);
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
            var baseButton = GetPrefab("BaseButton");
            GameObject newButton = Instantiate(baseButton, parent);
            newButton.GetComponentInChildren<Text>().text = text;
            ((RectTransform)newButton.transform).anchorMin = anchorMin;
            ((RectTransform)newButton.transform).anchorMax = anchorMax;
            ((RectTransform)newButton.transform).anchoredPosition = position;
            return newButton;
        }

        internal Sprite CreateSpriteFromAtlas(Rect rect, Vector2 pivot, float pixelsPerUnit = 50f, uint extrude = 0, SpriteMeshType meshType = SpriteMeshType.FullRect, Vector4 border = new Vector4())
        {
            return Sprite.Create(TextureAtlas, rect, pivot, pixelsPerUnit, extrude, meshType, border);
        }

        internal Sprite CreateSpriteFromAtlas2(Rect rect, Vector2 pivot, float pixelsPerUnit = 50f, uint extrude = 0, SpriteMeshType meshType = SpriteMeshType.FullRect, Vector4 border = new Vector4())
        {
            return Sprite.Create(TextureAtlas2, rect, pivot, pixelsPerUnit, extrude, meshType, border);
        }

        internal Sprite GetSprite(string spriteName)
        {
            if (Sprites.ContainsKey(spriteName))
            {
                return Sprites[spriteName];
            }

            return null;
        }

        public void ApplyInputFieldStyle(InputField field)
        {
            GameObject go = field.gameObject;

            go.GetComponent<Image>().sprite = CreateSpriteFromAtlas(new Rect(0, 2048 - 156, 139, 36), new Vector2(0.5f, 0.5f), 50f, 0, SpriteMeshType.FullRect, new Vector4(5, 5, 5, 5));
            go.transform.Find("Placeholder").GetComponent<Text>().font = AveriaSerifBold;
            go.transform.Find("Text").GetComponent<Text>().font = AveriaSerifBold;
            go.transform.Find("Text").GetComponent<Text>().color = new Color(1, 1, 1, 1);
        }

        public void ApplyToogleStyle(Toggle toggle)
        {

            ColorBlock tinter = new ColorBlock()
            {
                colorMultiplier = 1f,
                disabledColor = new Color(0.784f, 0.784f, 0.784f, 0.502f),
                fadeDuration = 0.1f,
                highlightedColor = new Color(1f, 1f, 1f, 1f),
                normalColor = new Color(0.61f, 0.61f, 0.61f, 1f),
                pressedColor = new Color(0.784f, 0.784f, 0.784f, 1f),
                selectedColor = new Color(1f, 1f, 1f, 1f)
            };
            toggle.toggleTransition = Toggle.ToggleTransition.Fade;
            toggle.colors = tinter;

            toggle.gameObject.transform.Find("Background").GetComponent<Image>().sprite = GetSprite("checkbox");

            toggle.gameObject.transform.Find("Background/Checkmark").GetComponent<Image>().color = new Color(1f, 0.678f, 0.103f, 1f);

            toggle.gameObject.transform.Find("Background/Checkmark").GetComponent<Image>().sprite = GetSprite("checkbox_marker");
            toggle.gameObject.transform.Find("Background/Checkmark").GetComponent<Image>().maskable = true;
        }
    }
}
