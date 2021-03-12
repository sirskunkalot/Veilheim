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
    public class PieceEntry
    {
        public PieceEntry(string line)
        {
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

            var blueprintPath = Path.Combine(Configuration.ConfigIniPath, ZNet.instance.GetWorldUID().ToString(), "blueprints");

            var filename = string.Join(" ", parts.Skip(1));

            var lines = File.ReadAllLines(Path.Combine(blueprintPath, filename + ".blueprint")).ToList();
            Debug.Log("Anzahl zeilen: " + lines.Count + " von " + Path.Combine(blueprintPath, filename + ".blueprint"));

            var pieces = new List<PieceEntry>();
            foreach (var line in lines)
            {
                pieces.Add(new PieceEntry(line));
            }

            var maxX = pieces.Max(x => x.posX);
            var maxZ = pieces.Max(x => x.posZ);

            var startPosition = Player.m_localPlayer.GetTransform();
            Transform tf = startPosition;
            tf.rotation = Camera.main.transform.rotation;
            Quaternion q = new Quaternion();
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
                return true;
            }


            foreach (var piece in pieces)
            {
                Create(tf, piece, prefabs, maxX, maxZ);
            }

            return true;
        }

        private GameObject Create(Transform startPosition, PieceEntry piece, Dictionary<string, GameObject> prefabs, float maxX, float maxZ)
        {
            var pos = startPosition.position + startPosition.right * piece.GetPosition().x + startPosition.forward * piece.GetPosition().z +
                      new Vector3(0, piece.GetPosition().y, 0);

            Quaternion q = new Quaternion();
            q.eulerAngles = new Vector3(0, startPosition.transform.rotation.eulerAngles.y + piece.GetRotation().eulerAngles.y);

            var toBuild = Object.Instantiate(prefabs[piece.name], pos, q);

            var component = toBuild.GetComponent<Piece>();
            if (component)
            {
                component.SetCreator(Player.m_localPlayer.GetPlayerID());
            }

            return toBuild;
        }
    }
}