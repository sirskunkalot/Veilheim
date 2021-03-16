// Veilheim
// a Valheim mod
// 
// File:    Player_Patches.cs
// Project: Veilheim

using System;
using HarmonyLib;

namespace Veilheim.PatchEvents.PatchStubs
{
    /// <summary>
    ///     Patch Player.PlacePiece
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece), typeof(Piece))]
    public class Player_PlacePiece_Patch
    {
        public delegate void BlockingPrefixHandler(Player instance, ref bool cancel);

        public delegate void PostfixHandler(Player instance, Piece piece, bool successful);

        public delegate void PrefixHandler(Player instance);

        public static event PrefixHandler PrefixEvent;

        public static event BlockingPrefixHandler BlockingPrefixEvent;

        public static event PostfixHandler PostfixEvent;


        private static bool Prefix(Player __instance)
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

        private static void Postfix(Player __instance, Piece piece, ref bool __result)
        {
            try
            {
                PostfixEvent?.Invoke(__instance, piece, __result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
    }
}