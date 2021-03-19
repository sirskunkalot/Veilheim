// Veilheim
// a Valheim mod
// 
// File:    Blueprint.cs
// Project: Veilheim

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Veilheim.Configurations;
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
            additionalInfo = parts[9];
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
        public string additionalInfo { get; set; }

        public Vector3 GetPosition()
        {
            return new Vector3(posX, posY, posZ);
        }

        public Quaternion GetRotation()
        {
            return new Quaternion(rotX, rotY, rotZ, rotW);
        }
    }

    internal class Blueprint
    {
        public static readonly Dictionary<string, Blueprint> m_blueprints = new Dictionary<string, Blueprint>();

        public static GameObject m_stub;

        public static float selectionRadius = 10.0f;

        /// <summary>
        ///     Name of the blueprint instance. Translates to &lt;m_name&gt;.blueprint in the filesystem
        /// </summary>
        private string m_name;

        /// <summary>
        ///     Array of the pieces this blueprint is made of
        /// </summary>
        internal PieceEntry[] m_pieceEntries;

        /// <summary>
        ///     Dynamically generated prefab for this blueprint
        /// </summary>
        private GameObject m_prefab;

        /// <summary>
        ///     Name of the generated prefab of the blueprint instance. Is always "piece_blueprint (&lt;m_name&gt;)"
        /// </summary>
        private string m_prefabname;

        /// <summary>
        ///     New "empty" Blueprint with a name but without any pieces. Call Capture() or Load() to add pieces to the blueprint.
        /// </summary>
        /// <param name="name"></param>
        public Blueprint(string name)
        {
            m_name = name;
            m_prefabname = $"piece_blueprint ({name})";
        }

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
        ///     Number of pieces currently stored in this blueprint
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
            Logger.LogDebug("Collecting piece information");

            var numPieces = 0;
            var collected = new List<Piece>();

            collected.Clear();

            foreach (var piece in Piece.m_allPieces)
            {
                if (Vector2.Distance(new Vector2(startPosition.x, startPosition.z), new Vector2(piece.transform.position.x, piece.transform.position.z)) <
                    startRadius)
                {
                    collected.Add(piece);
                    numPieces++;
                }
            }

            Logger.LogDebug($"Found {numPieces} in a radius of {startRadius:F2}");

            // Relocate Z
            var minZ = 9999999.9f;
            var minX = 9999999.9f;
            var minY = 9999999.9f;

            foreach (var piece in collected.Where(x => x.m_category != Piece.PieceCategory.Misc && x.IsPlacedByPlayer()))
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

            Logger.LogDebug($"{minX} - {minY} - {minZ}");

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

                var quat = piece.m_nview.GetZDO().m_rotation;
                quat.eulerAngles = new Vector3(0, quat.eulerAngles.y, 0);
                quat.eulerAngles = piece.transform.eulerAngles;

                var additionalInfo = piece.GetComponent<TextReceiver>() != null ? piece.GetComponent<TextReceiver>().GetText() : "";

                var line = string.Join(";", piece.name.Split('(')[0], piece.m_category.ToString(), v1.x.ToString("F5"), v1.y.ToString("F5"),
                    v1.z.ToString("F5"), quat.x.ToString("F5"), quat.y.ToString("F5"), quat.z.ToString("F5"), quat.w.ToString("F5"), additionalInfo);
                m_pieceEntries[i++] = new PieceEntry(line);
            }

            return true;
        }

        // Scale down a Texture2D
        public Texture2D ScaleTexture(Texture2D orig, int width, int height)
        {
            var result = new Texture2D(width, height);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var xp = 1f * x / width;
                    var yp = 1f * y / height;
                    var xo = (int) Mathf.Round(xp * orig.width); //Other X pos
                    var yo = (int) Mathf.Round(yp * orig.height); //Other Y pos
                    result.SetPixel(x, y, orig.GetPixel(xo, yo));
                }
            }

            result.Apply();
            return result;
        }

        // Save thumbnail
        public IEnumerator RecordFrame()
        {
            Console.instance.m_chatWindow.gameObject.SetActive(false);
            Console.instance.Update();
            var oldHud = Hud.instance.m_userHidden;
            Hud.instance.m_userHidden = true;
            Hud.instance.SetVisible(false);
            Hud.instance.Update();

            // Wait for end of frame (2x)
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            // Get a screenshot
            var screenShot = ScreenCapture.CaptureScreenshotAsTexture();

            // Calculate proper height
            var height = (int) Math.Round(160f * screenShot.height / screenShot.width);

            // Create thumbnail image from screenShot
            var tex = ScaleTexture(screenShot, 160, height);

            // Save to file
            File.WriteAllBytes(Path.Combine(GetBlueprintPath(), m_name + ".png"), tex.EncodeToPNG());

            // Destroy properly
            Object.Destroy(tex);
            Object.Destroy(screenShot);

            // Reset Hud to previous state
            Hud.instance.m_userHidden = oldHud;
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
            Logger.LogDebug($"{nulls} nulls found");
            if (nulls > 0)
            {
                return false;
            }

            foreach (var piece in pieces)
            {
                var gameobject = Create(tf, piece, prefabs, maxX, maxZ);

                var component = gameobject.GetComponent<Piece>();
                if (component)
                {
                    component.SetCreator(Player.m_localPlayer.GetPlayerID());
                }
            }

            return true;
        }

        public GameObject CreatePrefab()
        {
            if (m_prefab != null)
            {
                return m_prefab;
            }

            if (m_stub == null)
            {
                Logger.LogWarning("Stub not loaded");
                return null;
            }

            if (m_pieceEntries == null)
            {
                Logger.LogWarning("No pieces loaded");
                return null;
            }

            // Instantiate clone from stub
            m_prefab = Object.Instantiate(m_stub);
            m_prefab.name = m_prefabname;

            var piece = m_prefab.GetComponent<Piece>();
            if (File.Exists(Path.Combine(GetBlueprintPath(), m_name + ".png")))
            {
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(File.ReadAllBytes(Path.Combine(GetBlueprintPath(), m_name + ".png")));

                piece.m_icon = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
            }

            piece.m_name = m_name;
            piece.m_category = Piece.PieceCategory.Misc;

            // Instantiate child objects
            if (!GhostInstantiate(m_prefab))
            {
                Logger.LogWarning("Could not create prefab");
                Object.Destroy(m_prefab);
                return null;
            }

            // Add to known prefabs
            ZNetScene.instance.m_namedPrefabs.Add(m_prefabname.GetStableHashCode(), m_prefab);

            Logger.LogInfo($"Prefab {m_prefabname} created");

            return m_prefab;
        }

        public void AddToPieceTable()
        {
            if (m_prefab == null)
            {
                Logger.LogWarning("No prefab created");
                return;
            }

            var rune = ObjectDB.instance.GetItemPrefab("BlueprintRune");
            if (rune == null)
            {
                Logger.LogWarning("BlueprintRune prefab not found");
                return;
            }

            var table = rune.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces;
            if (table == null)
            {
                Logger.LogWarning("BlueprintPieceTable not found");
                return;
            }

            if (!table.m_pieces.Contains(m_prefab))
            {
                Logger.LogInfo($"Adding {m_prefabname} to BlueprintRune");

                table.m_pieces.Add(m_prefab);
            }
        }

        public void Destroy()
        {
            if (m_prefab == null)
            {
                return;
            }

            // Remove from PieceTable
            var rune = ObjectDB.instance.GetItemPrefab("BlueprintRune");
            if (rune == null)
            {
                Logger.LogWarning("BlueprintRune prefab not found");
                return;
            }

            var table = rune.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces;
            if (table == null)
            {
                Logger.LogWarning("BlueprintPieceTable not found");
                return;
            }

            if (table.m_pieces.Contains(m_prefab))
            {
                Logger.LogInfo($"Removing {m_prefabname} from BlueprintRune");

                table.m_pieces.Remove(m_prefab);
            }

            // Remove from prefabs
            if (ZNetScene.instance.m_namedPrefabs.ContainsKey(m_prefabname.GetStableHashCode()))
            {
                Logger.LogInfo($"Removing {m_prefabname} from ZNetScene");

                ZNetScene.instance.m_namedPrefabs.Remove(m_prefabname.GetStableHashCode());
            }

            // Destroy GameObject
            Logger.LogInfo($"Destroying {m_prefabname}");
            Object.DestroyImmediate(m_prefab);
        }

        private bool GhostInstantiate(GameObject baseObject)
        {
            var ret = true;
            ZNetView.m_ghostInit = true;

            try
            {
                var pieces = new List<PieceEntry>(m_pieceEntries);
                var maxX = pieces.Max(x => x.posX);
                var maxZ = pieces.Max(x => x.posZ);

                var tf = baseObject.transform;
                tf.rotation = Camera.main.transform.rotation;
                var quat = new Quaternion();
                quat.eulerAngles = new Vector3(0, tf.rotation.eulerAngles.y, 0);
                tf.SetPositionAndRotation(tf.position, quat);
                tf.position -= tf.right * (maxX / 2f);
                tf.position += tf.forward * 5f;

                var prefabs = new Dictionary<string, GameObject>();
                foreach (var piece in pieces.GroupBy(x => x.name).Select(x => x.FirstOrDefault()))
                {
                    var go = ZNetScene.instance.GetPrefab(piece.name);
                    go.transform.SetPositionAndRotation(go.transform.position, quat);
                    prefabs.Add(piece.name, go);
                }

                var nulls = prefabs.Values.Count(x => x == null);
                if (nulls > 0)
                {
                    throw new Exception($"{nulls} nulls found");
                }

                foreach (var piece in pieces)
                {
                    var child = Create(tf, piece, prefabs, maxX, maxZ);

                    child.transform.SetParent(baseObject.transform);
                    child.GetComponent<TextReceiver>()?.SetText(piece.additionalInfo);
                }

                baseObject.SetActive(true);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error while instantiating: {ex}");
                ret = false;
            }
            finally
            {
                ZNetView.m_ghostInit = false;
            }

            return ret;
        }

        private GameObject Create(Transform startPosition, PieceEntry piece, Dictionary<string, GameObject> prefabs, float maxX, float maxZ)
        {
            var pos = startPosition.position + startPosition.right * piece.GetPosition().x + startPosition.forward * piece.GetPosition().z +
                      new Vector3(0, piece.GetPosition().y, 0);

            var q = new Quaternion();
            q.eulerAngles = new Vector3(0, startPosition.transform.rotation.eulerAngles.y + piece.GetRotation().eulerAngles.y);

            var toBuild = Object.Instantiate(prefabs[piece.name], pos, q);

            //var component = toBuild.GetComponent<Piece>();
            /*if (component && Player.m_localPlayer != null)
            {
                component.SetCreator(Player.m_localPlayer.GetPlayerID());
            }*/
            /*if (component)
            {
                component.SetCreator(Game.instance.GetPlayerProfile().GetPlayerID());
            }*/

            return toBuild;
        }


        public Vector2 GetExtent()
        {
            return new Vector2(m_pieceEntries.Max(x => x.posX), m_pieceEntries.Max(x => x.posZ));
        }

        /// <summary>
        ///     Helper class for naming and saving a captured blueprint via GUI
        ///     Implements the Interface <see cref="TextReceiver" />. SetText is called from <see cref="TextInput" /> upon entering
        ///     an name for the blueprint.<br />
        ///     Save the actual blueprint and add it to the list of known blueprints.
        /// </summary>
        internal class BlueprintSaveGUI : TextReceiver
        {
            private Blueprint newbp;

            public BlueprintSaveGUI(Blueprint bp)
            {
                newbp = bp;
            }

            public string GetText()
            {
                return newbp.m_name;
            }

            public void SetText(string text)
            {
                newbp.m_name = text;
                newbp.m_prefabname = $"piece_blueprint ({newbp.m_name})";
                if (newbp.Save())
                {
                    if (m_blueprints.ContainsKey(newbp.m_name))
                    {
                        Blueprint oldbp;
                        m_blueprints.TryGetValue(newbp.m_name, out oldbp);
                        oldbp.Destroy();
                        m_blueprints.Remove(newbp.m_name);
                    }

                    VeilheimPlugin.instance.StartCoroutine(newbp.RecordFrame());
                    newbp.CreatePrefab();
                    newbp.AddToPieceTable();
                    Player.m_localPlayer.UpdateKnownRecipesList();
                    Player.m_localPlayer.UpdateAvailablePiecesList();
                    m_blueprints.Add(newbp.m_name, newbp);

                    Logger.LogInfo("Blueprint created");
                }

                newbp = null;
            }
        }
    }
}