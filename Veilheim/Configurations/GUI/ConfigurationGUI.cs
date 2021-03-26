// Veilheim
// a Valheim mod
// 
// File:    ConfigurationGUI.cs
// Project: Veilheim

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Steamworks;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;
using Veilheim.AssetManagers;
using Veilheim.AssetUtils;
using Veilheim.PatchEvents;
using Object = UnityEngine.Object;

namespace Veilheim.Configurations.GUI
{
    public class ConfigurationGUI : IPatchEventConsumer
    {

        private static GameObject GUIRoot;

        private static VerticalLayoutGroup ContentGrid;

        private static List<GameObject> entries = new List<GameObject>();

        private static List<GameObject> sections = new List<GameObject>();

        public static void EnableGUIRoot()
        {
            GUIRoot.SetActive(true);
        }

        public static void DisableGUIRoot()
        {
            GUIRoot.SetActive(false);
        }

        public static bool ToggleGUI()
        {
            bool newState = !GUIRoot.activeSelf;
            GUIRoot.SetActive(newState);
            return newState;
        }

        public static void EnableEntries()
        {
            foreach (var entry in entries)
            {
                entry.SetActive(true);
            }
        }

        public static void CreateConfigurationGUIRoot()
        {

            if (GUIRoot != null)
            {
                return;
            }

            sections.Clear();
            entries.Clear();

            GUIRoot = Object.Instantiate(PrefabManager.Instance.GetPrefab("ConfigurationGUIRoot"), InventoryGui.instance.m_playerGrid.transform.parent.parent.parent.parent);

            GUIRoot.SetActive(false);
            ContentGrid = GUIRoot.GetComponentInChildren<VerticalLayoutGroup>();

            foreach (var property in Configuration.Current.GetSections().Where(x => !typeof(ISyncableSection).IsAssignableFrom(x.PropertyType)))
            {
                BaseConfig configSection = property.GetValue(Configuration.Current, null) as BaseConfig;
                bool sectionEnabled = configSection.IsEnabled;
                GameObject section = CreateSection(property.Name, sectionEnabled, ContentGrid.transform);
                ((RectTransform)section.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                    BaseConfig.GetProps(property.PropertyType).Count(x => x.Name != nameof(BaseConfig.IsEnabled)) * 30f + 40f + 20f);
                ((RectTransform)section.transform.Find("Panel")).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, BaseConfig.GetProps(property.PropertyType).Count(x => x.Name != nameof(BaseConfig.IsEnabled)) * 30f + 15f);
                ((RectTransform)section.transform.Find("Panel")).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 465f);
                section.GetComponent<Text>().fontStyle = FontStyle.Bold;
                foreach (var sectionProperty in BaseConfig.GetProps(property.PropertyType).Where(x => x.Name != nameof(BaseConfig.IsEnabled)))
                {
                    GameObject entry = null;
                    if (sectionProperty.PropertyType == typeof(bool))
                    {
                        entry = AddEntry(sectionProperty.Name, configSection.GetValue<bool>(sectionProperty.Name), section.transform.Find("Panel").transform);
                    }
                    else if (sectionProperty.PropertyType == typeof(int))
                    {
                        entry = AddEntry(sectionProperty.Name, configSection.GetValue<int>(sectionProperty.Name), section.transform.Find("Panel").transform);
                    }
                    else if (sectionProperty.PropertyType == typeof(float))
                    {
                        entry = AddEntry(sectionProperty.Name, configSection.GetValue<float>(sectionProperty.Name), section.transform.Find("Panel").transform);
                    }

                    entry.SetActive(true);
                    entries.Add(entry);
                }
            }

            if (Configuration.PlayerIsAdmin)
            {
                foreach (var property in Configuration.Current.GetSections().Where(x => typeof(ISyncableSection).IsAssignableFrom(x.PropertyType)))
                {
                    BaseConfig configSection = property.GetValue(Configuration.Current, null) as BaseConfig;
                    bool sectionEnabled = configSection.IsEnabled;
                    GameObject section = CreateSection(property.Name, sectionEnabled, ContentGrid.transform);
                    ((RectTransform)section.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                        BaseConfig.GetProps(property.PropertyType).Count(x => x.Name != nameof(BaseConfig.IsEnabled)) * 30f + 40f + 20f);
                    ((RectTransform)section.transform.Find("Panel")).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, BaseConfig.GetProps(property.PropertyType).Count(x => x.Name != nameof(BaseConfig.IsEnabled)) * 30f + 15f);
                    ((RectTransform)section.transform.Find("Panel")).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 465f);

                    ((RectTransform) section.transform.Find("Panel")).gameObject.GetComponent<Image>().color = new Color(0.5f,61f/255f,0f,0.5f);
                    section.GetComponent<Text>().fontStyle = FontStyle.Bold;

                    foreach (var sectionProperty in BaseConfig.GetProps(property.PropertyType).Where(x => x.Name != nameof(BaseConfig.IsEnabled)))
                    {
                        GameObject entry = null;
                        if (sectionProperty.PropertyType == typeof(bool))
                        {
                            entry = AddEntry(sectionProperty.Name, configSection.GetValue<bool>(sectionProperty.Name), section.transform.Find("Panel").transform);
                        }
                        else if (sectionProperty.PropertyType == typeof(int))
                        {
                            entry = AddEntry(sectionProperty.Name, configSection.GetValue<int>(sectionProperty.Name), section.transform.Find("Panel").transform);
                        }
                        else if (sectionProperty.PropertyType == typeof(float))
                        {
                            entry = AddEntry(sectionProperty.Name, configSection.GetValue<float>(sectionProperty.Name), section.transform.Find("Panel").transform);
                        }

                        entry.SetActive(true);
                        entries.Add(entry);
                    }
                }
            }

            VeilheimPlugin.Instance.Invoke(nameof(VeilheimPlugin.EnableConfigGui), 0.001f);
        }

        public static void UpdateValuesFromConfiguration()
        {

        }

        private static GameObject CreateSection(string sectionName, bool isEnabled, Transform parentTransform)
        {
            GameObject newSection = Object.Instantiate(PrefabManager.Instance.GetPrefab("ConfigurationSection"), parentTransform);
            sections.Add(newSection);
            newSection.GetComponent<Text>().text = sectionName;
            newSection.GetComponentInChildren<Toggle>().isOn = isEnabled;

            return newSection;
        }

        private static GameObject AddEntry(string entryName, bool value, Transform parentTransform)
        {
            GameObject newEntry = AddEntry(entryName, parentTransform);

            newEntry.GetComponentInChildren<Toggle>().gameObject.SetActive(true);
            newEntry.GetComponentInChildren<InputField>().gameObject.SetActive(false);
            newEntry.GetComponentInChildren<Toggle>().isOn = value;

            return newEntry;
        }

        private static GameObject AddEntry(string entryName, int value, Transform parentTransform)
        {
            GameObject newEntry = AddEntry(entryName, parentTransform);

            newEntry.GetComponentInChildren<Toggle>().gameObject.SetActive(false);
            newEntry.GetComponentInChildren<InputField>().gameObject.SetActive(true);
            newEntry.GetComponentInChildren<InputField>().text = value.ToString();

            return newEntry;
        }

        private static GameObject AddEntry(string entryName, float value, Transform parentTransform)
        {
            GameObject newEntry = AddEntry(entryName, parentTransform);

            newEntry.GetComponentInChildren<Toggle>().gameObject.SetActive(false);
            newEntry.GetComponentInChildren<InputField>().gameObject.SetActive(true);
            newEntry.GetComponentInChildren<InputField>().text = value.ToString("F");

            return newEntry;
        }

        private static GameObject AddEntry(string entryName, Transform parentTransform)
        {
            GameObject newEntry = Object.Instantiate(PrefabManager.Instance.GetPrefab("ConfigurationEntry"), parentTransform);
            newEntry.name = "configentry." + entryName;
            newEntry.transform.Find("ConfigName").GetComponent<Text>().text = entryName + ":";
            return newEntry;
        }

        public static void RPC_IsAdmin(long sender, bool isAdmin)
        {
            if (ZNet.instance.IsClientInstance())
            {
                Logger.LogDebug("Received player admin status: " + isAdmin);
                Configuration.PlayerIsAdmin = isAdmin;
            }
            else
            {
                Logger.LogDebug("Requesting player admin status for peer #" + sender);
                var peer = ZNet.instance.m_peers.FirstOrDefault(x => x.m_uid == sender);
                if (peer != null)
                {
                    bool result = ZNet.instance.m_adminList.Contains(peer.m_socket.GetHostName());
                    ZRoutedRpc.instance.InvokeRoutedRPC(sender, nameof(RPC_IsAdmin), result);
                }
            }
        }

        [PatchEvent(typeof(Player), nameof(Player.InCutscene), PatchEventType.Postfix)]
        public static void GUIVisible(Player instance, ref bool result)
        {
            if (GUIRoot != null)
            {
                if (GUIRoot.activeSelf)
                {
                    result = true;
                }
            }
        }

        [PatchEvent(typeof(ZNet), nameof(ZNet.RPC_PeerInfo), PatchEventType.Postfix)]
        public static void RequestPlayerAdminStatus(ZNet instance)
        {
            if (ZNet.instance.IsClientInstance())
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), nameof(ConfigurationGUI.RPC_IsAdmin), false);
            }
        }

        [PatchEvent(typeof(Game), nameof(Game.Start), PatchEventType.Prefix)]
        public static void Register_RPC_PlayerAdminStatus(Game instance)
        {
            // Config Sync
            ZRoutedRpc.instance.Register(nameof(ConfigurationGUI.RPC_IsAdmin), new Action<long, bool>(ConfigurationGUI.RPC_IsAdmin));
        }

    }
}