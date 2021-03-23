// Veilheim
// a Valheim mod
// 
// File:    PieceManager.cs
// Project: Veilheim

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Veilheim.AssetEntities;
using Veilheim.PatchEvents;

namespace Veilheim.AssetManagers
{
    internal class PieceManager : AssetManager, IPatchEventConsumer
    {
        internal static PieceManager Instance { get; private set; }

        private readonly Dictionary<GameObject, PieceDef> Pieces = new Dictionary<GameObject, PieceDef>();

        private void Awake()
        {
            if (Instance != null)
            {
                Logger.LogError($"Two instances of singleton {GetType()}");
                return;
            }

            Instance = this;
        }

        internal override void Init()
        {
            Logger.LogInfo("Initialized PieceManager");
        }

        /// <summary>
        ///     Update Recipes and Pieces for the local Player if custom prefabs were added.<br />
        ///     Has a low priority (1000), so other hooks can register their prefabs before they get added to the player.
        /// </summary>
        /// <param name="instance"></param>
        [PatchEvent(typeof(Player), nameof(Player.OnSpawned), PatchEventType.Postfix, 1000)]
        public static void UpdateKnownRecipes(Player instance)
        {
            if (Player.m_localPlayer == null)
            {
                return;
            }

            if (Instance.Pieces.Count > 0)
            {
                Logger.LogMessage($"Updating known pieces for Player {Player.m_localPlayer.GetPlayerName()}");

                Player.m_localPlayer.UpdateAvailablePiecesList();
            }
        }

        /// <summary>
        ///     Register our custom building pieces to their respective ingame items or stations
        /// </summary>
        /*private void RegisterPieces(ObjectDB instance)
        {
            // Go through all registered Pieces and try to obtain references
            // to the actual objects defined as strings in PieceDef
            foreach (var entry in Pieces)
            {
                Logger.LogDebug($"GameObject: {entry.Key.name}");

                var prefab = entry.Key;
                var pieceDef = entry.Value;

                var piece = prefab.GetComponent<Piece>();

                if (piece == null)
                {
                    Logger.LogError("GameObject has no Piece attached");
                    continue;
                }

                if (pieceDef == null)
                {
                    Logger.LogError("No PieceDef available");
                    continue;
                }

                // Assign the piece to the actual PieceTable if not already in there
                var pieceTable = PieceTables.GetValueSafe(pieceDef.PieceTable);
                if (pieceTable == null)
                {
                    Logger.LogWarning($"Could not find piecetable: {pieceDef.PieceTable}");
                    continue;
                }

                if (pieceTable.m_pieces.Contains(prefab))
                {
                    Logger.LogDebug($"Piece already added to PieceTable {pieceDef.PieceTable}");
                    continue;
                }

                pieceTable.m_pieces.Add(prefab);

                // Assign the CraftingStation for this piece, if needed
                if (!string.IsNullOrEmpty(pieceDef.CraftingStation))
                {
                    var pieceStation = CraftingStations.GetValueSafe(pieceDef.CraftingStation);
                    if (pieceStation == null)
                    {
                        Logger.LogWarning($"Could not find crafting station: {pieceDef.CraftingStation}");
                        var stationList = string.Join(", ", CraftingStations.Keys);
                        Logger.LogDebug($"Available Stations: {stationList}");
                    }
                    else
                    {
                        piece.m_craftingStation = pieceStation;
                    }
                }

                // Assign all needed resources for this piece
                var resources = new List<Piece.Requirement>();
                foreach (var resource in pieceDef.Resources)
                {
                    var resourcePrefab = instance.GetItemPrefab(resource.Item);
                    if (resourcePrefab == null)
                    {
                        Logger.LogError($"Could not load requirement item: {resource.Item}");
                        continue;
                    }

                    resources.Add(new Piece.Requirement { m_resItem = resourcePrefab.GetComponent<ItemDrop>(), m_amount = resource.Amount });
                }

                piece.m_resources = resources.ToArray();

                // Try to assign the effect prefabs of another extension defined in ExtendStation
                var stationExt = prefab.GetComponent<StationExtension>();
                if (stationExt != null && !string.IsNullOrEmpty(pieceDef.ExtendStation))
                {
                    var stationPrefab = pieceTable.m_pieces.Find(x => x.name == pieceDef.ExtendStation);
                    if (stationPrefab != null)
                    {
                        var station = stationPrefab.GetComponent<CraftingStation>();
                        stationExt.m_craftingStation = station;
                    }

                    var otherExt = pieceTable.m_pieces.Find(x => x.GetComponent<StationExtension>() != null);
                    if (otherExt != null)
                    {
                        var otherStationExt = otherExt.GetComponent<StationExtension>();
                        var otherPiece = otherExt.GetComponent<Piece>();

                        stationExt.m_connectionPrefab = otherStationExt.m_connectionPrefab;
                        piece.m_placeEffect.m_effectPrefabs = otherPiece.m_placeEffect.m_effectPrefabs.ToArray();
                    }
                }

                // Otherwise just copy the effect prefabs of any piece within the table
                else
                {
                    var otherPiece = pieceTable.m_pieces.Find(x => x.GetComponent<Piece>() != null).GetComponent<Piece>();
                    piece.m_placeEffect.m_effectPrefabs.AddRangeToArray(otherPiece.m_placeEffect.m_effectPrefabs);
                }

                Logger.LogInfo($"Registered Piece {prefab.name}");
            }
        }*/
    }
}
