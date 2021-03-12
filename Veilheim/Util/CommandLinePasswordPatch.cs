using System;
using System.Linq;
using HarmonyLib;

namespace Veilheim.Util
{

    // Adding +password <password> to the commandline works now as intended
    // Password is checked against server, but no dialog is shown
    [HarmonyPatch(typeof(ZNet), "RPC_ClientHandshake", new Type[] { typeof(ZRpc), typeof(bool) })]
    public static class CommandLinePasswordPatch
    {
        public static bool Prefix(ZNet __instance, ZRpc rpc, bool needPassword)
        {
            if (Environment.GetCommandLineArgs().Any(x => x.ToLower() == "+password"))
            {
                string[] args = Environment.GetCommandLineArgs();
                
                // find password argument index
                int index = 0;
                while (index < args.Length && args[index].ToLower() != "+password")
                {
                    index++;
                }

                index++;

                // is there a password after +password?
                if (index >= args.Length)
                {
                    // Not enough commandline arguments
                    return true;
                }

                // do normal handshake
                __instance.m_connectingDialog.gameObject.SetActive(false);
                __instance.SendPeerInfo(rpc, args[index]);

                // prevent execution of original code 
                return false;
            }

            return true;
        }

    }
}