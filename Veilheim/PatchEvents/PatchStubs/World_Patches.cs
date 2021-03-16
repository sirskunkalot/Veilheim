// Veilheim

using System;
using HarmonyLib;

namespace Veilheim.PatchEvents.PatchStubs
{
    /// <summary>
    ///     Patch World.SetMapData
    /// </summary>
    [HarmonyPatch(typeof(World), nameof(World.LoadWorld))]
    public class World_Start_Patch
    {
        public delegate void BlockingPrefixHandler(World instance, ref bool cancel);

        public delegate void PostfixHandler(World instance);

        public delegate void PrefixHandler(World instance);

        public static event PrefixHandler PrefixEvent;

        public static event BlockingPrefixHandler BlockingPrefixEvent;

        public static event PostfixHandler PostfixEvent;


        private static bool Prefix(World __instance)
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

        private static void Postfix(World __instance)
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