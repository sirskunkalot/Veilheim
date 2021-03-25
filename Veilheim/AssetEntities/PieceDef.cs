// Veilheim
// a Valheim mod
// 
// File:    PieceDef.cs
// Project: Veilheim

using System.Collections.Generic;
using UnityEngine;

namespace Veilheim.AssetEntities
{
    /// <summary>
    ///     A wrapper class representing certain references to Valheim objects for a <see cref="Piece" />
    ///     as primitives. Must be instantiated for every <see cref="Piece" /> from an <see cref="AssetBundle" />
    ///     that you want to register. The actual objects are instantiated and referenced at runtime.
    /// </summary>
    internal class PieceDef
    {
        public string Name { get; set; }
        public string Description { get; set; } = string.Empty;
        public string CraftingStation { get; set; } = string.Empty;
        public string ExtendStation { get; set; } = string.Empty;
        public string PieceTable { get; set; } = string.Empty;
        public RequirementDef[] Resources { get; set; } = new RequirementDef[0];

        public Piece.Requirement[] GetRequirements()
        {
            Piece.Requirement[] reqs = new Piece.Requirement[Resources.Length];

            for (int i = 0; i < reqs.Length; i++)
            {
                reqs[i] = Resources[i].GetPieceRequirement();
            }

            return reqs;
        }
    }
}