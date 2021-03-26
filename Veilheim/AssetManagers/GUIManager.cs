// Veilheim
// a Valheim mod
// 
// File:    GUIManager.cs
// Project: Veilheim

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Veilheim.AssetEntities;
using Veilheim.PatchEvents;

namespace Veilheim.AssetManagers
{
    internal class GUIManager : AssetManager, IPatchEventConsumer
    {
        internal static GUIManager Instance { get; private set; }
        
        internal static GameObject GUIContainer;

        internal static GameObject Background;

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

        private void OnGUI()
        {
            // Load valheim GUI assets
            if (!loaded && SceneManager.GetActiveScene().name == "start" && SceneManager.GetActiveScene().isLoaded)
            {
                GameObject startGui = GameObject.Find("StartGui");
                
                if (startGui == null)
                {
                    Logger.LogWarning("GUI not found");
                    return;
                }

                var oldbkg = startGui.transform.Find("Menu/MenuList/GameObject").gameObject;

                Background = Instantiate(oldbkg);
                Background.name = "Background";
                Background.transform.SetParent(GUIContainer.transform);
                Background.SetActive(true);

                RectTransform tf = Background.transform as RectTransform;
                tf.localPosition = new Vector3(0, -100, 0);
                tf.anchoredPosition = new Vector2(0, 0);
                tf.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 257);
                tf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 320);
                tf.localScale = new Vector3(1f, 1f, 1f);
                
                loaded = true;
            }
        }
    }
}
