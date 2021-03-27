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
using UnityEngine.Events;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;
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
            if (GameCamera.instance)
            {
                GameCamera.instance.m_mouseCapture = true;
                GameCamera.instance.UpdateMouseCapture();
            }
        }

        public static bool ToggleGUI()
        {
            if (GUIManager.PixelFix == null)
            {
                return false;
            }

            if (GUIRoot == null)
            {
                CreateConfigurationGUIRoot();
            }
            
            bool setActive = !GUIRoot.activeSelf;
            GUIRoot.SetActive(setActive);

            if (setActive)
            {
                UpdateValuesFromConfiguration();
                GameCamera.instance.m_mouseCapture = false;
                GameCamera.instance.UpdateMouseCapture();
            }
            else
            {
                GameCamera.instance.m_mouseCapture = true;
                GameCamera.instance.UpdateMouseCapture();
            }
            
            return setActive;
        }

        public static void EnableEntries()
        {
            foreach (var entry in entries)
            {
                entry.SetActive(true);
            }
        }

        public static void OnOKClick()
        {
            Logger.LogDebug("Clicked OK");

            ApplyValuesToConfiguration();

            DisableGUIRoot();
        }

        public static void CreateConfigurationGUIRoot()
        {
            //GUIRoot = Object.Instantiate(GUIManager.Instance.GetGUIPrefab("ConfigurationGUIRoot"), InventoryGui.instance.m_playerGrid.transform.parent.parent.parent.parent);
            GUIRoot = Object.Instantiate(GUIManager.Instance.GetGUIPrefab("ConfigurationGUIRoot"));
            //GUIRoot.transform.SetParent(GUIManager.GUIContainer.transform, false);
            GUIRoot.transform.SetParent(GUIManager.PixelFix.transform, false);

            GUIRoot.GetComponent<Image>().sprite = GUIManager.Instance.CreateSpriteFromAtlas(new Rect(0, 2048 - 1018, 443, 1018 - 686), new Vector2(0f, 0f));

            var text = GUIRoot.transform.Find("Header").GetComponent<Text>();
            text.font = GUIManager.Instance.AveriaSans;
            //text.color = TextInput.instance.m_topic.color;
            //text.fontSize = TextInput.instance.m_topic.fontSize + 5;
            text.fontSize = text.fontSize + 5;

            var cancelButton = GUIManager.Instance.CreateButton("Cancel", GUIRoot.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-280f, -40f));
            cancelButton.GetComponentInChildren<Button>().onClick.AddListener(new UnityAction(DisableGUIRoot));
            cancelButton.SetActive(true);

            var okButton = GUIManager.Instance.CreateButton("OK", GUIRoot.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-80f, -40f));
            okButton.GetComponentInChildren<Button>().onClick.AddListener(new UnityAction(OnOKClick));
            okButton.SetActive(true);

            ContentGrid = GUIRoot.GetComponentInChildren<VerticalLayoutGroup>();
            
            InitValuesFromConfiguration();
            EnableEntries();
        }

        public static void InitValuesFromConfiguration()
        {
            foreach (var sectionProperty in Configuration.Current.GetSections().Where(x => !typeof(ISyncableSection).IsAssignableFrom(x.PropertyType)))
            {
                BaseConfig configSection = sectionProperty.GetValue(Configuration.Current, null) as BaseConfig;
                bool sectionEnabled = configSection.IsEnabled;
                GameObject section = CreateSection(sectionProperty.Name, sectionEnabled, ContentGrid.transform);
                ((RectTransform)section.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                    BaseConfig.GetProps(sectionProperty.PropertyType).Count(x => x.Name != nameof(BaseConfig.IsEnabled)) * 30f + 40f + 20f);
                ((RectTransform)section.transform.Find("Panel")).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, BaseConfig.GetProps(sectionProperty.PropertyType).Count(x => x.Name != nameof(BaseConfig.IsEnabled)) * 30f + 15f);
                ((RectTransform)section.transform.Find("Panel")).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 465f);
                section.GetComponent<Text>().fontStyle = FontStyle.Normal;
                //section.GetComponent<Text>().font = TextInput.instance.m_topic.font;
                section.GetComponent<Text>().font = GUIManager.Instance.AveriaSans;
                section.GetComponent<Text>().fontSize += 3;

                ((RectTransform)section.transform.Find("Panel")).gameObject.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 0.5f);

                foreach (var entryProperty in BaseConfig.GetProps(sectionProperty.PropertyType).Where(x => x.Name != nameof(BaseConfig.IsEnabled)))
                {
                    GameObject entry = null;
                    if (entryProperty.PropertyType == typeof(bool))
                    {
                        entry = AddEntry(entryProperty.Name, configSection.GetValue<bool>(entryProperty.Name), section.transform.Find("Panel").transform);
                    }
                    else if (entryProperty.PropertyType == typeof(int))
                    {
                        entry = AddEntry(entryProperty.Name, configSection.GetValue<int>(entryProperty.Name), section.transform.Find("Panel").transform);
                    }
                    else if (entryProperty.PropertyType == typeof(float))
                    {
                        entry = AddEntry(entryProperty.Name, configSection.GetValue<float>(entryProperty.Name), section.transform.Find("Panel").transform);
                    }

                    entry.name = sectionProperty.Name + "." + entryProperty.Name;
                    entry.SetActive(true);
                    entries.Add(entry);
                }
            }

            if (Configuration.PlayerIsAdmin)
            {
                foreach (var sectionProperty in Configuration.Current.GetSections().Where(x => typeof(ISyncableSection).IsAssignableFrom(x.PropertyType)))
                {
                    BaseConfig configSection = sectionProperty.GetValue(Configuration.Current, null) as BaseConfig;
                    bool sectionEnabled = configSection.IsEnabled;
                    GameObject section = CreateSection(sectionProperty.Name, sectionEnabled, ContentGrid.transform);
                    ((RectTransform)section.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                        BaseConfig.GetProps(sectionProperty.PropertyType).Count(x => x.Name != nameof(BaseConfig.IsEnabled)) * 30f + 40f + 20f);
                    ((RectTransform)section.transform.Find("Panel")).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, BaseConfig.GetProps(sectionProperty.PropertyType).Count(x => x.Name != nameof(BaseConfig.IsEnabled)) * 30f + 15f);
                    ((RectTransform)section.transform.Find("Panel")).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 465f);

                    ((RectTransform)section.transform.Find("Panel")).gameObject.GetComponent<Image>().color = new Color(0.5f, 61f / 255f, 0f, 0.5f);

                    foreach (var entryProperty in BaseConfig.GetProps(sectionProperty.PropertyType).Where(x => x.Name != nameof(BaseConfig.IsEnabled)))
                    {
                        GameObject entry = null;
                        if (entryProperty.PropertyType == typeof(bool))
                        {
                            entry = AddEntry(entryProperty.Name, configSection.GetValue<bool>(entryProperty.Name), section.transform.Find("Panel").transform);
                        }
                        else if (entryProperty.PropertyType == typeof(int))
                        {
                            entry = AddEntry(entryProperty.Name, configSection.GetValue<int>(entryProperty.Name), section.transform.Find("Panel").transform);
                        }
                        else if (entryProperty.PropertyType == typeof(float))
                        {
                            entry = AddEntry(entryProperty.Name, configSection.GetValue<float>(entryProperty.Name), section.transform.Find("Panel").transform);
                        }

                        entry.name = sectionProperty.Name + "." + entryProperty.Name;
                        entry.SetActive(true);
                        entries.Add(entry);
                    }
                }
            }
        }

        public static void UpdateValuesFromConfiguration()
        {
            foreach (var sectionProperty in Configuration.Current.GetSections())
            {
                if (Configuration.PlayerIsAdmin && typeof(ISyncableSection).IsAssignableFrom(sectionProperty.PropertyType))
                {
                    Logger.LogDebug("Getting values for section " + sectionProperty.Name);
                    GameObject section = sections.First(x => x.name == "section." + sectionProperty.Name);
                    section.transform.Find("Toggle").gameObject.GetComponent<Toggle>().isOn =
                        Configuration.GetValue<bool>(sectionProperty.Name + "." + nameof(BaseConfig.IsEnabled));

                    foreach (var entryProperty in BaseConfig.GetProps(sectionProperty.PropertyType).Where(x => x.Name != nameof(BaseConfig.IsEnabled)))
                    {
                        string path = sectionProperty.Name + "." + entryProperty.Name;
                        if (Configuration.GetValueType(path) == typeof(bool))
                        {
                            entries.First(x => x.name == path).GetComponentInChildren<Toggle>().isOn = Configuration.GetValue<bool>(path);
                        }
                        else if (Configuration.GetValueType(path) == typeof(int))
                        {
                            entries.First(x => x.name == path).GetComponentInChildren<InputField>().text = Configuration.GetValue<int>(path).ToString();
                        }
                        else if (Configuration.GetValueType(path) == typeof(float))
                        {
                            entries.First(x => x.name == path).GetComponentInChildren<InputField>().text = Configuration.GetValue<float>(path).ToString("F");
                        }
                    }
                }
            }

        }

        private static GameObject CreateSection(string sectionName, bool isEnabled, Transform parentTransform)
        {
            GameObject newSection = Object.Instantiate(GUIManager.Instance.GetGUIPrefab("ConfigurationSection"), parentTransform);
            sections.Add(newSection);
            var text = newSection.GetComponent<Text>();
            text.text = sectionName;
            text.fontStyle = FontStyle.Normal;
            //text.font = TextInput.instance.m_topic.font;
            text.font = GUIManager.Instance.AveriaSans;
            text.fontSize += 3;
            //text.color = TextInput.instance.m_topic.color;

            newSection.GetComponentInChildren<Toggle>().isOn = isEnabled;
            newSection.name = "section." + sectionName;

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
            GameObject newEntry = Object.Instantiate(GUIManager.Instance.GetGUIPrefab("ConfigurationEntry"), parentTransform);
            newEntry.name = "configentry." + entryName;
            newEntry.transform.Find("ConfigName").GetComponent<Text>().text = entryName + ":";
            //newEntry.transform.Find("ConfigName").GetComponent<Text>().font = TextInput.instance.m_topic.font;
            newEntry.transform.Find("ConfigName").GetComponent<Text>().font = GUIManager.Instance.AveriaSans;
            //newEntry.transform.Find("InputText").Find("Text").GetComponent<Text>().font = TextInput.instance.m_topic.font;
            newEntry.transform.Find("InputText").Find("Text").GetComponent<Text>().font = GUIManager.Instance.AveriaSans;
            return newEntry;
        }

        private static void ApplyValuesToConfiguration()
        {
            foreach (var sectionProperty in Configuration.Current.GetSections())
            {
                GameObject section = sections.First(x => x.name == "section." + sectionProperty.Name);
                bool sectionEnabled = section.transform.Find("Toggle").gameObject.GetComponent<Toggle>().isOn;
                Configuration.SetValue(sectionProperty.Name + "." + nameof(BaseConfig.IsEnabled), sectionEnabled);

                foreach (var entryProperty in BaseConfig.GetProps(sectionProperty.PropertyType).Where(x => x.Name != nameof(BaseConfig.IsEnabled)))
                {
                    string path = sectionProperty.Name + "." + entryProperty.Name;
                    if (Configuration.GetValueType(path) == typeof(bool))
                    {
                        bool value = entries.First(x => x.name == path).GetComponentInChildren<Toggle>().isOn;
                        Configuration.SetValue(path, value);
                    }
                    else if (Configuration.GetValueType(path) == typeof(int))
                    {
                        int value = 0;
                        string valueString = entries.First(x => x.name == path).GetComponentInChildren<InputField>().text;

                        if (int.TryParse(valueString, out value))
                        {
                            Configuration.SetValue(path, value);
                        }
                    }
                    else if (Configuration.GetValueType(path) == typeof(float))
                    {
                        float value = 0;
                        string valueString = entries.First(x => x.name == path).GetComponentInChildren<InputField>().text;

                        if (float.TryParse(valueString, out value))
                        {
                            Configuration.SetValue(path, value);
                        }
                    }
                }
            }
        }

        [PatchEvent(typeof(Menu), nameof(Menu.IsVisible), PatchEventType.Postfix)]
        public static void GUIVisible2(ref bool result)
        {
            if (GUIRoot != null)
            {
                if (GUIRoot.activeSelf)
                {
                    result = true;
                }
            }
        }
    }
}