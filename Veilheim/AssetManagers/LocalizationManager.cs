// Veilheim
// a Valheim mod
// 
// File:    LocalizationManager.cs
// Project: Veilheim

using System.Collections.Generic;
using UnityEngine;
using Veilheim.AssetEntities;
using Veilheim.PatchEvents;

namespace Veilheim.AssetManagers
{
    /// <summary>
    ///     Handles translation of asset bundle content.
    /// </summary>
    internal class LocalizationManager : AssetManager, IPatchEventConsumer
    {
        public static LocalizationManager Instance { get; private set; }

        internal List<LocalizationDef> Localizations = new List<LocalizationDef>();

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
            Logger.LogInfo("Initialized LocalizationManager");
        }

        internal void AddLocalization(LocalizationDef localization)
        {
            if (!Localizations.Contains(localization))
            {
                Localizations.Add(localization);
            }
        }

        /// <summary>
        ///     Setup languages for all registered <see cref="LocalizationDef" />
        /// </summary>
        /// <param name="language"></param>
        [PatchEvent(typeof(Localization), nameof(Localization.SetupLanguage), PatchEventType.Postfix)]
        public static void SetupLanguage(string language)
        {
            if (Instance.Localizations.Count > 0)
            {
                Logger.LogMessage($"----Setting up localizations for custom assets----");

                foreach (var localization in Instance.Localizations)
                {
                    localization.SetupLanguage(language);
                }
            }
        }

        /// <summary>
        ///     Try to translate a string in all registered <see cref="LocalizationDef" />s. First to translate wins.
        /// </summary>
        /// <param name="word"></param>
        /// <param name="translated"></param>
        [PatchEvent(typeof(Localization), nameof(Localization.Translate), PatchEventType.Postfix)]
        public static bool TryTranslate(string word, out string translated)
        {
            var isTranslated = false;
            var _translated = "";

            // first translation wins
            foreach (var localization in Instance.Localizations)
            {
                isTranslated = localization.TryTranslate(word, out _translated);
                if (isTranslated)
                {
                    break;
                }
            }

            translated = isTranslated ? _translated : $"[{word}]";

            return isTranslated;
        }

    }
}
