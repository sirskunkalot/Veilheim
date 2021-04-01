// Veilheim
// a Valheim mod
// 
// File:    ConfigurationExtra.cs
// Project: Veilheim

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using IniParser;
using IniParser.Model;
using IniParser.Parser;
using UnityEngine;
using Veilheim.ConsoleCommands;

namespace Veilheim.Configurations
{
    // Configuration implementation part
    public partial class Configuration
    {
        internal static readonly List<PropertyInfo> propertyCache;

        internal static bool PlayerIsAdmin = true;  // defaults to true for local instances

        static Configuration()
        {
            // Create dir if not existant
            ConfigIniPath = Path.Combine(Path.GetDirectoryName(Paths.BepInExConfigPath), VeilheimPlugin.PluginName);
            if (!Directory.Exists(ConfigIniPath))
            {
                Directory.CreateDirectory(ConfigIniPath);
            }

            // Fill property cache
            propertyCache = typeof(Configuration).GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
        }

        public Configuration()
        {
            // Create all configuration entries with default values
            foreach (var property in propertyCache)
            {
                Logger.LogDebug($"Initializing configuration section {property.Name}");
                var section = Activator.CreateInstance(property.PropertyType, true);
                property.SetValue(this, section, null);

                // and finally cache the default values
                ((BaseConfig)section).CacheDefaults();
            }
        }

        public static string ConfigIniPath { get; }

        // Loaded configuration
        public static Configuration Current { get; private set; }

        private static string GetIniFilePath(PropertyInfo property)
        {
            var needsSync = typeof(ISyncableSection).IsAssignableFrom(property.PropertyType);
            var iniPath = ConfigIniPath;

            if (needsSync && (ZNet.instance.IsServerInstance() || ZNet.instance.IsLocalInstance()))
            {
                iniPath = Path.Combine(iniPath, ZNet.instance.GetWorldUID().ToString());

                if (!Directory.Exists(iniPath))
                {
                    Directory.CreateDirectory(iniPath);
                }
            }

            return Path.Combine(iniPath, property.Name + ".ini");
        }

        /// <summary>
        ///     Load configuration from ini files.
        /// </summary>
        /// <returns>true if successful</returns>
        public static bool LoadConfiguration()
        {
            var errorWhileLoadingIni = false;
            Current = new Configuration();
            var sb = new StringBuilder();
            try
            {
                if (!Directory.Exists(ConfigIniPath))
                {
                    Directory.CreateDirectory(ConfigIniPath);
                }

                // For every property (representing a section)
                foreach (var property in propertyCache)
                {
                    // Server just reads syncable, client just base and local both sections
                    var needsSync = typeof(ISyncableSection).IsAssignableFrom(property.PropertyType);

                    if (ZNet.instance.IsLocalInstance() || ZNet.instance.IsClientInstance() && !needsSync || ZNet.instance.IsServerInstance() && needsSync)
                    {
                        // Load section from ini or create ini with default values
                        var iniPath = GetIniFilePath(property);

                        if (File.Exists(iniPath))
                        {
                            errorWhileLoadingIni |= !LoadFromIni(Current, property, iniPath, sb);
                        }
                        else
                        {
                            Logger.LogInfo($"Saving missing default config for {property.Name}");
                            Current.SaveConfiguration(property);
                        }
                    }
                }

                // If there were errors while loading the ini files, try to write missing ones
                if (errorWhileLoadingIni)
                {
                    Logger.LogError("Error(s) occured loading configuration files");
                    Logger.LogError(sb.ToString());
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message);
                Logger.LogError(e.StackTrace);
            }

            return !errorWhileLoadingIni;
        }

        /// <summary>
        ///     Get all section which need to synced with clients
        /// </summary>
        /// <returns></returns>
        public string GetSyncableSections()
        {
            var sb = new StringBuilder();
            foreach (var property in propertyCache.Where(x => typeof(ISyncableSection).IsAssignableFrom(x.PropertyType)))
            {
                sb.AppendLine(GenerateSection(property, property.GetValue(this, null)));
            }

            return sb.ToString();
        }

        public IEnumerable<PropertyInfo> GetSections()
        {
            return propertyCache;
        }


        /// <summary>
        ///     Save full config
        /// </summary>
        public void SaveConfiguration()
        {
            foreach (var property in propertyCache)
            {
                SaveConfiguration(property);
            }
        }

        /// <summary>
        ///     Save configuration section to its ini file
        /// </summary>
        /// <param name="property">PropertyInfo representing a config section</param>
        private void SaveConfiguration(PropertyInfo property)
        {
            // For clients only save client values, for servers only server values
            var section = property.GetValue(Current, null);
            var needsSync = section is ISyncableSection;

            if (ZNet.instance.IsLocalInstance() || ZNet.instance.IsClientInstance() && !needsSync || ZNet.instance.IsServerInstance() && needsSync)
            {
                var iniPath = GetIniFilePath(property);

                using (TextWriter tw = new StreamWriter(iniPath))
                {
                    tw.Write(GenerateSection(property, section, true));
                }
            }
        }

        /// <summary>
        ///     Generate ini section as string
        /// </summary>
        /// <param name="property">Configuration property</param>
        /// <param name="section">section object</param>
        /// <param name="withComments">add comments?</param>
        /// <returns></returns>
        private static string GenerateSection(PropertyInfo property, object section, bool withComments = false)
        {
            var sb = new StringBuilder();
            var sectionType = property.PropertyType;
            var sectionAttribute = sectionType.GetCustomAttributes(false).OfType<ConfigurationSectionAttribute>().First();

            if (sectionAttribute != null && withComments)
            {
                sb.AppendLine(sectionAttribute.Comment.ToIniComment());
                sb.AppendLine("; V" + sectionAttribute.SinceVersion);
            }

            sb.AppendLine($"[{property.Name}]");

            // IsEnabled first

            var enabledProperty = typeof(IConfig).GetProperty(nameof(IConfig.IsEnabled), BindingFlags.Public | BindingFlags.Instance);
            var enabledValue = enabledProperty.GetValue(section, null);
            var enabledAsString = enabledValue.ToString();

            if (withComments)
            {
                sb.AppendLine("; Change false to true to enable this section");
            }

            sb.AppendLine($"{enabledProperty.Name}={enabledAsString}");

            if (withComments)
            {
                sb.AppendLine();
            }

            foreach (var configProperty in BaseConfig.GetProps(sectionType))
            {
                var value = configProperty.GetValue(section, null);
                var valueAsString = value.ToString();
                if (value is float)
                {
                    valueAsString = ((float)value).ToString(CultureInfo.InvariantCulture.NumberFormat);
                }

                // Special case 'IsEnabled' is already handled above
                if (configProperty.Name == nameof(IConfig.IsEnabled))
                {
                    continue;
                }

                var ca = configProperty.GetCustomAttributes(false).OfType<ConfigurationAttribute>().FirstOrDefault();

                if (ca != null && withComments)
                {
                    sb.Append(ca.Comment.ToIniComment());
                    sb.AppendLine($"; V{ca.SinceVersion}");
                }

                sb.AppendLine($"{configProperty.Name}={valueAsString}");

                if (withComments)
                {
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        public static bool LoadFromIni(Configuration config, PropertyInfo property, string filename, StringBuilder sb)
        {
            try
            {
                var parser = new FileIniDataParser();

                var configdata = new IniData();
                if (File.Exists(filename))
                {
                    configdata = parser.ReadFile(filename);
                }

                var keyName = property.Name;
                var method = property.PropertyType.GetMethod("LoadIni", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

                if (method != null)
                {
                    var result = method.Invoke(null, new object[] { configdata, keyName });
                    property.SetValue(config, result, null);
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"Error loading ini file {filename}");
                Logger.LogError(e.Message);
                sb.AppendLine($"Error loading ini file {filename}");
                sb.AppendLine($"{e.Message}");
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Only set values which should be synced to current configuration
        /// </summary>
        /// <param name="receivedConfig"></param>
        public static void SetSyncableValues(Configuration receivedConfig)
        {
            foreach (var property in propertyCache.Where(x => typeof(ISyncableSection).IsAssignableFrom(x.PropertyType)))
            {
                property.SetValue(Current, property.GetValue(receivedConfig, null), null);
            }
        }

        public static void LoadFromIniString(Configuration receivedConfig, string inputString)
        {
            try
            {
                var parser = new IniDataParser();
                var ini = parser.Parse(inputString);

                foreach (var property in propertyCache)
                {
                    if (ini.Sections.ContainsSection(property.Name))
                    {
                        var result = property.PropertyType.GetMethod("LoadIni", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                            ?.Invoke(null, new object[] { ini, property.Name });
                        if (result == null)
                        {
                            throw new Exception($"LoadIni method not found on Type {property.PropertyType.Name}");
                        }

                        property.SetValue(receivedConfig, result, null);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"Error {e.Message} occured while parsing synced config");

                Logger.LogError($"while parsing {Environment.NewLine}{inputString}");
            }
        }

        /// <summary>
        /// Set value in configuration, send syncable's to the server
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyPath"></param>
        /// <param name="value"></param>
        public static void SetValue<T>(string propertyPath, T value) where T : IEquatable<T>
        {
            string[] pathParts = propertyPath.Split('.');
            if (pathParts.Length != 2)
            {
                throw new Exception($"Not a suitable propertyPath: {propertyPath}");
            }

            PropertyInfo sectionProperty = typeof(Configuration).GetProperty(pathParts[0], BindingFlags.Public | BindingFlags.Instance);
            if (sectionProperty == null)
            {
                throw new Exception($"Could not find property (section) for {pathParts[0]}");
            }

            BaseConfig sectionValue = sectionProperty.GetValue(Configuration.Current, null) as BaseConfig;

            PropertyInfo entryProperty = sectionValue.GetType().GetProperty(pathParts[1], BindingFlags.Public | BindingFlags.Instance);
            if (entryProperty == null)
            {
                throw new Exception($"Could not find property (entry) for {pathParts[1]}");
            }

            bool equal = value.Equals((T) entryProperty.GetValue(sectionValue, null));
            entryProperty.SetValue(sectionValue, value, null);
            if (!equal)
            {
                Logger.LogDebug($"Setting Property value for {propertyPath} -> {value}");
                if (typeof(ISyncableSection).IsAssignableFrom(sectionProperty.PropertyType))
                {
                    var zPgk = new ZPackage();
                    zPgk.Write($"setvalue {sectionProperty.Name}.{entryProperty.Name} {value}");
                    ZRoutedRpc.instance.InvokeRoutedRPC(nameof(SetConfigurationValue.RPC_Veilheim_SetConfigurationValue), zPgk);
                }
            }
        }

        /// <summary>
        /// Get value from configuration by name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyPath">Propertypath (section.entry)</param>
        /// <returns>configuration value</returns>
        public static T GetValue<T>(string propertyPath)
        {
            string[] pathParts = propertyPath.Split('.');
            if (pathParts.Length != 2)
            {
                throw new Exception($"Not a suitable propertyPath: {propertyPath}");
            }

            PropertyInfo sectionProperty = typeof(Configuration).GetProperty(pathParts[0], BindingFlags.Public | BindingFlags.Instance);
            if (sectionProperty == null)
            {
                throw new Exception($"Could not find property (section) for {pathParts[0]}");
            }

            BaseConfig sectionValue = sectionProperty.GetValue(Configuration.Current, null) as BaseConfig;

            PropertyInfo entryProperty = sectionValue.GetType().GetProperty(pathParts[1], BindingFlags.Public | BindingFlags.Instance);
            if (entryProperty == null)
            {
                throw new Exception($"Could not find property (entry) for {pathParts[1]}");
            }

            // Logger.LogDebug($"Reading Property value for {propertyPath} -> {entryProperty.GetValue(sectionValue, null)}");
            return (T)entryProperty.GetValue(sectionValue, null);
        }

        /// <summary>
        /// Get type of configuration entry
        /// </summary>
        /// <param name="propertyPath">Property path (section.entry)</param>
        /// <returns>Type of configuration value</returns>
        public static Type GetValueType(string propertyPath)
        {
            string[] pathParts = propertyPath.Split('.');
            if (pathParts.Length != 2)
            {
                throw new Exception($"Not a suitable propertyPath: {propertyPath}");
            }

            PropertyInfo sectionProperty = typeof(Configuration).GetProperty(pathParts[0], BindingFlags.Public | BindingFlags.Instance);
            if (sectionProperty == null)
            {
                throw new Exception($"Could not find property (section) for {pathParts[0]}");
            }

            BaseConfig sectionValue = sectionProperty.GetValue(Configuration.Current, null) as BaseConfig;

            PropertyInfo entryProperty = sectionValue.GetType().GetProperty(pathParts[1], BindingFlags.Public | BindingFlags.Instance);
            if (entryProperty == null)
            {
                throw new Exception($"Could not find property (entry) for {pathParts[1]}");
            }

            // Logger.LogDebug($"Reading Property type for {propertyPath} -> {entryProperty.PropertyType.Name}");
            return entryProperty.PropertyType;
        }

        public static string GetSectionDescription(PropertyInfo property)
        {
            if (property.PropertyType.GetCustomAttribute<ConfigurationSectionAttribute>() == null)
            {
                Logger.LogError($"Configuration-Section {property.Name} has no ConfigurationSection attribute");
                return property.Name;
            }

            ConfigurationSectionAttribute csa = property.PropertyType.GetCustomAttribute<ConfigurationSectionAttribute>();
            return csa.Comment;
        }

        public static string GetEntryDescription(PropertyInfo property)
        {
            if (property.GetCustomAttribute<ConfigurationAttribute>() == null)
            {
                Logger.LogError($"Configuration-Value {property.Name} has no Configuration attribute");
                return property.Name;
            }

            ConfigurationAttribute csa = property.GetCustomAttribute<ConfigurationAttribute>();
            return csa.Comment;
        }
    }

    public static class StringExtensions
    {
        public static string ToIniComment(this string comment)
        {
            var sb = new StringBuilder();
            foreach (var line in comment.Split('\n'))
            {
                sb.AppendLine("; " + line);
            }

            return sb.ToString();
        }
    }


    public static class IniDataExtensions
    {
        public static float GetFloat(this KeyDataCollection data, string key, float defaultVal)
        {
            if (float.TryParse(data[key], NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var result))
            {
                return result;
            }

            Logger.LogWarning($" [Float] Could not read {key}, using default value of {defaultVal}");
            return defaultVal;
        }

        public static bool GetBool(this KeyDataCollection data, string key)
        {
            var truevals = new[] { "y", "yes", "true" };
            return truevals.Contains(data[key].ToLower());
        }

        public static int GetInt(this KeyDataCollection data, string key, int defaultVal)
        {
            if (int.TryParse(data[key], NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var result))
            {
                return result;
            }

            Logger.LogWarning($" [Int] Could not read {key}, using default value of {defaultVal}");
            return defaultVal;
        }

        public static KeyCode GetKeyCode(this KeyDataCollection data, string key, KeyCode defaultVal)
        {
            if (Enum.TryParse<KeyCode>(data[key], out var result))
            {
                return result;
            }

            Logger.LogWarning($" [KeyCode] Could not read {key}, using default value of {defaultVal}");
            return defaultVal;
        }
    }
}