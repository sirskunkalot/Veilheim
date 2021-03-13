// Veilheim

using System;
using HarmonyLib;

namespace Veilheim.PatchEvents.PatchStubs
{
    /// <summary>
    ///     Patch TeleportWorld.Interact
    /// </summary>
    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Interact), typeof(Humanoid), typeof(bool))]
    public class TeleportWorld_Interact_Patch
    {
        public delegate void BlockingPrefixHandler(TeleportWorld instance, ref bool cancel);

        public delegate void PostfixHandler(TeleportWorld instance, Humanoid human, bool hold);

        public delegate void PrefixHandler(TeleportWorld instance);

        public static event PrefixHandler PrefixEvent;

        public static event BlockingPrefixHandler BlockingPrefixEvent;

        public static event PostfixHandler PostfixEvent;


        private static bool Prefix(TeleportWorld __instance)
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

        private static void Postfix(TeleportWorld __instance, Humanoid human, bool hold)
        {
            try
            {
                PostfixEvent?.Invoke(__instance, human, hold);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
    }

    /// <summary>
    ///     Patch TeleportWorld.RPC_SetTag
    /// </summary>
    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.RPC_SetTag), typeof(long), typeof(string))]
    public class TeleportWorld_RPC_SetTag_Patch
    {
        public delegate void BlockingPrefixHandler(TeleportWorld instance, ref bool cancel);

        public delegate void PostfixHandler(TeleportWorld instance, long sender, string tag);

        public delegate void PrefixHandler(TeleportWorld instance);

        public static event PrefixHandler PrefixEvent;

        public static event BlockingPrefixHandler BlockingPrefixEvent;

        public static event PostfixHandler PostfixEvent;


        private static bool Prefix(TeleportWorld __instance)
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

        private static void Postfix(TeleportWorld __instance, long sender, string tag)
        {
            try
            {
                PostfixEvent?.Invoke(__instance, sender, tag);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
    }
}