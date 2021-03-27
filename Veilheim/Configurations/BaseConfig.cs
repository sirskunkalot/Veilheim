// Veilheim
// a Valheim mod
// 
// File:    BaseConfig.cs
// Project: Veilheim

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using IniParser.Model;
using UnityEngine;

namespace Veilheim.Configurations
{
    public interface IConfig
    {
        bool IsEnabled { get; set; }
        void LoadIniData(KeyDataCollection data);
    }

    public abstract class BaseConfig : INotifyPropertyChanged
    {
        public static readonly Dictionary<Type, List<PropertyInfo>> propertyCache = new Dictionary<Type, List<PropertyInfo>>();
        public static readonly Dictionary<Type, Dictionary<string, object>> defaultValueCache = new Dictionary<Type, Dictionary<string, object>>();

        public event PropertyChangedEventHandler PropertyChanged;

        private bool _isEnabled;

        public EventHandler<SectionStatusChangeEventArgs> SectionStatusChangedEvent;

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (value != _isEnabled)
                {
                    SectionStatusChangedEvent?.Invoke(this, new SectionStatusChangeEventArgs(value));
                }

                _isEnabled = value;
            }
        }


        internal static IEnumerable<PropertyInfo> GetProps<T>()
        {
            return GetProps(typeof(T));
        }

        internal static IEnumerable<PropertyInfo> GetProps(Type t)
        {
            if (!propertyCache.ContainsKey(t))
            {
                propertyCache.Add(t, t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy).ToList());
            }

            foreach (var property in propertyCache[t])
            {
                yield return property;
            }
        }

        public void SetValue<T>(string propertyName, object value)
        {
            var p = GetProps(typeof(T)).FirstOrDefault(x => x.Name == propertyName);
            if (p == null)
            {
                throw new ArgumentException($"Property {propertyName} does not exist in {typeof(T).Name}");
            }

            var oldValue = p.GetValue(this, null);
            p.SetValue(this, value, null);
            if (oldValue != value)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p.Name));
            }
        }

        public object GetDefault(Type sectionType, string propertyName)
        {
            return defaultValueCache[sectionType][propertyName];
        }

        public U GetValue<U>(string propertyName)
        {
            return (U) GetProps(GetType()).FirstOrDefault(x => x.Name == propertyName).GetValue(this, null);
        }

        public U GetDefault<U>(string propertyName)
        {
            return (U) defaultValueCache[GetType()][propertyName];
        }

        public void CacheDefaults()
        {
            if (defaultValueCache.ContainsKey(GetType()))
            {
                return;
            }

            var temp = new Dictionary<string, object>();
            foreach (var p in GetProps(GetType()))
            {
                temp.Add(p.Name, p.GetValue(this, null));
            }

            defaultValueCache.Add(GetType(), temp);
        }
    }

    public abstract class BaseConfig<T> : BaseConfig, IConfig where T : IConfig, INotifyPropertyChanged, new()
    {
        public static IniData iniUpdated = null;



        public void LoadIniData(KeyDataCollection data)
        {
            IsEnabled = true;

            foreach (var prop in GetProps<T>())
            {
                var keyName = prop.Name;

                if (!data.ContainsKey(keyName))
                {
                    Logger.LogWarning($" Key {keyName} not defined, using default value");
                    continue;
                }

                Logger.LogInfo($" Loading key {keyName}");
                var existingValue = prop.GetValue(this, null);

                if (prop.PropertyType == typeof(float))
                {
                    SetValue<T>(keyName, data.GetFloat(keyName, (float) existingValue));
                    continue;
                }

                if (prop.PropertyType == typeof(int))
                {
                    SetValue<T>(keyName, data.GetInt(keyName, (int) existingValue));
                    continue;
                }

                if (prop.PropertyType == typeof(bool))
                {
                    SetValue<T>(keyName, data.GetBool(keyName));
                    continue;
                }

                if (prop.PropertyType == typeof(KeyCode))
                {
                    SetValue<T>(keyName, data.GetKeyCode(keyName, (KeyCode) existingValue));
                    continue;
                }

                Logger.LogWarning($" Could not load data of type {prop.PropertyType} for key {keyName}");
            }
        }

        public static T LoadIni(IniData data, string section)
        {
            var n = new T();

            Logger.LogInfo($"Loading config section {section}");
            if (data[section] != null)
            {
                n.LoadIniData(data[section]);
            }

            if (data[section] == null || data[section][nameof(IsEnabled)] == null || !data[section].GetBool(nameof(IsEnabled)))
            {
                n.IsEnabled = false;
                Logger.LogInfo(" Section not enabled");
            }

            return n;
        }
    }

    public abstract class ServerSyncConfig<T> : BaseConfig<T>, ISyncableSection where T : class, IConfig, INotifyPropertyChanged, new()
    {
    }
}