// Veilheim

using System;
using HarmonyLib;

namespace Veilheim.PatchEvents.PatchStubs
{
    /// <summary>
    ///     Patch Game.Start
    /// </summary>
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    public class Game_Start_Patch
    {
        public delegate void BlockingPrefixHandler(Game instance, ref bool cancel);

        public delegate void PostfixHandler(Game instance);

        public delegate void PrefixHandler(Game instance);

        public static event PrefixHandler PrefixEvent;

        public static event BlockingPrefixHandler BlockingPrefixEvent;

        public static event PostfixHandler PostfixEvent;


        private static bool Prefix(Game __instance)
        {
            Logger.LogInfo($"{__instance} starting.");

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

        private static void Postfix(Game __instance)
        {
            Logger.LogInfo($"{__instance} started.");

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