// Veilheim

using System;
using HarmonyLib;

namespace Veilheim.PatchEvents.PatchStubs
{
    /// <summary>
    ///     Patch Minimap.SetMapData
    /// </summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.SetMapData))]
    public class Minimap_Start_Patch
    {
        public delegate void BlockingPrefixHandler(Minimap instance, ref bool cancel);

        public delegate void PostfixHandler(Minimap instance);

        public delegate void PrefixHandler(Minimap instance);

        public static event PrefixHandler PrefixEvent;

        public static event BlockingPrefixHandler BlockingPrefixEvent;

        public static event PostfixHandler PostfixEvent;


        private static bool Prefix(Minimap __instance)
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

        private static void Postfix(Minimap __instance)
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

    /// <summary>
    ///     Patch Minimap.UpdateLocationPins
    /// </summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdateLocationPins))]
    public class Minimap_UpdateLocationPins_Patch
    {
        public delegate void BlockingPrefixHandler(Minimap instance, ref bool cancel);

        public delegate void PostfixHandler(Minimap instance);

        public delegate void PrefixHandler(Minimap instance);

        public static event PrefixHandler PrefixEvent;

        public static event BlockingPrefixHandler BlockingPrefixEvent;

        public static event PostfixHandler PostfixEvent;


        private static bool Prefix(Minimap __instance)
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

        private static void Postfix(Minimap __instance)
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

    /// <summary>
    ///     Patch Minimap.UpdateExplore
    /// </summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdateExplore))]
    public class Minimap_UpdateExplore_Patch
    {
        public delegate void BlockingPrefixHandler(Minimap instance, ref bool cancel);

        public delegate void PostfixHandler(Minimap instance);

        public delegate void PrefixHandler(Minimap instance);

        public static event PrefixHandler PrefixEvent;

        public static event BlockingPrefixHandler BlockingPrefixEvent;

        public static event PostfixHandler PostfixEvent;


        private static bool Prefix(Minimap __instance)
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

        private static void Postfix(Minimap __instance)
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