// Veilheim
// a Valheim mod
// 
// File:    BlueprintHooks.cs
// Project: Veilheim

using System.IO;
using System.Linq;
using UnityEngine;
using Veilheim.AssetUtils;
using Veilheim.PatchEvents;

namespace Veilheim.Blueprints
{
    internal class BlueprintHooks : PatchEventConsumer
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
                    Logger.LogInfo($"{bp.Key}.blueprint");

                    var prefab = bp.Value.CreatePrefab();
                    if (prefab != null)
                    {
                        bp.Value.AddToPieceTable();
                    }
                }
            }
        }

        [PatchEvent(typeof(ZNet), nameof(ZNet.Shutdown), PatchEventType.Postfix)]
        public static void DestroyDynamicPrefabs(ZNet instance)
        {
            // Client only
            if (!instance.IsServerInstance())
            {
                Logger.LogMessage("Destroying known blueprints");

                // Try to destroy all known blueprints
                foreach (var bp in Blueprint.m_blueprints)
                {
                    Logger.LogInfo($"{bp.Key}.blueprint");

                    bp.Value.Destroy();
                }
            }
        }

        /// <summary>
        ///     React to the "placement" of make_blueprint
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="piece"></param>
        /// <param name="cancel"></param>
        [PatchEvent(typeof(Player), nameof(Player.PlacePiece), PatchEventType.BlockingPrefix)]
        public static void BeforeCapturingBlueprint(Player instance, Piece piece, ref bool cancel)
        {
            // Client only
            if (!ZNet.instance.IsServerInstance())
            {
                // Capture a new blueprint
                if (piece.name == "make_blueprint")
                {
                    var circleProjector = instance.m_placementGhost.GetComponent<CircleProjector>();
                    if (circleProjector != null)
                    {
                        Object.Destroy(circleProjector);
                    }

                    var bpname = $"blueprint{Blueprint.m_blueprints.Count() + 1:000}";
                    Logger.LogInfo($"Capturing blueprint {bpname}");

                    if (Player.m_localPlayer.m_hoveringPiece != null)
                    {
                        var bp = new Blueprint(bpname);
                        if (bp.Capture(Player.m_localPlayer.m_hoveringPiece.transform.position, Blueprint.selectionRadius, 1.0f))
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

                    // Don't place the piece and clutter the world with it
                    cancel = true;
                }
            }
        }

        /// <summary>
        /// Flatten terrain if left ctrl is pressed
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="piece"></param>
        /// <param name="successful"></param>
        [PatchEvent(typeof(Player), nameof(Player.PlacePiece), PatchEventType.BlockingPrefix)]
        public static void BeforePlacingBlueprint(Player instance, Piece piece, ref bool cancel)
        {
            // Client only
            if (!ZNet.instance.IsServerInstance())
            {
                if (Player.m_localPlayer.m_placementStatus == Player.PlacementStatus.Valid && piece.name.StartsWith("piece_blueprint"))
                { 
                    if (Input.GetKey(KeyCode.LeftControl))
                    {
                        Vector2 extent = Blueprint.m_blueprints.First(x => $"piece_blueprint ({x.Key})" == piece.name).Value.GetExtent();
                        FlattenTerrain.FlattenForBlueprint(instance.m_placementGhost.transform, extent.x, extent.y);
                    }

                    Blueprint bp = Blueprint.m_blueprints[piece.m_name];
                    var transform = instance.m_placementGhost.transform;
                    var position = instance.m_placementGhost.transform.position;
                    var rotation = instance.m_placementGhost.transform.rotation;

                    foreach (var entry in bp.m_pieceEntries)
                    {
                        // Final position
                        Vector3 entryPosition = position + transform.forward * entry.posZ + transform.right * entry.posX + new Vector3(0, entry.posY, 0);
                        
                        // Final rotation
                        Quaternion entryQuat = new Quaternion(entry.rotX, entry.rotY, entry.rotZ, entry.rotW);
                        entryQuat.eulerAngles += rotation.eulerAngles ;

                        // Get the prefab
                        var prefab = ZNetScene.instance.GetPrefab(entry.name);
                        if (prefab == null)
                        {
                            Logger.LogError(piece.name + " not found?");
                        }

                        // Instantiate a new object with the new prefab
                        GameObject gameObject2 = Object.Instantiate(prefab, entryPosition, new Quaternion());
                        gameObject2.GetComponent<Piece>().transform.rotation = entryQuat;

                        CraftingStation componentInChildren = gameObject2.GetComponentInChildren<CraftingStation>();
                        if (componentInChildren)
                        {
                            instance.AddKnownStation(componentInChildren);
                        }
                        Piece component = gameObject2.GetComponent<Piece>();
                        if (component)
                        {
                            component.SetCreator(instance.GetPlayerID());
                        }
                        PrivateArea component2 = gameObject2.GetComponent<PrivateArea>();
                        if (component2)
                        {
                            component2.Setup(Game.instance.GetPlayerProfile().GetName());
                        }
                        WearNTear component3 = gameObject2.GetComponent<WearNTear>();
                        if (component3)
                        {
                            component3.OnPlaced();
                        }

                        gameObject2.GetComponent<Piece>().m_placeEffect.Create(gameObject2.transform.position, rotation, gameObject2.transform, 1f);
                        instance.AddNoise(50f);
                        Game.instance.GetPlayerProfile().m_playerStats.m_builds++;
                    }

                    cancel = true;
                }
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
            // Client only
            if (!ZNet.instance.IsServerInstance())
            {
                // Place a blueprint
                if (successful && piece.name.StartsWith("piece_blueprint"))
                {
                    // Do something with this instance
                    piece.SetCreator(Game.instance.GetPlayerProfile().GetPlayerID());

                    foreach (var component in piece.GetComponents<Piece>())
                    {
                        component.SetCreator(Game.instance.GetPlayerProfile().GetPlayerID());
                        Logger.LogError($"{piece.m_name}.{piece.m_category}");
                    }
                }
            }
        }

        /// <summary>
        ///     Add some camera height while planting a blueprint
        /// </summary>
        /// <param name="instance"></param>
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

        /// <summary>
        ///     Show and change blueprint selection radius
        /// </summary>
        /// <param name="instance"></param>
        [PatchEvent(typeof(Player), nameof(Player.UpdatePlacement), PatchEventType.Postfix)]
        public static void ShowBlueprintRadius(Player instance)
        {
            if (instance.m_placementGhost)
            {
                var piece = instance.m_placementGhost.GetComponent<Piece>();
                if (piece != null)
                {
                    if (piece.name == "make_blueprint" && !piece.IsCreator())
                    {
                        instance.m_maxPlaceDistance = 30;

                        if (Input.GetAxis("Mouse ScrollWheel") < 0f)
                        {
                            Blueprint.selectionRadius -= 2f;
                            if (Blueprint.selectionRadius < 2f)
                            {
                                Blueprint.selectionRadius = 2f;
                            }
                        }

                        if (Input.GetAxis("Mouse ScrollWheel") > 0f)
                        {
                            Blueprint.selectionRadius += 2f;
                        }

                        if (!instance.m_placementMarkerInstance)
                        {
                            return;
                        }

                        var circleProjector = instance.m_placementMarkerInstance.GetComponent<CircleProjector>();
                        if (circleProjector == null)
                        {
                            circleProjector = instance.m_placementMarkerInstance.AddComponent<CircleProjector>();
                            circleProjector.m_prefab = ZNetScene.instance.GetPrefab("piece_workbench").GetComponentInChildren<CircleProjector>().m_prefab;

                            // Force calculation of segment count
                            circleProjector.m_radius = -1;
                            circleProjector.Start();
                        }

                        if (circleProjector.m_radius != Blueprint.selectionRadius)
                        {
                            circleProjector.m_radius = Blueprint.selectionRadius;
                            circleProjector.m_nrOfSegments = (int)circleProjector.m_radius * 4;
                            circleProjector.Update();
                            Logger.LogDebug($"Setting radius to {Blueprint.selectionRadius}");
                        }
                    }
                    else
                    {
                        // Destroy placement marker instance to get rid of the circleprojector
                        if (instance.m_placementMarkerInstance)
                        {
                            Object.DestroyImmediate(instance.m_placementMarkerInstance);
                        }

                        if (!piece.name.StartsWith("piece_blueprint"))
                        {
                            // default value, if we introduce config stuff for this, then change it here!
                            instance.m_maxPlaceDistance = 8;
                        }
                    }
                }
            }
        }
    }
}