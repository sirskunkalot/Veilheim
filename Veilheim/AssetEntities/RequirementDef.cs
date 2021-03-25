// Veilheim
// a Valheim mod
// 
// File:    RequirementDef.cs
// Project: Veilheim

using Veilheim.AssetManagers;

namespace Veilheim.AssetEntities
{
    /// <summary>
    ///     A wrapper class representing <see cref="Piece.Requirement" />s as primitives.
    ///     Valheim objects are instantiated and referenced at runtime.
    /// </summary>
    internal class RequirementDef
    {
        public string Item { get; set; }
        public int Amount { get; set; }
        public int AmountPerLevel { get; set; }
        public bool Recover { get; set; }

        public Piece.Requirement GetPieceRequirement()
        {
            return new Piece.Requirement()
            {
                m_resItem = PrefabManager.Instance.GetPrefab(Item).GetComponent<ItemDrop>(),
                m_amount = Amount,
                m_amountPerLevel = AmountPerLevel,
                m_recover = Recover
            };
        }
    }
}