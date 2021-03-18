// Veilheim
// a Valheim mod
// 
// File:    Localization_Patches.cs
// Project: Veilheim

using System;
using HarmonyLib;

namespace Veilheim.PatchEvents.PatchStubs
{
    /// <summary>
    ///     Patch Localization.SetupLanguage
    /// </summary>
    [HarmonyPatch(typeof(Localization), nameof(Localization.SetupLanguage))]
    public class Localization_SetupLanguage_Patch
    {
        public delegate void PostfixHandler(string language);

        public static event PostfixHandler PostfixEvent;

        private static void Postfix(string language)
        {
            try
            {
                PostfixEvent?.Invoke(language);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
    }

    /// <summary>
    ///     Patch Localization.Translate
    /// </summary>
    [HarmonyPatch(typeof(Localization), nameof(Localization.Translate))]
    public class Localization_Translate_Patch
    {
        public delegate bool PostfixHandler(string word, out string translated);

        public static event PostfixHandler PostfixEvent;

        private static string Postfix(string result, string word)
        {
            var failed = $"[{word}]";
            if (result != failed)
            {
                return result;
            }

            if (PostfixEvent.Invoke(word, out var translated))
            {
                return translated;
            }

            return failed;
        }
    }
}