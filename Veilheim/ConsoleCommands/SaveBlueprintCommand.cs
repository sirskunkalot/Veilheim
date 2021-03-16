// Veilheim
// a Valheim mod
// 
// File:    SaveBlueprintCommand.cs
// Project: Veilheim

using System.Linq;
using Veilheim.Blueprints;

namespace Veilheim.ConsoleCommands
{
    public class SaveBlueprintCommand : BaseConsoleCommand
    {
        public SaveBlueprintCommand()
        {
            CommandName = "SaveBlueprint";
            HelpText = "SaveBlueprint <radiusDelta> <name>";
        }


        public override bool ParseCommand(ref string input, bool silent = false)
        {
            var parts = input.Split(' ');
            if (parts.Length < 3)
            {
                Console.instance.AddString(HelpText);
                return false;
            }

            var radiusDelta = 10.0f;
            if (!float.TryParse(parts[1], out radiusDelta))
            {
                Console.instance.AddString("First parameter has to be a float");
                return false;
            }

            var name = string.Join(" ", parts.Skip(2).ToList());

            var blueprint = new Blueprint(name);

            if (!blueprint.Capture(radiusDelta))
            {
                return false;
            }

            if (!blueprint.Save())
            {
                return false;
            }

            blueprint.RecordFrame();
            return true;
        }
    }
}