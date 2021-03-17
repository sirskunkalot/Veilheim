// Veilheim
// a Valheim mod
// 
// File:    GameCamera_Patches.cs
// Project: Veilheim

using System;
using HarmonyLib;

namespace Veilheim.PatchEvents.PatchStubs
{
    [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateCamera))]
    public class GameCamera_UpdateCamera_Patch
    {
        public delegate void BlockingPrefixHandler(GameCamera instance, ref bool cancel);

        public delegate void PostfixHandler(GameCamera instance);

        public delegate void PrefixHandler(GameCamera instance);

        public static event PrefixHandler PrefixEvent;

        public static event BlockingPrefixHandler BlockingPrefixEvent;

        public static event PostfixHandler PostfixEvent;


        private static bool Prefix(GameCamera __instance)
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

        private static void Postfix(GameCamera __instance)
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