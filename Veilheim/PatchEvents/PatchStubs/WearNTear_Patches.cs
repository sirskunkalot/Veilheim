// Veilheim
// a Valheim mod
// 
// File:    WearNTear_Patches.cs
// Project: Veilheim

using System;
using HarmonyLib;

namespace Veilheim.PatchEvents.PatchStubs
{
    /// <summary>
    ///     Patch WearNTear.Destroy
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Destroy))]
    public class WearNTear_Destroy_Patch
    {
        public delegate void BlockingPrefixHandler(WearNTear instance, ref bool cancel);

        public delegate void PostfixHandler(WearNTear instance);

        public delegate void PrefixHandler(WearNTear instance);

        public static event PrefixHandler PrefixEvent;

        public static event BlockingPrefixHandler BlockingPrefixEvent;

        public static event PostfixHandler PostfixEvent;


        private static bool Prefix(WearNTear __instance)
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

        private static void Postfix(WearNTear __instance)
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