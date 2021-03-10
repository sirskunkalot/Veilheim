using HarmonyLib;
using System;

namespace Veilheim.ConsoleCommands
{
    /// <summary>
    /// Route user input through our command instances. If 'help' is issued, amend own commands to the output first.
    /// </summary>
    [HarmonyPatch(typeof(Console), "InputText")]
    public static class Console_InputText_Patch
    {
        public static void Postfix()
        {
            string temp = Console.instance.m_input.text;

            // if help is issued, add list of our commands here
            if (string.Equals(temp.Trim(), "help", StringComparison.InvariantCultureIgnoreCase))
            {
                Console.instance.AddString("");
                Console.instance.AddString("Veilheim console commands:");
                foreach (var cmd in BaseConsoleCommand.consoleCommandInstances)
                {
                    Console.instance.AddString(cmd.HelpText);
                }
            }

            if (!BaseConsoleCommand.TryExecuteCommand(ref temp))
            {
                // Output something if command could not execute? not at this time.
            }
        }
    }

    /// <summary>
    /// Register new commands and RPC calls for that commands if needed
    /// </summary>
    [HarmonyPatch(typeof(Game), "Start")]
    public static class Game_Start_Patch
    {
        private static void Prefix()
        {
            // Register RPC calls
            ZRoutedRpc.instance.Register(
                nameof(SetConfigurationValue.RPC_SetConfigurationValue), 
                new Action<long, ZPackage>(SetConfigurationValue.RPC_SetConfigurationValue));

            // Register console commands
            BaseConsoleCommand.InitializeCommand<SetConfigurationValue>();
        }
    }
}