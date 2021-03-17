﻿using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Veilheim.AssetUtils;
using Veilheim.PatchEvents;

namespace Veilheim.Blueprints
{
    class BlueprintHooks : PatchEventConsumer
    {
        [PatchEvent(typeof(ZNet), nameof(ZNet.Awake), PatchEventType.Postfix)]
        public static void LoadKnownBlueprints(ZNet instance)
        {
            // Client only
            if (!instance.IsServerInstance())
            {
                Logger.LogMessage("Loading known blueprints");

                // Try to load all saved blueprints
                foreach (var name in Directory.EnumerateFiles(Blueprint.GetBlueprintPath(), "*.blueprint").Select(Path.GetFileNameWithoutExtension))
                {
                    if (!Blueprint.m_blueprints.ContainsKey(name))
                    {
                        var bp = new Blueprint(name);
                        if (bp.Load())
                        {
                            Blueprint.m_blueprints.Add(name, bp);
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
                if (Blueprint.m_stub == null)
                {
                    var assetBundle = AssetLoader.LoadAssetBundleFromResources("blueprintrune");
                    Blueprint.m_stub = assetBundle.LoadAsset<GameObject>("piece_blueprint");
                    assetBundle.Unload(false);
                }

                // Get prefabs from all known blueprints
                foreach (var bp in Blueprint.m_blueprints)
                {
                    Logger.LogDebug($"{bp.Key}.blueprint");

                    var prefab = bp.Value.CreatePrefab();
                    if (prefab != null)
                    {
                        AssetManager.RegisterPiecePrefab(prefab, new PieceDef { PieceTable = "_BlueprintPieceTable" });
                    }
                }

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
                string bpname = "blueprint" + String.Format("{0:000}", Blueprint.m_blueprints.Count() + 1);
                Logger.LogInfo($"Capturing blueprint {bpname}");

                if (Player.m_localPlayer.m_hoveringPiece != null)
                {
                    var bp = new Blueprint(bpname);
                    if (bp.Capture(Player.m_localPlayer.m_hoveringPiece.transform.position, 3.0f, 1.0f))
                    {
                        TextInput.instance.m_queuedSign = new Blueprint.BlueprintSaveGUI(bp);
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

            // Place a blueprint
            if (successful && piece.name.StartsWith("piece_blueprint"))
            {
                // Do something with this instance
                piece.SetCreator(Game.instance.GetPlayerProfile().GetPlayerID());

                foreach (var component in piece.GetComponents<Piece>())
                {
                    component.SetCreator(Game.instance.GetPlayerProfile().GetPlayerID());
                }
            }
        }

        [PatchEvent(typeof(GameCamera), nameof(GameCamera.UpdateCamera), PatchEventType.Postfix)]
        public static void AdjustCameraHeight(GameCamera instance)
        {
            if (Player.m_localPlayer)
            {
                if (Player.m_localPlayer.InPlaceMode())
                {
                    if (Player.m_localPlayer.m_placementGhost)
                    {
                        if (Player.m_localPlayer.m_placementGhost.name.StartsWith("piece_blueprint"))
                        {
                            instance.transform.position += new Vector3(0, 5.0f, 0);
                        }
                    }
                }
            }
        }
    }
}
