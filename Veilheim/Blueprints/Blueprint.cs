﻿// Veilheim
// a Valheim mod
// 
// File:    Blueprint.cs
// Project: Veilheim

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Veilheim.AssetUtils;
using Veilheim.Configurations;
using Veilheim.PatchEvents;
using Object = UnityEngine.Object;

namespace Veilheim.Blueprints
{
    internal class PieceEntry
    {
        public PieceEntry(string _line)
        {
            line = _line;
            var parts = line.Split(';');
            name = parts[0].TrimStart('$');
            posX = float.Parse(parts[2]);
            posY = float.Parse(parts[3]);
            posZ = float.Parse(parts[4]);
            rotX = float.Parse(parts[5]);
            rotY = float.Parse(parts[6]);
            rotZ = float.Parse(parts[7]);
            rotW = float.Parse(parts[8]);
        }

        public string line { get; set; }
        public string name { get; set; }
        public float posX { get; set; }
        public float posY { get; set; }
        public float posZ { get; set; }
        public float rotX { get; set; }
        public float rotY { get; set; }
        public float rotZ { get; set; }
        public float rotW { get; set; }

        public Vector3 GetPosition()
        {
            return new Vector3(posX, posY, posZ);
        }

        public Quaternion GetRotation()
        {
            return new Quaternion(rotX, rotY, rotZ, rotW);
        }
    }

    internal class Blueprint : PatchEventConsumer
    {
        private static readonly Dictionary<string, Blueprint> m_blueprints = new Dictionary<string, Blueprint>();

        public static string GetBlueprintPath()
        {
            //TODO: save per profile or world or global?
            var path = Path.Combine(Configuration.ConfigIniPath, "blueprints");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

        /// <summary>
        /// Name of the blueprint instance. Translates to &lt;m_name&gt;.blueprint in the filesystem
        /// </summary>
        private string m_name;

        /// <summary>
        /// Array of the pieces this blueprint is made of
        /// </summary>
        private PieceEntry[] m_pieceEntries;

        /// <summary>
        /// New "empty" Blueprint with a name but without any pieces. Call Capture() or Load() to add pieces to the blueprint.
        /// </summary>
        /// <param name="name"></param>
        public Blueprint(string name)
        {
            m_name = name;
        }

        /// <summary>
        /// Number of pieces currently stored in this blueprint
        /// </summary>
        /// <returns></returns>
        public int GetPieceCount()
        {
            return m_pieceEntries.Count();
        }

        public bool Capture(Vector3 startPosition, float startRadius, float radiusDelta)
        {
            var vec = startPosition;
            var rot = Camera.main.transform.rotation.eulerAngles;
            Console.instance.AddString("Collecting piece information");

            var numPieces = 0;
            var numLastIteration = -1;
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

            // select and order instance piece entries
            var pieces = collected.Where(x => x.IsPlacedByPlayer() && x.m_category != Piece.PieceCategory.Misc).OrderBy(x => x.transform.position.y)
                .ThenBy(x => x.transform.position.x).ThenBy(x => x.transform.position.z);

            if (m_pieceEntries == null)
            {
                m_pieceEntries = new PieceEntry[pieces.Count()];
            }
            else if (m_pieceEntries.Length > 0)
            {
                Array.Clear(m_pieceEntries, 0, m_pieceEntries.Length - 1);
                Array.Resize(ref m_pieceEntries, pieces.Count());
            }

            uint i = 0;
            foreach (var piece in pieces)
            {
                var v1 = new Vector3(piece.m_nview.GetZDO().m_position.x - bottomleft.x, piece.m_nview.GetZDO().m_position.y - bottomleft.y,
                    piece.m_nview.GetZDO().m_position.z - bottomleft.z);

                var q = piece.m_nview.GetZDO().m_rotation;
                q.eulerAngles = new Vector3(0, q.eulerAngles.y, 0);

                var line = string.Join(";", piece.name.Split('(')[0], piece.m_category.ToString(), v1.x.ToString("F5"), v1.y.ToString("F5"),
                    v1.z.ToString("F5"), q.x.ToString("F5"), q.y.ToString("F5"), q.z.ToString("F5"), q.w.ToString("F5"), q.eulerAngles.x.ToString("F5"),
                    q.eulerAngles.y.ToString("F5"), q.eulerAngles.z.ToString("F5"));
                m_pieceEntries[i++] = new PieceEntry(line);
            }

            return true;
        }

        public void RecordFrame()
        {
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

                ScreenCapture.CaptureScreenshot(Path.Combine(GetBlueprintPath(), m_name + ".png"));

                Hud.instance.m_userHidden = oldHud;
            });
        }

        public bool Save()
        {
            var path = GetBlueprintPath();

            if (m_pieceEntries == null)
            {
                Logger.LogWarning("No pieces stored to save");
            }
            else
            {
                using (TextWriter tw = new StreamWriter(Path.Combine(path, m_name + ".blueprint")))
                {
                    foreach (var piece in m_pieceEntries)
                    {
                        tw.WriteLine(piece.line);
                    }

                    Logger.LogDebug("Wrote " + m_pieceEntries.Length + " pieces to " + Path.Combine(path, m_name + ".blueprint"));
                }
            }

            return true;
        }

        public bool Load()
        {
            var path = GetBlueprintPath();
            var lines = File.ReadAllLines(Path.Combine(path, m_name + ".blueprint")).ToList();
            Logger.LogDebug("read " + lines.Count + " pieces from " + Path.Combine(path, m_name + ".blueprint"));

            if (m_pieceEntries == null)
            {
                m_pieceEntries = new PieceEntry[lines.Count()];
            }
            else if (m_pieceEntries.Length > 0)
            {
                Array.Clear(m_pieceEntries, 0, m_pieceEntries.Length - 1);
                Array.Resize(ref m_pieceEntries, lines.Count());
            }

            uint i = 0;
            foreach (var line in lines)
            {
                m_pieceEntries[i++] = new PieceEntry(line);
            }

            return true;
        }

        public bool Instantiate()
        {
            var pieces = new List<PieceEntry>(m_pieceEntries);
            var maxX = pieces.Max(x => x.posX);
            var maxZ = pieces.Max(x => x.posZ);

            var startPosition = Player.m_localPlayer.GetTransform();
            var tf = startPosition;
            tf.rotation = Camera.main.transform.rotation;
            var q = new Quaternion();
            q.eulerAngles = new Vector3(0, tf.rotation.eulerAngles.y, 0);
            tf.SetPositionAndRotation(tf.position, q);
            tf.position -= tf.right * (maxX / 2f);
            tf.position += tf.forward * 5f;

            FlattenTerrain.Flatten(tf, new Vector2(maxX, maxZ), pieces);

            var prefabs = new Dictionary<string, GameObject>();
            foreach (var piece in pieces.GroupBy(x => x.name).Select(x => x.FirstOrDefault()))
            {
                var go = ZNetScene.instance.GetPrefab(piece.name);
                go.transform.SetPositionAndRotation(go.transform.position, q);
                prefabs.Add(piece.name, go);
            }

            var nulls = prefabs.Values.Count(x => x == null);
            Console.instance.AddString($"{nulls} nulls found");
            if (nulls > 0)
            {
                return false;
            }

            foreach (var piece in pieces)
            {
                Create(tf, piece, prefabs, maxX, maxZ);
            }

            return true;
        }

        public bool GhostInstantiate(GameObject baseObject)
        {
            try
            {

                ZNetView.m_ghostInit = true;

                var pieces = new List<PieceEntry>(m_pieceEntries);
                var maxX = pieces.Max(x => x.posX);
                var maxZ = pieces.Max(x => x.posZ);

                var tf = baseObject.transform;
                tf.rotation = Camera.main.transform.rotation;
                var q = new Quaternion();
                q.eulerAngles = new Vector3(0, tf.rotation.eulerAngles.y, 0);
                tf.SetPositionAndRotation(tf.position, q);
                tf.position -= tf.right * (maxX / 2f);
                tf.position += tf.forward * 5f;

                var prefabs = new Dictionary<string, GameObject>();
                foreach (var piece in pieces.GroupBy(x => x.name).Select(x => x.FirstOrDefault()))
                {
                    var go = ZNetScene.instance.GetPrefab(piece.name);
                    go.transform.SetPositionAndRotation(go.transform.position, q);
                    prefabs.Add(piece.name, go);
                }

                var nulls = prefabs.Values.Count(x => x == null);
                Logger.LogWarning($"{nulls} nulls found");
                if (nulls > 0)
                {
                    return false;
                }

                foreach (var piece in pieces)
                {
                    var child = Create(tf, piece, prefabs, maxX, maxZ);

                    child.transform.SetParent(baseObject.transform);
                }

                baseObject.SetActive(true);

                ZNetView.m_ghostInit = false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error while instantiating: {ex}");
                return false;
            }

            return true;
        }

        private GameObject Create(Transform startPosition, PieceEntry piece, Dictionary<string, GameObject> prefabs, float maxX, float maxZ)
        {
            var pos = startPosition.position + startPosition.right * piece.GetPosition().x + startPosition.forward * piece.GetPosition().z +
                      new Vector3(0, piece.GetPosition().y, 0);

            var q = new Quaternion();
            q.eulerAngles = new Vector3(0, startPosition.transform.rotation.eulerAngles.y + piece.GetRotation().eulerAngles.y);

            var toBuild = Object.Instantiate(prefabs[piece.name], pos, q);

            var component = toBuild.GetComponent<Piece>();
            if (component && Player.m_localPlayer != null)
            {
                component.SetCreator(Player.m_localPlayer.GetPlayerID());
            }

            return toBuild;
        }


        [PatchEvent(typeof(ZNet), nameof(ZNet.Awake), PatchEventType.Postfix)]
        public static void LoadKnownBlueprints(ZNet instance)
        {
            // Client only
            if (!instance.IsServerInstance())
            {
                Logger.LogMessage("Loading known blueprints");

                // Try to load all saved blueprints
                foreach (var name in Directory.EnumerateFiles(GetBlueprintPath(), "*.blueprint").Select(Path.GetFileNameWithoutExtension))
                {
                    if (!m_blueprints.ContainsKey(name))
                    {
                        var bp = new Blueprint(name);
                        if (bp.Load())
                        {
                            m_blueprints.Add(name, bp);
                        }
                        else
                        {
                            Logger.LogWarning($"Could not load blueprint {name}");
                        }
                    }
                }

                Logger.LogMessage("Known blueprints loaded");
            }
        }

        [PatchEvent(typeof(ZNetScene), nameof(ZNetScene.Awake), PatchEventType.Postfix)]
        public static void RegisterKnownBlueprints(ZNetScene instance)
        {
            // Client only
            if (!ZNet.instance.IsServerInstance())
            {
                Logger.LogMessage("Registering known blueprints");

                // Get prefab stub from bundle
                var assetBundle = AssetLoader.LoadAssetBundleFromResources("blueprintrune");
                var stub = assetBundle.LoadAsset<GameObject>("piece_blueprint");

                // Instantiate from stub for all known blueprints
                foreach (var bp in m_blueprints)
                {
                    Logger.LogDebug($"{bp.Key}.blueprint");

                    var piecename = $"piece_blueprint ({bp.Key})";

                    // Instantiate clone from stub
                    var go = UnityEngine.Object.Instantiate<GameObject>(stub);

                    go.name = piecename;
                    go.GetComponent<Piece>().name = piecename;
                    go.GetComponent<Piece>().m_name = bp.Key;

                    // Save way without children / ghost
                    /*ZNetView.m_ghostInit = true;
                    go.SetActive(true);
                    ZNetView.m_ghostInit = false;
                    
                    AssetManager.RegisterPiecePrefab(go, new PieceDef { PieceTable = "_BlueprintPieceTable" }); */

                    // Instantiate child objects
                    if (!bp.Value.GhostInstantiate(go))
                    {
                        Logger.LogWarning("Could not instantiate blueprint");
                    }
                    else
                    {
                        // Register instance with AssetManager
                        AssetManager.RegisterPiecePrefab(go, new PieceDef { PieceTable = "_BlueprintPieceTable" }); 
                    }
                }

                assetBundle.Unload(false);

                Logger.LogMessage("Known blueprints registered");
            }
        }

        /// <summary>
        ///     React to a placement of blueprints
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="piece"></param>
        /// <param name="successful"></param>
        [PatchEvent(typeof(Player), nameof(Player.PlacePiece), PatchEventType.Postfix)]
        public static void AfterPlacingBlueprint(Player instance, Piece piece, bool successful)
        {
            if (ZNet.instance.IsServerInstance())
            {
                return;
            }

            // Capture a new blueprint
            if (successful && !piece.IsCreator() && piece.m_name == "$piece_make_blueprint")
            {
                string bpname = "blueprint" + String.Format("{0:000}", m_blueprints.Count() + 1);
                Logger.LogInfo($"Capturing blueprint {bpname}");

                if (Player.m_localPlayer.m_hoveringPiece != null)
                {
                    var bp = new Blueprint(bpname);
                    if (bp.Capture(Player.m_localPlayer.m_hoveringPiece.transform.position, 2.0f, 1.0f))
                    {
                        TextInput.instance.m_queuedSign = new BlueprintSaveGUI(bp);
                        TextInput.instance.Show($"Save Blueprint ({bp.GetPieceCount()} pieces captured)", bpname, 50);
                    }
                    else
                    {
                        Logger.LogWarning($"Could not capture blueprint {bpname}");
                    }
                }
                else
                {
                    Logger.LogInfo("Not hovering any piece");
                }
            }
        }

        /// <summary>
        /// Helper class for naming and saving a captured blueprint via GUI
        /// 
        /// Implements the Interface <see cref="TextReceiver"/>. SetText is called from <see cref="TextInput"/> upon entering an name for the blueprint.<br />
        /// Save the actual blueprint and add it to the list of known blueprints.
        /// </summary>
        private class BlueprintSaveGUI : TextReceiver
        {
            private readonly Blueprint bp;

            public BlueprintSaveGUI(Blueprint bp)
            {
                this.bp = bp;
            }

            public string GetText()
            {
                return bp.m_name;
            }

            public void SetText(string text)
            {
                bp.m_name = text;
                if (bp.Save())
                {
                    if (m_blueprints.ContainsKey(bp.m_name))
                    {
                        m_blueprints.Remove(bp.m_name);
                    }
                    m_blueprints.Add(bp.m_name, bp);
                    bp.RecordFrame();

                    Logger.LogInfo("Blueprint saved");
                }
            }

        }
    }
}