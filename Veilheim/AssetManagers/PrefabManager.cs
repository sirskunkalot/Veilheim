// Veilheim
// a Valheim mod
// 
// File:    PrefabManager.cs
// Project: Veilheim

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Veilheim.PatchEvents;

namespace Veilheim.AssetManagers
{
    internal class PrefabManager : AssetManager, IPatchEventConsumer
    {
        internal static PrefabManager Instance { get; private set; }
        internal static GameObject PrefabContainer;

        internal Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();

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
            PrefabContainer = new GameObject("Prefabs");
            PrefabContainer.transform.parent = VeilheimPlugin.RootObject.transform;
            PrefabContainer.SetActive(false);

            Logger.LogInfo("Initialized PrefabManager");
        }

        internal void AddPrefab(string name, GameObject prefab)
        {
            if (Prefabs.ContainsKey(name))
            {
                Logger.LogError("Prefab already exists: " + name);
                return;
            }

            prefab.name = name;
            prefab.transform.parent = PrefabContainer.transform;
            prefab.SetActive(true);
            Prefabs.Add(name, prefab);
        }

        /// <summary>
        ///     Add all registered prefabs to the namedPrefabs in <see cref="ZNetScene" />.<br />
        ///     Has a low priority (1000), so other hooks can register their prefabs before they get added.
        /// </summary>
        /// <param name="instance"></param>
        [PatchEvent(typeof(ZNetScene), nameof(ZNetScene.Awake), PatchEventType.Postfix, 1000)]
        public static void RegisterToZNetScene(ZNetScene instance)
        {
            if (ZNetScene.instance == null)
            {
                return;
            }

            if (Instance.Prefabs.Count > 0)
            {
                Logger.LogMessage("----Adding custom prefabs to ZNetScene----");

                foreach (var prefab in Instance.Prefabs)
                {
                    var name = prefab.Key;

                    Logger.LogDebug($"GameObject: {name}");

                    if (!ZNetScene.instance.m_namedPrefabs.ContainsKey(name.GetStableHashCode()))
                    {
                        var gameObject = prefab.Value;

                        ZNetScene.instance.m_prefabs.Add(gameObject);
                        ZNetScene.instance.m_namedPrefabs.Add(name.GetStableHashCode(), gameObject);
                        Logger.LogInfo($"Added {name}");
                    }
                }
            }
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

            if (!ZNetScene.instance)
            {
                Debug.LogError("ZNetScene instance null");
                return null;
            }

            foreach (GameObject obj in ZNetScene.instance.m_prefabs)
            {
                if (obj.name == name)
                {
                    return obj;
                }
            }

            return null;
        }
    }
}
