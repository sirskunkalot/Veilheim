// Veilheim
// a Valheim mod
// 
// File:    ZNetScene_Patches.cs
// Project: Veilheim

using System;
using HarmonyLib;

namespace Veilheim.PatchEvents.PatchStubs
{
    /// <summary>
    ///     Patch ZNetScene.Awake
    /// </summary>
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    public class ZNetScene_Patches
    {
        public delegate void BlockingPrefixHandler(ZNetScene instance, ref bool cancel);

        public delegate void PostfixHandler(ZNetScene instance);

        public delegate void PrefixHandler(ZNetScene instance);

        public static event PrefixHandler PrefixEvent;

        public static event BlockingPrefixHandler BlockingPrefixEvent;

        public static event PostfixHandler PostfixEvent;


        private static bool Prefix(ZNetScene __instance)
        {
            Logger.LogInfo($"{__instance} spawned.");

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

        private static void Postfix(ZNetScene __instance)
        {
            Logger.LogInfo($"{__instance} awoken.");

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