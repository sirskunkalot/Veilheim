// Veilheim
// a Valheim mod
// 
// File:    ConfigDescriptionExtension.cs
// Project: Veilheim

using System.IO;
using BepInEx.Configuration;

namespace Veilheim.Utils
{
    public static class ConfigUtil
    {
        public static T Get<T>(this ConfigEntryBase def)
        {
            return (T)def.BoxedValue;
        }

        public static T Get<T>(string section, string key)
        {
            return VeilheimPlugin.Instance.Config[section, key].Get<T>();
        }

        public static string GetConfigIniPath()
        {
            return Path.GetDirectoryName(VeilheimPlugin.Instance.Config.ConfigFilePath);
        }
    }
}