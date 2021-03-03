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

namespace Veilheim.Configurations
{
    // Configuration implementation part
    public partial class Configuration
    {
        public static string ConfigIniPath = Path.Combine(Path.GetDirectoryName(Paths.BepInExConfigPath), "Veilheim");

        internal static readonly List<PropertyInfo> propertyCache;

        static Configuration()
        {
            // Fill property cache
            propertyCache = typeof(Configuration).GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
        }

        public Configuration()
        {
            // Create all configuration entries with default values
            foreach (var property in propertyCache)
            {
                //ZLog.Log($"Initializing configuration section {property.Name}");
                Logger.LogInfo($"Initializing configuration section {property.Name}");
                var section = Activator.CreateInstance(property.PropertyType, true);
                property.SetValue(this, section, null);

                // and finally cache the default values
                ((BaseConfig) section).CacheDefaults();
            }
        }

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

                    if (ZNet.instance.IsLocalInstance() ||
                        (ZNet.instance.IsClientInstance() && !needsSync) ||
                        (ZNet.instance.IsServerInstance() && needsSync))
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
            //foreach (var property in propertyCache)
            foreach (var property in propertyCache.Where(x => typeof(ISyncableSection).IsAssignableFrom(x.PropertyType)))
            {
                sb.AppendLine(GenerateSection(property, property.GetValue(this, null)));
            }

            return sb.ToString();
        }


        /// <summary>
        ///     Save full config
        ///     Do not call this from startup, ZNet isn't initialized yet
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
        /// <param name="property">Configuration property</param>
        private void SaveConfiguration(PropertyInfo property)
        {
            // For clients only save client values, for servers only server values
            var section = property.GetValue(Current, null);
            var needsSync = section is ISyncableSection;

            if (ZNet.instance.IsLocalInstance() ||
                (ZNet.instance.IsClientInstance() && !needsSync) ||
                (ZNet.instance.IsServerInstance() && needsSync))
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
            var sectionAttribute = sectionType.GetCustomAttributes(false).OfType<ConfigurationSectionAttribute>().FirstOrDefault();

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