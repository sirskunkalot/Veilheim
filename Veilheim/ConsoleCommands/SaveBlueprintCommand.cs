// Veilheim

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Veilheim.Configurations;
using Object = UnityEngine.Object;

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

            var vec = Player.m_localPlayer.GetHeadPoint();
            Console.instance.AddString("Collecting piece information");

            var numPieces = 0;
            var numLastIteration = -1;
            var startRadius = 20.0f;
            var collected = new List<Piece>();
            var iteration = 0;
            while (numLastIteration < numPieces)
            {
                collected.Clear();
                Piece.GetAllPiecesInRadius(vec, startRadius, collected);
                numLastIteration = numPieces;
                numPieces = collected.Count(x => x.IsPlacedByPlayer() && x.m_category != Piece.PieceCategory.Misc);
                var newStart = new Vector3();
                foreach (var position in collected.Where(x => x.IsPlacedByPlayer() && x.m_category != Piece.PieceCategory.Misc)
                    .Select(x => x.transform.position))
                {
                    newStart.x += position.x;
                    newStart.y += position.y;
                    newStart.z += position.z;
                }

                newStart.x = newStart.x / (numPieces * 1f);
                newStart.y = newStart.y / (numPieces * 1f);
                newStart.z = newStart.z / (numPieces * 1f);
                vec = newStart;

                iteration++;
                Console.instance.AddString($"Iteration #{iteration} - found {numPieces} pieces in radius {startRadius}");
                startRadius += radiusDelta;
                Thread.Sleep(100);
            }

            Console.instance.AddString($"Found {numPieces} in a radius of {startRadius:F2}");

            var blueprintPath = Path.Combine(Configuration.ConfigIniPath, ZNet.instance.GetWorldUID().ToString(), "blueprints");
            if (!Directory.Exists(blueprintPath))
            {
                Directory.CreateDirectory(blueprintPath);
            }

            using (TextWriter tw = new StreamWriter(Path.Combine(blueprintPath, name + ".blueprint")))
            {
                var bottomleft = collected.Where(x => x.IsPlacedByPlayer() && x.m_category != Piece.PieceCategory.Misc).OrderBy(x => x.GetCenter().y)
                    .ThenBy(x => x.GetCenter().x).ThenBy(x => x.GetCenter().z).First().transform.position;
                foreach (var piece in collected.Where(x => x.IsPlacedByPlayer() && x.m_category != Piece.PieceCategory.Misc).OrderBy(x => x.GetCenter().y)
                    .ThenBy(x => x.GetCenter().x).ThenBy(x => x.GetCenter().z))
                {
                    var v1 = piece.GetCenter() - bottomleft;
                    var q = piece.m_nview.GetZDO().m_rotation;
                    tw.WriteLine(string.Join(";", piece.m_name, piece.m_category.ToString(), v1.x.ToString("F5"), v1.y.ToString("F5"), v1.z.ToString("F5"),
                        q.x.ToString("F5"), q.y.ToString("F5"), q.z.ToString("F5"), q.w.ToString("F5")));
                }
            }

            Task.Factory.StartNew(() =>
            {
                // Delay, to 'miss' the enter keyup event
                Thread.Sleep(200);

                Console.instance.m_chatWindow.gameObject.SetActive(false);
                Console.instance.Update();
                var oldHud = Hud.instance.m_userHidden;
                Hud.instance.m_userHidden = true;
                Hud.instance.SetVisible(false);
                Hud.instance.Update();
                Thread.Sleep(100);

                ScreenCapture.CaptureScreenshot(Path.Combine(blueprintPath,name+".png"));

                Hud.instance.m_userHidden = oldHud;
                Hud.instance.Update();
            });
            return true;
        }
    }
}