// Veilheim
// a Valheim mod
// 
// File:    Menu_Patches.cs
// Project: Veilheim

using System;
using HarmonyLib;

namespace Veilheim.PatchEvents.PatchStubs
{
    [HarmonyPatch(typeof(Menu), nameof(Menu.IsVisible))]
    public class Menu_IsVisible_Patch
    {
        public delegate void BlockingPrefixHandler(ref bool __result, ref bool cancel);

        public delegate void PostfixHandler(ref bool __result);

        public delegate void PrefixHandler(ref bool __result);

        public static event PrefixHandler PrefixEvent;

        public static event BlockingPrefixHandler BlockingPrefixEvent;

        public static event PostfixHandler PostfixEvent;


        private static bool Prefix(ref bool __result)
        {
            var cancel = false;
            BlockingPrefixEvent?.Invoke(ref __result, ref cancel);

            if (!cancel)
            {
                try
                {
                    PrefixEvent?.Invoke(ref __result);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex.Message + Environment.NewLine + ex.StackTrace);
                }
            }

            return !cancel;
        }

        private static void Postfix(ref bool __result)
        {
            try
            {
                PostfixEvent?.Invoke(ref __result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
    }
}