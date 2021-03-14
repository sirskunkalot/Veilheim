// Veilheim

using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.IO;
using System.Linq;
using UnityEngine;
using Veilheim.Blueprints;
using Veilheim.Configurations;

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