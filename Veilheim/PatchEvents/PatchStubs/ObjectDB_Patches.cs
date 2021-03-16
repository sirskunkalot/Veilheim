// Veilheim
// a Valheim mod
// 
// File:    ObjectDB_Patches.cs
// Project: Veilheim

using System;
using HarmonyLib;

namespace Veilheim.PatchEvents.PatchStubs
{
    /// <summary>
    ///     Patch ObjectDB.Awake
    /// </summary>
    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
    public class ObjectDB_Awake_Patch
    {
        public delegate void BlockingPrefixHandler(ObjectDB instance, ref bool cancel);

        public delegate void PostfixHandler(ObjectDB instance);

        public delegate void PrefixHandler(ObjectDB instance);

        public static event PrefixHandler PrefixEvent;

        public static event BlockingPrefixHandler BlockingPrefixEvent;

        public static event PostfixHandler PostfixEvent;


        private static bool Prefix(ObjectDB __instance)
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

        private static void Postfix(ObjectDB __instance)
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

    /// <summary>
    ///     Patch ObjectDB.CopyOtherDB
    /// </summary>
    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
    public class ObjectDB_Patches
    {
        public delegate void BlockingPrefixHandler(ObjectDB instance, ref bool cancel);

        public delegate void PostfixHandler(ObjectDB instance);

        public delegate void PrefixHandler(ObjectDB instance);

        public static event PrefixHandler PrefixEvent;

        public static event BlockingPrefixHandler BlockingPrefixEvent;

        public static event PostfixHandler PostfixEvent;


        private static bool Prefix(ObjectDB __instance)
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

        private static void Postfix(ObjectDB __instance)
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