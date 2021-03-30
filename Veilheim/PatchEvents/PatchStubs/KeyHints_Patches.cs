// Veilheim
// a Valheim mod
// 
// File:    KeyHints_Patches.cs
// Project: Veilheim

using System;
using HarmonyLib;

namespace Veilheim.PatchEvents.PatchStubs
{
    /// <summary>
    ///     Patch KeyHints.Start
    /// </summary>
    [HarmonyPatch(typeof(KeyHints), nameof(KeyHints.UpdateHints))]
    public class KeyHints_UpdateHints_Patch
    {
        public delegate void BlockingPrefixHandler(KeyHints instance, ref bool cancel);

        public delegate void PrefixHandler(KeyHints instance);

        public delegate void PostfixHandler(KeyHints instance);

        public static event BlockingPrefixHandler BlockingPrefixEvent;

        public static event PrefixHandler PrefixEvent;

        public static event PostfixHandler PostfixEvent;


        private static bool Prefix(KeyHints __instance)
        {
            var cancel = false;
            BlockingPrefixEvent?.Invoke(__instance, ref cancel);

            if (!cancel)
            {
                try
                {
                    PrefixEvent?.Invoke(__instance);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex.Message + Environment.NewLine + ex.StackTrace);
                }
            }

            return !cancel;
        }

        private static void Postfix(KeyHints __instance)
        {
            try
            {
                PostfixEvent?.Invoke(__instance);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
    }
}