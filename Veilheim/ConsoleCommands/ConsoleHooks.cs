// Veilheim

using System;
using System.Linq;
using System.Reflection;
using Veilheim.PatchEvents;

namespace Veilheim.ConsoleCommands
{
    public class ConsoleCommandPatches : Payload
    {
        /// <summary>
        /// Route user input through our command instances. If 'help' is issued, amend own commands to the output first.
        /// </summary>
        /// <param name="instance"></param>
        [PatchEvent(typeof(Console), nameof(Console.InputText), PatchEventType.Postfix)]
        public static void InjectCommandExecution(Console instance)
        {
            var temp = instance.m_input.text;

            // if help is issued, add list of our commands here
            if (string.Equals(temp.Trim(), "help", StringComparison.InvariantCultureIgnoreCase))
            {
                instance.AddString("");
                instance.AddString("Veilheim console commands:");
                foreach (var cmd in BaseConsoleCommand.consoleCommandInstances)
                {
                    instance.AddString(cmd.HelpText);
                }
            }

            if (!BaseConsoleCommand.TryExecuteCommand(ref temp))
            {
                // Output something if command could not execute? not at this time.
            }
        }

        /// <summary>
        /// Register new commands and RPC calls for that commands if needed
        /// </summary>
        /// <param name="instance"></param>
        [PatchEvent(typeof(Game), nameof(Game.Start), PatchEventType.Prefix)]
        public static void InitializeConsoleCommands(Game instance)
        {
            // Register RPC calls
            ZRoutedRpc.instance.Register(nameof(SetConfigurationValue.RPC_SetConfigurationValue),
                new Action<long, ZPackage>(SetConfigurationValue.RPC_SetConfigurationValue));

            // Register console commands

            // Get InitializeCommand method
            var initializeMethod =
                typeof(BaseConsoleCommand).GetMethod(nameof(BaseConsoleCommand.InitializeCommand), BindingFlags.Public | BindingFlags.Static);

            foreach (var type in typeof(VeilheimPlugin).Assembly.GetTypes().Where(x => x.BaseType == typeof(BaseConsoleCommand)))
            {
                // Activate each console command
                var generic = initializeMethod.MakeGenericMethod(type);
                generic.Invoke(null, null);
            }
        }
    }
}