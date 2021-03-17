// Veilheim
// a Valheim mod
// 
// File:    ZNet_Patches.cs
// Project: Veilheim

using System;
using HarmonyLib;

namespace Veilheim.PatchEvents.PatchStubs
{
    /// <summary>
    ///     Patch ZNet.Awake
    /// </summary>
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
    public class ZNet_Awake_Patch
    {
        public delegate void BlockingPrefixHandler(ZNet instance, ref bool cancel);

        public delegate void PostfixHandler(ZNet instance);

        public delegate void PrefixHandler(ZNet instance);

        public static event PrefixHandler PrefixEvent;

        public static event BlockingPrefixHandler BlockingPrefixEvent;

        public static event PostfixHandler PostfixEvent;


        private static bool Prefix(ZNet __instance)
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

        private static void Postfix(ZNet __instance)
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
    ///     Patch ZNet.OnDestroy
    /// </summary>
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Shutdown))]
    public class ZNet_OnDestroy_Patch
    {
        public delegate void BlockingPrefixHandler(ZNet instance, ref bool cancel);

        public delegate void PostfixHandler(ZNet instance);

        public delegate void PrefixHandler(ZNet instance);

        public static event PrefixHandler PrefixEvent;

        public static event BlockingPrefixHandler BlockingPrefixEvent;

        public static event PostfixHandler PostfixEvent;


        private static bool Prefix(ZNet __instance)
        {
            Logger.LogInfo($"{__instance} despawns.");

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

        private static void Postfix(ZNet __instance)
        {
            Logger.LogInfo($"{__instance} destroyed.");

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
    ///     Patch ZNet.RPC_Save
    /// </summary>
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_Save))]
    public class ZNet_RPC_Save_Patch
    {
        public delegate void BlockingPrefixHandler(ZNet instance, ref bool cancel);

        public delegate void PostfixHandler(ZNet instance);

        public delegate void PrefixHandler(ZNet instance);

        public static event PrefixHandler PrefixEvent;

        public static event BlockingPrefixHandler BlockingPrefixEvent;

        public static event PostfixHandler PostfixEvent;


        private static bool Prefix(ZNet __instance)
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

        private static void Postfix(ZNet __instance)
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
    ///     Patch ZNet.RPC_PeerInfo
    /// </summary>
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_PeerInfo))]
    public class ZNet_RPC_PeerInfo_Patch
    {
        public delegate void BlockingPrefixHandler(ZNet instance, ref bool cancel);

        public delegate void PostfixHandler(ZNet instance);

        public delegate void PrefixHandler(ZNet instance);

        public static event PrefixHandler PrefixEvent;

        public static event BlockingPrefixHandler BlockingPrefixEvent;

        public static event PostfixHandler PostfixEvent;


        private static bool Prefix(ZNet __instance)
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

        private static void Postfix(ZNet __instance)
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
    ///     Patch ZNet.RPC_ClientHandshake
    /// </summary>
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_ClientHandshake), typeof(ZRpc), typeof(bool))]
    public class ZNet_RPC_ClientHandshake_Patch
    {
        public delegate void BlockingPrefixHandler(ZNet instance, ref bool cancel, ZRpc rpc, bool needPassword);

        public delegate void PostfixHandler(ZNet instance);

        public delegate void PrefixHandler(ZNet instance);

        public static event PrefixHandler PrefixEvent;

        public static event BlockingPrefixHandler BlockingPrefixEvent;

        public static event PostfixHandler PostfixEvent;


        private static bool Prefix(ZNet __instance, ZRpc rpc, bool needPassword)
        {
            var cancel = false;
            BlockingPrefixEvent?.Invoke(__instance, ref cancel, rpc, needPassword);

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

        private static void Postfix(ZNet __instance)
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
    ///     Patch ZNet.SetPublicReferencePosition
    /// </summary>
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.SetPublicReferencePosition))]
    public class ZNet_SetPublicReferencePosition_Patch
    {
        public delegate void BlockingPrefixHandler(ZNet instance, ref bool cancel);

        public delegate void PostfixHandler(ZNet instance);

        public delegate void PrefixHandler(ZNet instance);

        public static event PrefixHandler PrefixEvent;

        public static event BlockingPrefixHandler BlockingPrefixEvent;

        public static event PostfixHandler PostfixEvent;


        private static bool Prefix(ZNet __instance)
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

        private static void Postfix(ZNet __instance)
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