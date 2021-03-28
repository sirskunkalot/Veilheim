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

        internal Texture2D TextureAtlas2;

        internal Font AveriaSerif;

        internal Font AveriaSerifBold;

        private bool needsLoad = true;

        private Sprite _checkbox;

        private Sprite _checkboxMarker;

        internal Sprite Checkbox
        {
            get
            {
                if (_checkbox == null)
                {
                    _checkbox = Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(x => x.name == "checkbox");
                }
                return _checkbox;
            }
        }

        internal Sprite CheckboxMarker
        {
            get
            {
                if (_checkboxMarker == null)
                {
                    _checkboxMarker = Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(x => x.name == "checkbox_marker");
                }
                return _checkboxMarker;
            }
        }

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
                    TextureAtlas = textures.LastOrDefault(x => x.name == "sactx-2048x2048-Uncompressed-UIAtlas-a5f4e704");
                    TextureAtlas2 = textures.FirstOrDefault(x => x.name == "sactx-2048x2048-Uncompressed-UIAtlas-a5f4e704");

                    if (TextureAtlas == null || TextureAtlas2 == null)
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

        internal Sprite CreateSpriteFromAtlas(Rect rect, Vector2 pivot, float pixelsPerUnit = 50f, uint extrude = 0, SpriteMeshType meshType = SpriteMeshType.FullRect, Vector4 border = new Vector4())
        {
            return Sprite.Create(TextureAtlas, rect, pivot, pixelsPerUnit, extrude, meshType, border);
        }

        internal Sprite CreateSpriteFromAtlas2(Rect rect, Vector2 pivot, float pixelsPerUnit = 50f, uint extrude = 0, SpriteMeshType meshType = SpriteMeshType.FullRect, Vector4 border = new Vector4())
        {
            return Sprite.Create(TextureAtlas2, rect, pivot, pixelsPerUnit, extrude, meshType, border);
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

            toggle.gameObject.transform.Find("Background").GetComponent<Image>().sprite = Checkbox;

            toggle.gameObject.transform.Find("Background/Checkmark").GetComponent<Image>().color = new Color(1f, 0.678f, 0.103f, 1f);

            toggle.gameObject.transform.Find("Background/Checkmark").GetComponent<Image>().sprite = CheckboxMarker;
            toggle.gameObject.transform.Find("Background/Checkmark").GetComponent<Image>().maskable = true;
        }
    }
}
