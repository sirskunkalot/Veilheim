using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Veilheim.Configurations;

namespace Veilheim.Blueprints
{
    internal class Blueprint
    {
        public static string BlueprintPath {
            get 
            {
                var blueprintPath = Path.Combine(Configuration.ConfigIniPath, ZNet.instance.GetWorldUID().ToString(), "blueprints");
                if (!Directory.Exists(blueprintPath))
                {
                    Directory.CreateDirectory(blueprintPath);
                }
                return blueprintPath;
            }
        }

        public static Blueprint Current { get; private set; }

        public static readonly Dictionary<string, Blueprint> m_blueprints = new Dictionary<string, Blueprint>();

        private string m_name;

        public Blueprint(string name)
        {
            m_name = name;

            //TODO: blueprint known? load blueprints on init (global)?
        }

        public bool Capture(float radiusDelta)
        {

            var vec = Player.m_localPlayer.transform.position;
            var rot = Camera.main.transform.rotation.eulerAngles;
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
                    .Select(x => x.m_nview.GetZDO().m_position))
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

            // Relocate Z
            var minZ = 9999999.9f;
            var minX = 9999999.9f;
            var minY = 9999999.9f;

            foreach (var piece in collected.Where(x => x.IsPlacedByPlayer() && x.m_category != Piece.PieceCategory.Misc))
            {
                if (piece.m_nview.GetZDO().m_position.x < minX)
                {
                    minX = piece.m_nview.GetZDO().m_position.x;
                }

                if (piece.m_nview.GetZDO().m_position.z < minZ)
                {
                    minZ = piece.m_nview.GetZDO().m_position.z;
                }

                if (piece.m_nview.GetZDO().m_position.y < minY)
                {
                    minY = piece.m_nview.GetZDO().m_position.y;
                }
            }

            Console.instance.AddString($"{minX} - {minY} - {minZ}");

            var bottomleft = new Vector3(minX, minY, minZ);

            using (TextWriter tw = new StreamWriter(Path.Combine(BlueprintPath, m_name + ".blueprint")))
            {
                foreach (var piece in collected.Where(x => x.IsPlacedByPlayer() && x.m_category != Piece.PieceCategory.Misc)
                    .OrderBy(x => x.transform.position.y).ThenBy(x => x.transform.position.x).ThenBy(x => x.transform.position.z))
                {
                    var v1 = new Vector3(piece.m_nview.GetZDO().m_position.x - bottomleft.x, piece.m_nview.GetZDO().m_position.y - bottomleft.y,
                        piece.m_nview.GetZDO().m_position.z - bottomleft.z);

                    var q = piece.m_nview.GetZDO().m_rotation;
                    q.eulerAngles = new Vector3(0, q.eulerAngles.y, 0);
                    tw.WriteLine(string.Join(";", piece.name.Split('(')[0], piece.m_category.ToString(), v1.x.ToString("F5"), v1.y.ToString("F5"),
                        v1.z.ToString("F5"), q.x.ToString("F5"), q.y.ToString("F5"), q.z.ToString("F5"), q.w.ToString("F5"), q.eulerAngles.x.ToString("F5"), q.eulerAngles.y.ToString("F5"), q.eulerAngles.z.ToString("F5")));
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

                ScreenCapture.CaptureScreenshot(Path.Combine(BlueprintPath, m_name + ".png"));

                Hud.instance.m_userHidden = oldHud;
                Hud.instance.Update();
            });

            return true;
        }
    }
}
