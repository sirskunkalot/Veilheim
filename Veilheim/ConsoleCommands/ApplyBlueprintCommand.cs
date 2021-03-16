// Veilheim
// a Valheim mod
// 
// File:    ApplyBlueprintCommand.cs
// Project: Veilheim

using System.Linq;
using Veilheim.Blueprints;

namespace Veilheim.ConsoleCommands
{
    public class ApplyBlueprintCommand : BaseConsoleCommand
    {
        public ApplyBlueprintCommand()
        {
            CommandName = "ApplyBlueprint";
            HelpText = "ApplyBlueprint <name>";
        }

        public override bool ParseCommand(ref string input, bool silent = false)
        {
            var parts = input.Split(' ');

            var name = string.Join(" ", parts.Skip(1));

            var blueprint = new Blueprint(name);

            if (!blueprint.Load())
            {
                return false;
            }

            if (!blueprint.Instantiate())
            {
                return false;
            }

            return true;
        }
    }
}