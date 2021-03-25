// Veilheim
// a Valheim mod
// 
// File:    ConfigurationGUI.cs
// Project: Veilheim

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Steamworks;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;
using Veilheim.AssetManagers;
using Veilheim.AssetUtils;
using Object = UnityEngine.Object;

namespace Veilheim.Configurations.GUI
{
    public class ConfigurationGUI
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

        public static void ToggleGUI()
        {
            GUIRoot.SetActive(!GUIRoot.activeSelf);
        }

        public static void CreateConfigurationGUIRoot()
        {
            Assembly asm = Assembly.GetAssembly(typeof(UnityEngine.UI.Text));

            if (GUIRoot != null)
            {
                return;
            }

            sections.Clear();
            entries.Clear();

            //GUIRoot = Object.Instantiate(PrefabManager.Instance.GetPrefab("ConfigurationGUIRoot"), InventoryGui.instance.m_playerGrid.transform.parent.parent.parent.parent);
            GUIRoot = Object.Instantiate(PrefabManager.Instance.GetPrefab("ConfigurationGUIRoot"));
            GUIRoot.transform.SetParent(VeilheimPlugin.RootObject.transform);

            GUIRoot.SetActive(false);
            ContentGrid = GUIRoot.GetComponentInChildren<VerticalLayoutGroup>();


            foreach (var property in Configuration.Current.GetSections().Where(x => !typeof(ISyncableSection).IsAssignableFrom(x.PropertyType)))
            {
                BaseConfig configSection = property.GetValue(Configuration.Current, null) as BaseConfig;
                bool sectionEnabled = configSection.IsEnabled;
                GameObject section = CreateSection(property.Name, sectionEnabled, ContentGrid.transform);

                foreach (var sectionProperty in BaseConfig.GetProps(property.PropertyType).Where(x => x.Name != nameof(BaseConfig.IsEnabled)))
                {
                    GameObject entry = null;
                    if (sectionProperty.PropertyType == typeof(bool))
                    {
                        entry = AddEntry(sectionProperty.Name, configSection.GetValue<bool>(sectionProperty.Name), section.transform);
                    }
                    else if (sectionProperty.PropertyType == typeof(int))
                    {
                        entry = AddEntry(sectionProperty.Name, configSection.GetValue<int>(sectionProperty.Name), section.transform);
                    }
                    else if (sectionProperty.PropertyType == typeof(float))
                    {
                        entry = AddEntry(sectionProperty.Name, configSection.GetValue<float>(sectionProperty.Name), section.transform);
                    }

                    entries.Add(entry);
                }
            }


            foreach (var property in Configuration.Current.GetSections().Where(x => typeof(ISyncableSection).IsAssignableFrom(x.PropertyType)))
            {
                BaseConfig configSection = property.GetValue(Configuration.Current, null) as BaseConfig;
                bool sectionEnabled = configSection.IsEnabled;
                GameObject section = CreateSection(property.Name, sectionEnabled, ContentGrid.transform);

                foreach (var sectionProperty in BaseConfig.GetProps(property.PropertyType).Where(x => x.Name != nameof(BaseConfig.IsEnabled)))
                {
                    GameObject entry = null;
                    if (sectionProperty.PropertyType == typeof(bool))
                    {
                        entry = AddEntry(sectionProperty.Name, configSection.GetValue<bool>(sectionProperty.Name), section.transform);
                    }
                    else if (sectionProperty.PropertyType == typeof(int))
                    {
                        entry = AddEntry(sectionProperty.Name, configSection.GetValue<int>(sectionProperty.Name), section.transform);
                    }
                    else if (sectionProperty.PropertyType == typeof(float))
                    {
                        entry = AddEntry(sectionProperty.Name, configSection.GetValue<float>(sectionProperty.Name), section.transform);
                    }

                    entries.Add(entry);
                }
            }

        }

        private static GameObject CreateSection(string sectionName, bool isEnabled, Transform parentTransform)
        {
            GameObject newSection = Object.Instantiate(PrefabManager.Instance.GetPrefab("ConfigurationSection"), parentTransform);
            sections.Add(newSection);
            newSection.GetComponent<Text>().text = sectionName;
            newSection.GetComponentInChildren<Toggle>().isOn = isEnabled;

            return newSection.GetComponentInChildren<VerticalLayoutGroup>().gameObject;
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
            newEntry.GetComponent<Text>().text = entryName + ":";
            return newEntry;
        }



        public static void RPC_IsAdmin(long sender, bool isAdmin)
        {
            if (ZNet.instance.IsClientInstance())
            {
                Configuration.PlayerIsAdmin = isAdmin;
            }
            else
            {
                var peer = ZNet.instance.m_peers.FirstOrDefault(x => x.m_uid == sender);
                if (peer != null)
                {
                    bool result = ZNet.instance.m_adminList.Contains(peer.m_socket.GetHostName());
                    ZRoutedRpc.instance.InvokeRoutedRPC(sender, nameof(RPC_IsAdmin), result);
                }

            }
        }

    }
}