// Veilheim
// a Valheim mod
// 
// File:    Minimap_Patches.cs
// Project: Veilheim

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

    /// <summary>
    ///     Patch Minimap.UpdateExplore
    /// </summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Explore), typeof(int), typeof(int))]
    public class Minimap_Explore_Patch
    {
        public delegate void BlockingPrefixHandler(Minimap instance, ref bool cancel);

        public delegate void PostfixHandler(Minimap instance, int x, int y, bool __result);

        public delegate void PrefixHandler(Minimap instance, int x, int y);

        public static event PrefixHandler PrefixEvent;

        public static event BlockingPrefixHandler BlockingPrefixEvent;

        public static event PostfixHandler PostfixEvent;


        private static bool Prefix(Minimap __instance, int x, int y)
        {
            var cancel = false;
            BlockingPrefixEvent?.Invoke(__instance, ref cancel);

            if (!cancel)
            {
                try
                {
                    PrefixEvent?.Invoke(__instance, x, y);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex.Message + Environment.NewLine + ex.StackTrace);
                }
            }

            return !cancel;
        }

        private static void Postfix(Minimap __instance, int x, int y, bool __result)
        {
            try
            {
                PostfixEvent?.Invoke(__instance, x, y, __result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
    }


    /// <summary>
    ///     Patch Minimap.SetMapData
    /// </summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Awake))]
    public class Minimap_Awake_Patch
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