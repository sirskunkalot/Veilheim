// Veilheim
// a Valheim mod
// 
// File:    ConfigurationGUI.cs
// Project: Veilheim

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Veilheim.AssetManagers;
using Veilheim.PatchEvents;
using Object = UnityEngine.Object;

namespace Veilheim.Configurations.GUI
{
    public class ConfigurationGUI : IPatchEventConsumer
    {
        private static GameObject GUIRoot;

        private static VerticalLayoutGroup ContentGrid;

        private static readonly List<GameObject> entries = new List<GameObject>();

        private static readonly List<GameObject> sections = new List<GameObject>();

        /// <summary>
        ///     Disable configuration GUI
        /// </summary>
        public static void DisableGUIRoot()
        {
            GUIRoot.SetActive(false);
            if (GameCamera.instance)
            {
                GameCamera.instance.m_mouseCapture = true;
                GameCamera.instance.UpdateMouseCapture();
            }
        }

        /// <summary>
        ///     Toggle GUI visibility
        /// </summary>
        /// <returns>new visibility</returns>
        public static bool ToggleGUI()
        {
            if (GUIManager.PixelFix == null)
            {
                if (GUIRoot != null)
                {
                    Object.Destroy(GUIRoot);
                    GUIRoot = null;
                }

                return false;
            }

            if (GUIRoot == null)
            {
                CreateConfigurationGUIRoot();
            }

            var setActive = !GUIRoot.activeSelf;
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

        /// <summary>
        ///     Save values to configuration on OK
        /// </summary>
        public static void OnOKClick()
        {
            Logger.LogDebug("Clicked OK");

            ApplyValuesToConfiguration();

            DisableGUIRoot();
        }

        /// <summary>
        ///     Create root part of GUI
        /// </summary>
        public static void CreateConfigurationGUIRoot()
        {
            GUIRoot = Object.Instantiate(GUIManager.Instance.GetGUIPrefab("ConfigurationGUIRoot"));
            GUIRoot.transform.SetParent(GUIManager.PixelFix.transform, false);

            GUIRoot.GetComponent<Image>().sprite = GUIManager.Instance.CreateSpriteFromAtlas(new Rect(0, 2048 - 1018, 443, 1018 - 686), new Vector2(0f, 0f));

            var text = GUIRoot.transform.Find("Header").GetComponent<Text>();
            text.font = GUIManager.Instance.AveriaSerifBold;
            text.color = new Color(1f, 0.7176f, 0.363f, 1f);
            text.fontSize = 25;

            var cancelButton = GUIManager.Instance.CreateButton("Cancel", GUIRoot.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-280f, -40f));
            cancelButton.GetComponentInChildren<Button>().onClick.AddListener(DisableGUIRoot);
            cancelButton.SetActive(true);

            var okButton = GUIManager.Instance.CreateButton("OK", GUIRoot.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-80f, -40f));
            okButton.GetComponentInChildren<Button>().onClick.AddListener(OnOKClick);
            okButton.SetActive(true);

            ContentGrid = GUIRoot.GetComponentInChildren<VerticalLayoutGroup>();

            InitValuesFromConfiguration();
        }

        /// <summary>
        ///     Create sections and entries
        /// </summary>
        public static void InitValuesFromConfiguration()
        {
            foreach (var sectionProperty in Configuration.Current.GetSections().Where(x => !typeof(ISyncableSection).IsAssignableFrom(x.PropertyType)))
            {

                var configSection = sectionProperty.GetValue(Configuration.Current, null) as BaseConfig;
                var sectionEnabled = configSection.IsEnabled;
                var section = CreateSection(sectionProperty, sectionEnabled, ContentGrid.transform);
                // ((RectTransform) section.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, BaseConfig.GetProps(sectionProperty.PropertyType).Count(x => x.Name != nameof(BaseConfig.IsEnabled)) * 70f + 40f + 20f);
                // ((RectTransform) section.transform.Find("Panel")).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, BaseConfig.GetProps(sectionProperty.PropertyType).Count(x => x.Name != nameof(BaseConfig.IsEnabled)) * 70f + 15f);
                ((RectTransform)section.transform.Find("Panel")).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 465f);
                section.GetComponent<Text>().fontStyle = FontStyle.Normal;
                section.GetComponent<Text>().font = GUIManager.Instance.AveriaSerifBold;
                section.GetComponent<Text>().fontSize += 3;

                ((RectTransform)section.transform.Find("Panel")).gameObject.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 0.5f);

                float maxHeight = 0f;
                foreach (var entryProperty in BaseConfig.GetProps(sectionProperty.PropertyType).Where(x => x.Name != nameof(BaseConfig.IsEnabled)))
                {
                    GameObject entry = null;
                    if (entryProperty.PropertyType == typeof(bool))
                    {
                        entry = AddEntry(entryProperty, configSection.GetValue<bool>(entryProperty.Name), section.transform.Find("Panel").transform, configSection.GetDefault(configSection.GetType(), entryProperty.Name));
                    }
                    else if (entryProperty.PropertyType == typeof(int))
                    {
                        entry = AddEntry(entryProperty, configSection.GetValue<int>(entryProperty.Name), section.transform.Find("Panel").transform, configSection.GetDefault(configSection.GetType(), entryProperty.Name));
                    }
                    else if (entryProperty.PropertyType == typeof(float))
                    {
                        entry = AddEntry(entryProperty, configSection.GetValue<float>(entryProperty.Name), section.transform.Find("Panel").transform, configSection.GetDefault(configSection.GetType(), entryProperty.Name));
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
                    var configSection = sectionProperty.GetValue(Configuration.Current, null) as BaseConfig;
                    var sectionEnabled = configSection.IsEnabled;
                    var section = CreateSection(sectionProperty, sectionEnabled, ContentGrid.transform);
                    // ((RectTransform) section.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, BaseConfig.GetProps(sectionProperty.PropertyType).Count(x => x.Name != nameof(BaseConfig.IsEnabled)) * 70f + 40f + 20f);
                    // ((RectTransform) section.transform.Find("Panel")).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, BaseConfig.GetProps(sectionProperty.PropertyType).Count(x => x.Name != nameof(BaseConfig.IsEnabled)) * 70f + 15f);
                    ((RectTransform)section.transform.Find("Panel")).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 465f);
                    ((RectTransform)section.transform.Find("Panel")).gameObject.GetComponent<Image>().color = new Color(0.5f, 61f / 255f, 0f, 0.5f);

                    section.GetComponent<Text>().fontSize += 3;

                    float maxHeight = 0f;
                    foreach (var entryProperty in BaseConfig.GetProps(sectionProperty.PropertyType).Where(x => x.Name != nameof(BaseConfig.IsEnabled)))
                    {
                        GameObject entry = null;
                        if (entryProperty.PropertyType == typeof(bool))
                        {
                            entry = AddEntry(entryProperty, configSection.GetValue<bool>(entryProperty.Name), section.transform.Find("Panel").transform, configSection.GetDefault(configSection.GetType(), entryProperty.Name));
                        }
                        else if (entryProperty.PropertyType == typeof(int))
                        {
                            entry = AddEntry(entryProperty, configSection.GetValue<int>(entryProperty.Name), section.transform.Find("Panel").transform, configSection.GetDefault(configSection.GetType(), entryProperty.Name));
                        }
                        else if (entryProperty.PropertyType == typeof(float))
                        {
                            entry = AddEntry(entryProperty, configSection.GetValue<float>(entryProperty.Name), section.transform.Find("Panel").transform, configSection.GetDefault(configSection.GetType(), entryProperty.Name));
                        }

                        entry.name = sectionProperty.Name + "." + entryProperty.Name;
                        entry.SetActive(true);
                        entries.Add(entry);
                    }
                }
            }
            VeilheimPlugin.Instance.Invoke(nameof(VeilheimPlugin.UpdateGUI), 0.1f);
        }

        /// <summary>
        ///     Get values from configuration and set them in the GUI fields
        /// </summary>
        public static void UpdateValuesFromConfiguration()
        {
            foreach (var sectionProperty in Configuration.Current.GetSections())
            {
                if (Configuration.PlayerIsAdmin && typeof(ISyncableSection).IsAssignableFrom(sectionProperty.PropertyType))
                {
                    Logger.LogDebug("Getting values for section " + sectionProperty.Name);
                    var section = sections.First(x => x.name == "section." + sectionProperty.Name);
                    section.transform.Find("Toggle").gameObject.GetComponent<Toggle>().isOn =
                        Configuration.GetValue<bool>(sectionProperty.Name + "." + nameof(BaseConfig.IsEnabled));

                    foreach (var entryProperty in BaseConfig.GetProps(sectionProperty.PropertyType).Where(x => x.Name != nameof(BaseConfig.IsEnabled)))
                    {
                        var path = sectionProperty.Name + "." + entryProperty.Name;
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

        /// <summary>
        ///     Create new section object
        /// </summary>
        /// <param name="sectionName">Name of the section</param>
        /// <param name="isEnabled">is it enabled? (configuration)</param>
        /// <param name="parentTransform">parent</param>
        /// <returns></returns>
        private static GameObject CreateSection(PropertyInfo property, bool isEnabled, Transform parentTransform)
        {
            var newSection = Object.Instantiate(GUIManager.Instance.GetGUIPrefab("ConfigurationSection"), parentTransform);
            sections.Add(newSection);
            var text = newSection.GetComponent<Text>();
            text.text = Configuration.GetSectionDescription(property);
            text.fontStyle = FontStyle.Normal;
            text.font = GUIManager.Instance.AveriaSerifBold;
            text.fontSize += 3;
            text.color = new Color(1f, 0.7176f, 0.363f, 1f);

            newSection.GetComponentInChildren<Toggle>().isOn = isEnabled;
            newSection.name = "section." + property.Name;
            newSection.SetActive(true);
            return newSection;
        }

        /// <summary>
        ///     Add new configuration value entry
        /// </summary>
        /// <param name="entryName">Name</param>
        /// <param name="value">value</param>
        /// <param name="parentTransform">parent</param>
        /// <returns>new entry</returns>
        private static GameObject AddEntry(PropertyInfo entryProperty, bool value, Transform parentTransform, object defaultValue)
        {
            var newEntry = AddEntry(entryProperty, parentTransform, defaultValue);

            newEntry.GetComponentInChildren<Toggle>().gameObject.SetActive(true);
            newEntry.GetComponentInChildren<InputField>().gameObject.SetActive(false);
            newEntry.GetComponentInChildren<Toggle>().isOn = value;

            return newEntry;
        }

        /// <summary>
        ///     Add new configuration value entry
        /// </summary>
        /// <param name="entryName">Name</param>
        /// <param name="value">value</param>
        /// <param name="parentTransform">parent</param>
        /// <returns>new entry</returns>
        private static GameObject AddEntry(PropertyInfo entryProperty, int value, Transform parentTransform, object defaultValue)
        {
            var newEntry = AddEntry(entryProperty, parentTransform, defaultValue);

            newEntry.GetComponentInChildren<Toggle>().gameObject.SetActive(false);
            newEntry.GetComponentInChildren<InputField>().gameObject.SetActive(true);
            newEntry.GetComponentInChildren<InputField>().text = value.ToString();

            return newEntry;
        }

        /// <summary>
        ///     Add new configuration value entry
        /// </summary>
        /// <param name="entryName">Name</param>
        /// <param name="value">value</param>
        /// <param name="parentTransform">parent</param>
        /// <returns>new entry</returns>
        private static GameObject AddEntry(PropertyInfo entryProperty, float value, Transform parentTransform, object defaultValue)
        {
            var newEntry = AddEntry(entryProperty, parentTransform, defaultValue);

            newEntry.GetComponentInChildren<Toggle>().gameObject.SetActive(false);
            newEntry.GetComponentInChildren<InputField>().gameObject.SetActive(true);
            newEntry.GetComponentInChildren<InputField>().text = value.ToString("F");

            return newEntry;
        }

        /// <summary>
        ///     Create a new entry (type independent)
        /// </summary>
        /// <param name="entryName">Name</param>
        /// <param name="parentTransform">parent</param>
        /// <returns>new entry</returns>
        private static GameObject AddEntry(PropertyInfo entryProperty, Transform parentTransform, object defaultValue)
        {
            var newEntry = Object.Instantiate(GUIManager.Instance.GetGUIPrefab("ConfigurationEntry"), parentTransform);
            newEntry.name = "configentry." + entryProperty.Name;

            newEntry.transform.Find("ConfigName").GetComponent<Text>().text = Configuration.GetEntryDescription(entryProperty) + ":" + Environment.NewLine + $"({entryProperty.Name}, default: {defaultValue})";
            newEntry.transform.Find("ConfigName").GetComponent<Text>().font = GUIManager.Instance.AveriaSerifBold;
            newEntry.transform.Find("InputText").Find("Text").GetComponent<Text>().font = GUIManager.Instance.AveriaSerifBold;
            newEntry.SetActive(true);
            return newEntry;
        }

        /// <summary>
        ///     Apply values from the GUI to the configuration, automatically send server side values to the server
        /// </summary>
        private static void ApplyValuesToConfiguration()
        {
            foreach (var sectionProperty in Configuration.Current.GetSections())
            {
                var section = sections.First(x => x.name == "section." + sectionProperty.Name);
                var sectionEnabled = section.transform.Find("Toggle").gameObject.GetComponent<Toggle>().isOn;
                Configuration.SetValue(sectionProperty.Name + "." + nameof(BaseConfig.IsEnabled), sectionEnabled);

                foreach (var entryProperty in BaseConfig.GetProps(sectionProperty.PropertyType).Where(x => x.Name != nameof(BaseConfig.IsEnabled)))
                {
                    var path = sectionProperty.Name + "." + entryProperty.Name;
                    if (Configuration.GetValueType(path) == typeof(bool))
                    {
                        var value = entries.First(x => x.name == path).GetComponentInChildren<Toggle>().isOn;
                        Configuration.SetValue(path, value);
                    }
                    else if (Configuration.GetValueType(path) == typeof(int))
                    {
                        var value = 0;
                        var valueString = entries.First(x => x.name == path).GetComponentInChildren<InputField>().text;

                        if (int.TryParse(valueString, out value))
                        {
                            Configuration.SetValue(path, value);
                        }
                    }
                    else if (Configuration.GetValueType(path) == typeof(float))
                    {
                        float value = 0;
                        var valueString = entries.First(x => x.name == path).GetComponentInChildren<InputField>().text;

                        if (float.TryParse(valueString, out value))
                        {
                            Configuration.SetValue(path, value);
                        }
                    }
                }
            }
        }

        public static void RecalculateHeights()
        {
            float maxSectionHeight = 0f;
            foreach (var section in sections)
            {
                float maxHeight = 0f;

                foreach (var entry in entries.Where(x => x.name.StartsWith(section.name.Split('.')[1] + ".")))
                {
                    entry.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -maxHeight);
                    maxHeight += Math.Max(33f, entry.transform.Find("ConfigName").GetComponent<RectTransform>().rect.height + 15f);
                }

                maxSectionHeight += maxHeight + 50;
                section.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, maxHeight + 50);
                section.transform.Find("Panel").GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, maxHeight + 5);
            }
            ContentGrid.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, maxSectionHeight);
            GUIRoot.transform.Find("Canvas/Scroll View").GetComponent<ScrollRect>().normalizedPosition = new Vector2(0, 1);
        }
    }
}