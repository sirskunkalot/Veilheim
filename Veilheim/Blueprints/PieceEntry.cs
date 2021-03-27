// Veilheim
// a Valheim mod
// 
// File:    PieceEntry.cs
// Project: Veilheim

using UnityEngine;

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
}