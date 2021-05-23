// Veilheim
// a Valheim mod
// 
// File:    BlueprintManager.cs
// Project: Veilheim

using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Veilheim.Utils;
using Object = UnityEngine.Object;

namespace Veilheim.Blueprints
{
    internal class BlueprintManager : Manager
    {
        internal static BlueprintManager Instance { get; private set; }

        internal static string BlueprintPath = Path.Combine(ConfigUtil.GetConfigPath(), "blueprints");
        
        private float selectionRadius = 10.0f;

        private float cameraOffsetMake = 0.0f;
        private float cameraOffsetPlace = 5.0f;

        internal readonly Dictionary<string, Blueprint> m_blueprints = new Dictionary<string, Blueprint>();

        private void Awake()
        {
            if (Instance != null)
            {
                Jotunn.Logger.LogError($"Two instances of singleton {GetType()}");
                return;
            }

            Instance = this;
        }

        internal override void Init()
        {
            //TODO: Client only - how to do? or just ignore - there are no bps and maybe someday there will be a server-wide directory of blueprints for sharing :)
            
            LoadAssets();

            LoadKnownBlueprints();

            CreateCustomKeyHints();

            ItemManager.OnVanillaItemsAvailable += GetPlanShader;

            On.ZNetScene.Awake += RegisterKnownBlueprints;
            On.Player.PlacePiece += BeforePlaceBlueprintPiece;
            On.GameCamera.UpdateCamera += AdjustCameraHeight;
            On.Player.UpdatePlacement += ShowBlueprintRadius;

            Jotunn.Logger.LogInfo("BlueprintManager Initialized");
        }

        private void LoadAssets()
        {
            AssetBundle assetBundle = AssetUtils.LoadAssetBundleFromResources("blueprints", typeof(VeilheimPlugin).Assembly);

            PieceManager.Instance.AddPieceTable(assetBundle.LoadAsset<GameObject>("_BlueprintPieceTable"));

            GameObject runeprefab = assetBundle.LoadAsset<GameObject>("BlueprintRune");
            CustomItem rune = new CustomItem(runeprefab, fixReference: false);
            ItemManager.Instance.AddItem(rune);

            CustomRecipe runeRecipe = new CustomRecipe(new RecipeConfig()
            {
                Item = "BlueprintRune",
                Amount = 1,
                Requirements = new RequirementConfig[]
                {
                    new RequirementConfig {Item = "Stone", Amount = 1}
                }
            });
            ItemManager.Instance.AddRecipe(runeRecipe);

            GameObject makebp_prefab = assetBundle.LoadAsset<GameObject>("make_blueprint");
            PrefabManager.Instance.AddPrefab(makebp_prefab);
            GameObject placebp_prefab = assetBundle.LoadAsset<GameObject>("piece_blueprint");
            PrefabManager.Instance.AddPrefab(placebp_prefab);

            TextAsset[] textAssets = assetBundle.LoadAllAssets<TextAsset>();
            foreach (var textAsset in textAssets)
            {
                var lang = textAsset.name.Replace(".json", null);
                LocalizationManager.Instance.AddJson(lang, textAsset.ToString());
            }
            assetBundle.Unload(false);
        }

        private void LoadKnownBlueprints()
        {
            Jotunn.Logger.LogMessage("Loading known blueprints");

            if (!Directory.Exists(BlueprintPath))
            {
                Directory.CreateDirectory(BlueprintPath);
            }

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
                        Jotunn.Logger.LogWarning($"Could not load blueprint {name}");
                    }
                }
            }
        }

        private void CreateCustomKeyHints()
        {
            KeyHintConfig KHC_default = new KeyHintConfig
            {
                Item = "BlueprintRune",
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = "BuildMenu", HintToken = "$" }
                }
            };
            GUIManager.Instance.AddKeyHint(KHC_default);

            KeyHintConfig KHC_make = new KeyHintConfig
            {
                Item = "BlueprintRune",
                Piece = "make_blueprint",
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bpcapture" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bpradius" }
                }
            };
            GUIManager.Instance.AddKeyHint(KHC_make);

            foreach (var entry in m_blueprints)
            {
                entry.Value.CreateKeyHint();
            }
        }

        private void GetPlanShader()
        {
            ShaderHelper.planShader = Shader.Find("Lux Lit Particles/ Bumped");
        }

        private void RegisterKnownBlueprints(On.ZNetScene.orig_Awake orig, ZNetScene self)
        {
            orig(self);

            // Client only
            if (!ZNet.instance.IsServerInstance())
            {
                Jotunn.Logger.LogMessage("Registering known blueprints");

                // Create prefabs for all known blueprints
                foreach (var bp in Instance.m_blueprints.Values)
                {
                    bp.CreatePrefab();
                }
            }
        }

        /// <summary>
        ///     Incept placing of the meta pieces.
        ///     Cancels the real placement of the placeholder pieces.
        /// </summary>
        private bool BeforePlaceBlueprintPiece(On.Player.orig_PlacePiece orig, Player self, Piece piece)
        {
            // Client only
            if (!ZNet.instance.IsServerInstance())
            {
                // Capture a new blueprint
                if (piece.name == "make_blueprint")
                {
                    var circleProjector = self.m_placementGhost.GetComponent<CircleProjector>();
                    if (circleProjector != null)
                    {
                        Destroy(circleProjector);
                    }

                    var bpname = $"blueprint{Instance.m_blueprints.Count() + 1:000}";
                    Jotunn.Logger.LogInfo($"Capturing blueprint {bpname}");

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
                            Jotunn.Logger.LogWarning($"Could not capture blueprint {bpname}");
                        }
                    }
                    else
                    {
                        Jotunn.Logger.LogInfo("Not hovering any piece");
                    }

                    // Reset Camera offset
                    Instance.cameraOffsetMake = 0f;

                    // Don't place the piece and clutter the world with it
                    return false;
                }

                // Place a known blueprint
                if (Player.m_localPlayer.m_placementStatus == Player.PlacementStatus.Valid && piece.name.StartsWith("piece_blueprint"))
                {
                    Blueprint bp = Instance.m_blueprints[piece.m_name];
                    var transform = self.m_placementGhost.transform;
                    var position = self.m_placementGhost.transform.position;
                    var rotation = self.m_placementGhost.transform.rotation;

                    if (ZInput.GetButton("AltPlace"))
                    {
                        Vector2 extent = bp.GetExtent();
                        FlattenTerrain.FlattenForBlueprint(transform, extent.x, extent.y, bp.m_pieceEntries);
                    }

                    uint cntEffects = 0u;
                    uint maxEffects = 10u;

                    foreach (var entry in bp.m_pieceEntries)
                    {
                        // Final position
                        Vector3 entryPosition = position + transform.forward * entry.posZ + transform.right * entry.posX + new Vector3(0, entry.posY, 0);

                        // Final rotation
                        Quaternion entryQuat = new Quaternion(entry.rotX, entry.rotY, entry.rotZ, entry.rotW);
                        entryQuat.eulerAngles += rotation.eulerAngles;

                        // Get the prefab of the piece or the plan piece
                        string prefabName = entry.name;
                        if (!ConfigUtil.Get<bool>("Blueprints", "allowPlacementWithoutMaterial") || !ZInput.GetButton("Crouch"))
                        {
                            prefabName += "_planned";
                        }
                        GameObject prefab = PrefabManager.Instance.GetPrefab(prefabName);
                        if (!prefab)
                        {
                            Jotunn.Logger.LogError(entry.name + " not found?");
                            continue;
                        }

                        // Instantiate a new object with the new prefab
                        GameObject gameObject = Instantiate(prefab, entryPosition, entryQuat);

                        // Register special effects
                        CraftingStation craftingStation = gameObject.GetComponentInChildren<CraftingStation>();
                        if (craftingStation)
                        {
                            self.AddKnownStation(craftingStation);
                        }
                        Piece newpiece = gameObject.GetComponent<Piece>();
                        if (newpiece != null)
                        {
                            newpiece.SetCreator(self.GetPlayerID());
                        }
                        PrivateArea privateArea = gameObject.GetComponent<PrivateArea>();
                        if (privateArea != null)
                        {
                            privateArea.Setup(Game.instance.GetPlayerProfile().GetName());
                        }
                        WearNTear wearntear = gameObject.GetComponent<WearNTear>();
                        if (wearntear != null)
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
                            newpiece.m_placeEffect.Create(gameObject.transform.position, rotation, gameObject.transform, 1f);
                            self.AddNoise(50f);
                            cntEffects++;
                        }

                        // Count up player builds
                        Game.instance.GetPlayerProfile().m_playerStats.m_builds++;
                    }

                    // Reset Camera offset
                    Instance.cameraOffsetPlace = 5f;

                    // Dont set the blueprint piece and clutter the world with it
                    return false;
                }
            }

            return orig(self, piece);
        }

        /// <summary>
        ///     Add some camera height while planting a blueprint
        /// </summary>
        private void AdjustCameraHeight(On.GameCamera.orig_UpdateCamera orig, GameCamera self, float dt)
        {
            orig(self, dt);

            if (Player.m_localPlayer != null)
            {
                if (Player.m_localPlayer.InPlaceMode())
                {
                    if (Player.m_localPlayer.m_placementGhost)
                    {
                        var pieceName = Player.m_localPlayer.m_placementGhost.name;
                        if (pieceName.StartsWith("make_blueprint"))
                        {
                            if (Input.GetKey(KeyCode.LeftShift))
                            {
                                float minOffset = 0f;
                                float maxOffset = 20f;
                                if (Input.GetAxis("Mouse ScrollWheel") < 0f)
                                {
                                    Instance.cameraOffsetMake = Mathf.Clamp(Instance.cameraOffsetMake += 1f, minOffset, maxOffset);
                                }

                                if (Input.GetAxis("Mouse ScrollWheel") > 0f)
                                {
                                    Instance.cameraOffsetMake = Mathf.Clamp(Instance.cameraOffsetMake -= 1f, minOffset, maxOffset);
                                }
                            }

                            self.transform.position += new Vector3(0, Instance.cameraOffsetMake, 0);
                        }
                        if (pieceName.StartsWith("piece_blueprint"))
                        {
                            if (Input.GetKey(KeyCode.LeftShift))
                            {
                                // TODO: base min/max off of selected piece dimensions
                                float minOffset = 2f;
                                float maxOffset = 20f;
                                if (Input.GetAxis("Mouse ScrollWheel") < 0f)
                                {
                                    Instance.cameraOffsetPlace = Mathf.Clamp(Instance.cameraOffsetPlace += 1f, minOffset, maxOffset);
                                }

                                if (Input.GetAxis("Mouse ScrollWheel") > 0f)
                                {
                                    Instance.cameraOffsetPlace = Mathf.Clamp(Instance.cameraOffsetPlace -= 1f, minOffset, maxOffset);
                                }
                            }

                            self.transform.position += new Vector3(0, Instance.cameraOffsetPlace, 0);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Show and change blueprint selection radius
        /// </summary>
        private void ShowBlueprintRadius(On.Player.orig_UpdatePlacement orig, Player self, bool takeInput, float dt)
        {
            orig(self, takeInput, dt);

            if (self.m_placementGhost)
            {
                var piece = self.m_placementGhost.GetComponent<Piece>();
                if (piece != null)
                {
                    if (piece.name == "make_blueprint" && !piece.IsCreator())
                    {
                        if (!self.m_placementMarkerInstance)
                        {
                            return;
                        }

                        self.m_maxPlaceDistance = 50f;

                        if (!Input.GetKey(KeyCode.LeftShift))
                        {
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
                        }

                        var circleProjector = self.m_placementMarkerInstance.GetComponent<CircleProjector>();
                        if (circleProjector == null)
                        {
                            circleProjector = self.m_placementMarkerInstance.AddComponent<CircleProjector>();
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
                            Jotunn.Logger.LogDebug($"Setting radius to {Instance.selectionRadius}");
                        }
                    }
                    else if (piece.name.StartsWith("piece_blueprint"))
                    {
                        // Destroy placement marker instance to get rid of the circleprojector
                        if (self.m_placementMarkerInstance)
                        {
                            DestroyImmediate(self.m_placementMarkerInstance);
                        }

                        // Reset rotation when changing camera
                        if (Input.GetAxis("Mouse ScrollWheel") != 0f && Input.GetKey(KeyCode.LeftShift))
                        {

                            if (Input.GetAxis("Mouse ScrollWheel") < 0f)
                            {
                                self.m_placeRotation++;
                            }

                            if (Input.GetAxis("Mouse ScrollWheel") > 0f)
                            {
                                self.m_placeRotation--;
                            }

                        }
                    }
                    else
                    {
                        // Destroy placement marker instance to get rid of the circleprojector
                        if (self.m_placementMarkerInstance)
                        {
                            DestroyImmediate(self.m_placementMarkerInstance);
                        }

                        // Restore placementDistance
                        // default value, if we introduce config stuff for this, then change it here!
                        self.m_maxPlaceDistance = 8;
                    }
                }
            }
        }
    }
}
