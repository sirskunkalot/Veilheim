// Veilheim
// a Valheim mod
// 
// File:    BlueprintManager.cs
// Project: Veilheim

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Veilheim.AssetManagers;
using Veilheim.Configurations;
using Veilheim.PatchEvents;
using Object = UnityEngine.Object;

namespace Veilheim.Blueprints
{
    internal class BlueprintManager : Manager, IPatchEventConsumer
    {
        internal static BlueprintManager Instance { get; private set; }

        internal static string BlueprintPath = Path.Combine(Configuration.ConfigIniPath, "blueprints");
        
        internal float selectionRadius = 10.0f;

        internal float cameraOffset = 5.0f;

        internal readonly Dictionary<string, Blueprint> m_blueprints = new Dictionary<string, Blueprint>();

        private GameObject kbHintsMake;
        private GameObject kbHintsPlace;
        private GameObject kbHintsOrig;

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
            //TODO: save per profile or world or global?
            if (!Directory.Exists(BlueprintPath))
            {
                Directory.CreateDirectory(BlueprintPath);
            }

            // Client only - how to do? or just ignore - there are no bps and maybe someday there will be a server-wide directory of blueprints for sharing :)
            Logger.LogMessage("Loading known blueprints");

            // Try to load all saved blueprints
            foreach (var name in Directory.EnumerateFiles(BlueprintPath, "*.blueprint").Select(Path.GetFileNameWithoutExtension))
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

            Logger.LogInfo("BlueprintManager Initialized");
        }

        [PatchEvent(typeof(ZNetScene), nameof(ZNetScene.Awake), PatchEventType.Postfix)]
        public static void RegisterKnownBlueprints(ZNetScene instance)
        {
            // Client only
            if (!ZNet.instance.IsServerInstance())
            {
                Logger.LogMessage("Registering known blueprints");

                // Create prefabs for all known blueprints
                foreach (var bp in Instance.m_blueprints.Values)
                {
                    bp.CreatePrefab();
                }
            }
        }

        /// <summary>
        ///     React to the "placement" of make_blueprint. Captures a new blueprint and cancels
        ///     the original placement.
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
                        Destroy(circleProjector);
                    }

                    var bpname = $"blueprint{Instance.m_blueprints.Count() + 1:000}";
                    Logger.LogInfo($"Capturing blueprint {bpname}");

                    if (Player.m_localPlayer.m_hoveringPiece != null)
                    {
                        var bp = new Blueprint(bpname);
                        if (bp.Capture(Player.m_localPlayer.m_hoveringPiece.transform.position, Instance.selectionRadius, 1.0f))
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
        ///     Incept placing of the blueprint and instantiate all pieces individually.
        ///     Cancels the real placement of the placeholder piece_blueprint.<br />
        ///     Flatten terrain if left ctrl is pressed.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="piece_bp"></param>
        /// <param name="cancel"></param>
        [PatchEvent(typeof(Player), nameof(Player.PlacePiece), PatchEventType.BlockingPrefix)]
        public static void BeforePlacingBlueprint(Player instance, Piece piece_bp, ref bool cancel)
        {
            // Client and Local only
            if (!ZNet.instance.IsServerInstance())
            {
                if (Player.m_localPlayer.m_placementStatus == Player.PlacementStatus.Valid && piece_bp.name.StartsWith("piece_blueprint"))
                {
                    if (ZInput.GetButton("AltPlace"))
                    {
                        Vector2 extent = Instance.m_blueprints.First(x => $"piece_blueprint ({x.Key})" == piece_bp.name).Value.GetExtent();
                        FlattenTerrain.FlattenForBlueprint(instance.m_placementGhost.transform, extent.x, extent.y,
                            Instance.m_blueprints.First(x => $"piece_blueprint ({x.Key})" == piece_bp.name).Value.m_pieceEntries);
                    }

                    uint cntEffects = 0u;
                    uint maxEffects = 10u;

                    Blueprint bp = Instance.m_blueprints[piece_bp.m_name];
                    var transform = instance.m_placementGhost.transform;
                    var position = instance.m_placementGhost.transform.position;
                    var rotation = instance.m_placementGhost.transform.rotation;

                    foreach (var entry in bp.m_pieceEntries)
                    {
                        // Final position
                        Vector3 entryPosition = position + transform.forward * entry.posZ + transform.right * entry.posX + new Vector3(0, entry.posY, 0);

                        // Final rotation
                        Quaternion entryQuat = new Quaternion(entry.rotX, entry.rotY, entry.rotZ, entry.rotW);
                        entryQuat.eulerAngles += rotation.eulerAngles;

                        // Get the prefab
                        var prefab = PrefabManager.Instance.GetPrefab(entry.name);
                        if (prefab == null)
                        {
                            Logger.LogError(entry.name + " not found?");
                        }

                        // Instantiate a new object with the new prefab
                        GameObject gameObject = Instantiate(prefab, entryPosition, entryQuat);

                        // Register special effects
                        CraftingStation craftingStation = gameObject.GetComponentInChildren<CraftingStation>();
                        if (craftingStation)
                        {
                            instance.AddKnownStation(craftingStation);
                        }
                        Piece piece = gameObject.GetComponent<Piece>();
                        if (piece)
                        {
                            piece.SetCreator(instance.GetPlayerID());
                        }
                        PrivateArea privateArea = gameObject.GetComponent<PrivateArea>();
                        if (privateArea)
                        {
                            privateArea.Setup(Game.instance.GetPlayerProfile().GetName());
                        }
                        WearNTear wearntear = gameObject.GetComponent<WearNTear>();
                        if (wearntear)
                        {
                            wearntear.OnPlaced();
                        }
                        TextReceiver textReceiver = gameObject.GetComponent<TextReceiver>();
                        if (textReceiver != null)
                        {
                            textReceiver.SetText(entry.additionalInfo);
                        }

                        // Limited build effects
                        if (cntEffects < maxEffects)
                        {
                            piece.m_placeEffect.Create(gameObject.transform.position, rotation, gameObject.transform, 1f);
                            instance.AddNoise(50f);
                            cntEffects++;
                        }

                        // Count up player builds
                        Game.instance.GetPlayerProfile().m_playerStats.m_builds++;
                    }

                    cancel = true;
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
                            if (Input.GetKey(KeyCode.LeftShift))
                            {
                                // TODO: base min/max off of selected piece dimensions
                                float minOffset = 2f;
                                float maxOffset = 20f;
                                if (Input.GetAxis("Mouse ScrollWheel") < 0f)
                                {
                                    Instance.cameraOffset = Mathf.Clamp(Instance.cameraOffset += 1f, minOffset, maxOffset);
                                }

                                if (Input.GetAxis("Mouse ScrollWheel") > 0f)
                                {
                                    Instance.cameraOffset = Mathf.Clamp(Instance.cameraOffset -= 1f, minOffset, maxOffset);
                                }
                            }
                            
                            instance.transform.position += new Vector3(0, Instance.cameraOffset, 0);
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
                        instance.m_maxPlaceDistance = 50f;

                        if (Input.GetAxis("Mouse ScrollWheel") < 0f)
                        {
                            Instance.selectionRadius -= 2f;
                            if (Instance.selectionRadius < 2f)
                            {
                                Instance.selectionRadius = 2f;
                            }
                        }

                        if (Input.GetAxis("Mouse ScrollWheel") > 0f)
                        {
                            Instance.selectionRadius += 2f;
                        }

                        if (!instance.m_placementMarkerInstance)
                        {
                            return;
                        }

                        var circleProjector = instance.m_placementMarkerInstance.GetComponent<CircleProjector>();
                        if (circleProjector == null)
                        {
                            circleProjector = instance.m_placementMarkerInstance.AddComponent<CircleProjector>();
                            circleProjector.m_prefab = PrefabManager.Instance.GetPrefab("piece_workbench").GetComponentInChildren<CircleProjector>().m_prefab;

                            // Force calculation of segment count
                            circleProjector.m_radius = -1;
                            circleProjector.Start();
                        }

                        if (circleProjector.m_radius != Instance.selectionRadius)
                        {
                            circleProjector.m_radius = Instance.selectionRadius;
                            circleProjector.m_nrOfSegments = (int)circleProjector.m_radius * 4;
                            circleProjector.Update();
                            Logger.LogDebug($"Setting radius to {Instance.selectionRadius}");
                        }
                    }
                    else
                    {
                        // Destroy placement marker instance to get rid of the circleprojector
                        if (instance.m_placementMarkerInstance)
                        {
                            DestroyImmediate(instance.m_placementMarkerInstance);
                        }

                        // Restore placementDistance
                        if (!piece.name.StartsWith("piece_blueprint"))
                        {
                            // default value, if we introduce config stuff for this, then change it here!
                            instance.m_maxPlaceDistance = 8;
                        }

                        // Reset rotation when changing camera
                        if (piece.name.StartsWith("piece_blueprint") && Input.GetAxis("Mouse ScrollWheel") != 0f && Input.GetKey(KeyCode.LeftShift))
                        {

                            if (Input.GetAxis("Mouse ScrollWheel") < 0f)
                            {
                                instance.m_placeRotation++;
                            }

                            if (Input.GetAxis("Mouse ScrollWheel") > 0f)
                            {
                               instance.m_placeRotation--;
                            }

                        }
                    }
                }
            }
        }

        private static void InitHint(GameObject hint, string component, bool active, string text = null)
        {
            GameObject obj;
            Text txt;
            obj = hint.transform.Find(component).gameObject;
            obj.SetActive(active);

            if (text != null)
            {
                string translated;
                LocalizationManager.TryTranslate(text, out translated);
                txt = obj.transform.Find("Text").GetComponent<Text>();
                txt.text = translated;
            }
        }

        /// <summary>
        ///     Changes the hint GUI for the BlueprintRune
        /// </summary>
        [PatchEvent(typeof(KeyHints), nameof(KeyHints.UpdateHints), PatchEventType.Prefix)]
        public static void ShowBlueprintHints(KeyHints __instance)
        {
            Player localPlayer = Player.m_localPlayer;
            if (localPlayer == null)
            {
                return;
            }

            if (localPlayer.InPlaceMode() && localPlayer.m_buildPieces.GetSelectedPiece() != null)
            {
                if (Instance.kbHintsOrig == null)
                {
                    Instance.kbHintsOrig = __instance.m_buildHints;
                }
                if (Instance.kbHintsMake == null)
                {
                    Instance.kbHintsMake = Object.Instantiate(Instance.kbHintsOrig);
                    Instance.kbHintsMake.transform.SetParent(Instance.kbHintsOrig.transform.parent.parent, false);
                    Instance.kbHintsMake.name = "BlueprintHintsMake";

                    InitHint(Instance.kbHintsMake, "Keyboard/Place", true, "hud_bpcapture");
                    InitHint(Instance.kbHintsMake, "Keyboard/Remove", false);
                    InitHint(Instance.kbHintsMake, "Keyboard/BuildMenu", false);
                    InitHint(Instance.kbHintsMake, "Keyboard/AltPlace", false);
                    InitHint(Instance.kbHintsMake, "Keyboard/rotate", true, "hud_bpradius");
                }
                if (Instance.kbHintsPlace == null)
                {
                    Instance.kbHintsPlace = Object.Instantiate(Instance.kbHintsOrig);
                    Instance.kbHintsPlace.transform.SetParent(Instance.kbHintsOrig.transform.parent.parent, false);
                    Instance.kbHintsPlace.name = "BlueprintHintsPlace";

                    InitHint(Instance.kbHintsPlace, "Keyboard/Place", true, "hud_bpplace");
                    InitHint(Instance.kbHintsPlace, "Keyboard/Remove", false);
                    InitHint(Instance.kbHintsPlace, "Keyboard/BuildMenu", false);
                    InitHint(Instance.kbHintsPlace, "Keyboard/AltPlace", true, "hud_bpflatten");
                    InitHint(Instance.kbHintsPlace, "Keyboard/rotate", true, "hud_bprotate");
                }

                if (localPlayer.m_buildPieces.name.Equals("_BlueprintPieceTable"))
                {
                    if (localPlayer.m_buildPieces.GetSelectedPiece().name == "make_blueprint")
                    {
                        Instance.kbHintsMake.SetActive(true);
                        Instance.kbHintsPlace.SetActive(false);
                        Instance.kbHintsOrig.SetActive(false);
                        __instance.m_buildHints = Instance.kbHintsMake;
                    }
                    else
                    {
                        Instance.kbHintsMake.SetActive(false);
                        Instance.kbHintsPlace.SetActive(true);
                        Instance.kbHintsOrig.SetActive(false);
                        __instance.m_buildHints = Instance.kbHintsPlace;
                    }
                    
                }
                else
                {
                    Instance.kbHintsMake.SetActive(false);
                    Instance.kbHintsPlace.SetActive(false);
                    Instance.kbHintsOrig.SetActive(true);
                    __instance.m_buildHints = Instance.kbHintsOrig;
                }
            }
        }
    }
}
