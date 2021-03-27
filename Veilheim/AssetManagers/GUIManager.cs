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

        internal static GameObject Background;

        internal static GameObject Button;

        private bool loaded = false;

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
            prefab.transform.parent = GUIContainer.transform;
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
            if (!loaded && SceneManager.GetActiveScene().name == "start" && SceneManager.GetActiveScene().isLoaded)
            {
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
                    Logger.LogWarning("GUI not found");
                    return;
                }

                //var oldbkg = ingameGui.transform.Find("Menu/MenuList/GameObject").gameObject;

                /*Background = Instantiate(oldbkg);
                Background.name = "Background";
                Background.transform.SetParent(GUIContainer.transform);
                Background.SetActive(false);

                RectTransform tf = Background.transform as RectTransform;
                tf.localPosition = new Vector3(0, -100, 0);
                tf.anchoredPosition = new Vector2(0, 0);
                tf.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 257);
                tf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 320);
                tf.localScale = new Vector3(1f, 1f, 1f);*/

                Button = Instantiate(ingameGui.transform.Find("TextInput/panel/OK").gameObject);
                Button.name = "Button";
                Button.SetActive(false);

                loaded = true;
            }
        }

        internal GameObject CreateButton(string text, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position)
        {
            GameObject newButton = Instantiate(Button, parent);
            newButton.GetComponentInChildren<Text>().text = text;
            ((RectTransform)newButton.transform).anchorMin = anchorMin;
            ((RectTransform)newButton.transform).anchorMax = anchorMax;
            ((RectTransform)newButton.transform).anchoredPosition = position;
            return newButton;
        }
    }
}
