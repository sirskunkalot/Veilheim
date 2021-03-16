// Veilheim
// a Valheim mod
// 
// File:    CommandLinePasswordPatch.cs
// Project: Veilheim

using System;
using System.Linq;
using Veilheim.PatchEvents;

namespace Veilheim.Util
{
    public class CommandLinePassword : PatchEventConsumer
    {
        /// <summary>
        ///     Adding +password «password» to the commandline works now as intended
        ///     Password is checked against server, but no dialog is shown
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="cancel"></param>
        /// <param name="rpc"></param>
        /// <param name="needPassword"></param>
        [PatchEvent(typeof(ZNet), nameof(ZNet.RPC_ClientHandshake), PatchEventType.BlockingPrefix)]
        public static void ApplyPassword(ZNet instance, ref bool cancel, ZRpc rpc, bool needPassword)
        {
            // return if previous blocking prefix event was cancelling
            if (cancel)
            {
                return;
            }

            if (Environment.GetCommandLineArgs().Any(x => x.ToLower() == "+password"))
            {
                var args = Environment.GetCommandLineArgs();

                // find password argument index
                var index = 0;
                while (index < args.Length && args[index].ToLower() != "+password")
                {
                    index++;
                }

                index++;

                // is there a password after +password?
                if (index >= args.Length)
                {
                    // Not enough commandline arguments
                    return;
                }

                // do normal handshake
                instance.m_connectingDialog.gameObject.SetActive(false);
                instance.SendPeerInfo(rpc, args[index]);

                // prevent execution of original code 
                cancel = true;
            }
        }
    }
}