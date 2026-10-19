#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Synty.SidekickCharacters.API;
using Synty.SidekickCharacters.Database;
using Synty.SidekickCharacters.Database.DTO;
using Synty.SidekickCharacters.Enums;
using Synty.SidekickCharacters.Serialization;
using TurnBasedGame.Skills;
using TurnBasedGame.Unit;
using UCP.Bridge;
using Unity.VisualScripting.YamlDotNet.Serialization;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

public class CharacterBuilder : IUCPScript
{
    public string Name => "character-builder";
    public string Description => "Tao hoac validate Unit Prefab tu Synty Sidekick Model Prefab theo workflow";

    private static readonly int ColorMapProp = Shader.PropertyToID("_ColorMap");

    [Serializable]
    public class ColorPaletteParams
    {
        public string skinColor;
        public string hairColor;
        public string eyeColor;
        public string primaryColor;
        public string secondaryColor;
        public string trimColor;
        public string metalColor;
        public string jewelColor;
    }

    [Serializable]
    public class ColorOverride
    {
        public string propertyName;
        public string colorHex;
    }

    [Serializable]
    private class BuildParams
    {
        public string action = "build";
        public string className;
        public string modelPrefabPath;
        public string weaponPrefabPath;
        public string weaponBoneName = "prop_r";
        public Vector3 weaponLocalPos = new Vector3(0.033f, 0.016f, 0.002f);
        public Vector3 weaponLocalEuler = new Vector3(-2.5f, -94f, 88f);
        public Vector3 weaponLocalScale = Vector3.one;
        public string baseUnitPrefabPath = "Assets/MyGame/Resource/Unit/Knight.prefab";
        public string outputPrefabPath;
        public string animatorControllerPath;
        public string unitDataPath;
        public string[] skillPaths;

        // Synty Sidekick generation options
        public bool generateSidekickModel = false;
        public float bodyTypeBlend = -10f;
        public float bodySizeBlend = 15f;
        public float musclesBlend = 15f;

        // Dynamic Parts & Colors
        public string[] partNames;
        public ColorPaletteParams palette;
        public string skinColor = "D8B294";
        public string hairColor = "2B262D";
        public string eyeColor = "9B111E";
        public string primaryColor = "1A1A24";
        public string secondaryColor = "251F33";
        public string trimColor = "D4AF37";
        public string metalColor = "E5E5EA";
        public string jewelColor = "9B111E";
        public ColorOverride[] colorOverrides;
    }

    public object Execute(string paramsJson)
    {
        var p = string.IsNullOrEmpty(paramsJson)
            ? new BuildParams()
            : JsonUtility.FromJson<BuildParams>(paramsJson);

        if (string.IsNullOrEmpty(p.className))
            return new { success = false, error = "Missing className" };

        if (p.action == "validate")
            return ValidateUnitPrefab(p);

        if (p.action == "check-animation")
            return CheckAnimation(p);

        if (p.action == "set-weapon-transform")
            return SetWeaponTransform(p);

        if (p.action == "setup-animation-events")
            return SetupAnimationEvents(p);

        return BuildUnitPrefab(p);
    }

    private object BuildUnitPrefab(BuildParams p)
    {
        var logs = new List<string>();
        string outputPath = string.IsNullOrEmpty(p.outputPrefabPath)
            ? $"Assets/MyGame/Resource/Unit/{p.className}.prefab"
            : p.outputPrefabPath;

        string modelPath = p.modelPrefabPath;
        if (string.IsNullOrEmpty(modelPath))
            modelPath = $"Assets/MyGame/Prefabs/Model/{p.className}_M/{p.className}.prefab";

        if (p.generateSidekickModel || AssetDatabase.LoadAssetAtPath<GameObject>(modelPath) == null)
        {
            string genError = GenerateSyntyModelPrefab(p, modelPath, logs);
            if (!string.IsNullOrEmpty(genError))
                return new { success = false, error = genError, logs };
        }

        // 1. Validate inputs
        var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(p.baseUnitPrefabPath);
        var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (basePrefab == null || modelPrefab == null)
            return new { success = false, error = $"Base prefab ({p.baseUnitPrefabPath}) or Model prefab ({modelPath}) not found", logs };

        Mesh directMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"Assets/MyGame/Prefabs/Model/{p.className}_M/Meshes/{p.className}.asset");
        Material directMat = AssetDatabase.LoadAssetAtPath<Material>($"Assets/MyGame/Prefabs/Model/{p.className}_M/Materials/{p.className}.mat");
        logs.Add($"Direct loaded: mesh={(directMesh != null ? directMesh.name : "null")}, mat={(directMat != null ? directMat.name : "null")}");

        if (p.generateSidekickModel && File.Exists(outputPath))
        {
            AssetDatabase.DeleteAsset(outputPath);
            AssetDatabase.Refresh();
        }

        string sourcePrefabToLoad = p.baseUnitPrefabPath;
        GameObject root = PrefabUtility.LoadPrefabContents(sourcePrefabToLoad);
        try
        {
            root.name = p.className;

            // 3. Setup Body & Scale = 1.5
            Transform body = root.transform.Find("Body");
            if (body == null)
                return new { success = false, error = "Child 'Body' not found on base prefab", logs };

            body.localPosition = Vector3.zero;
            body.localRotation = Quaternion.identity;
            body.localScale = new Vector3(1.5f, 1.5f, 1.5f);

            // Xoa model cu trong Body (giu lai cac particle/effect neu khong phai model)
            for (int i = body.childCount - 1; i >= 0; i--)
            {
                Transform child = body.GetChild(i);
                if (child.GetComponentInChildren<SkinnedMeshRenderer>(true) != null ||
                    child.name.Contains("Human") ||
                    child.name.Contains("RPG-Character") ||
                    child.name.Contains(p.className))
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            // 4. Gan Model Prefab moi vao Body
            GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, body);
            modelInstance.name = modelPrefab.name;
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;

            var instSmr = modelInstance.GetComponentInChildren<SkinnedMeshRenderer>();
            var modelSmr = modelPrefab.GetComponentInChildren<SkinnedMeshRenderer>();
            logs.Add($"modelSmr: smr={(modelSmr != null)}, mesh={(modelSmr?.sharedMesh != null ? modelSmr.sharedMesh.name : "null")}, mat={(modelSmr?.sharedMaterial != null ? modelSmr.sharedMaterial.name : "null")}");
            logs.Add($"instSmr before: smr={(instSmr != null)}, mesh={(instSmr?.sharedMesh != null ? instSmr.sharedMesh.name : "null")}, mat={(instSmr?.sharedMaterial != null ? instSmr.sharedMaterial.name : "null")}");
            if (instSmr != null)
            {
                if (instSmr.sharedMesh == null && directMesh != null)
                    instSmr.sharedMesh = directMesh;
                if (instSmr.sharedMaterial == null && directMat != null)
                    instSmr.sharedMaterial = directMat;
                instSmr.updateWhenOffscreen = true;
            }
            logs.Add($"instSmr after: smr={(instSmr != null)}, mesh={(instSmr?.sharedMesh != null ? instSmr.sharedMesh.name : "null")}, mat={(instSmr?.sharedMaterial != null ? instSmr.sharedMaterial.name : "null")}");

            // Lay Avatar tu Model Prefab hoac Meshes folder
            Avatar modelAvatar = null;
            Animator childAnimator = modelInstance.GetComponent<Animator>();
            if (childAnimator != null)
            {
                modelAvatar = childAnimator.avatar;
                Object.DestroyImmediate(childAnimator);
            }
            if (modelAvatar == null)
            {
                string modelDir = Path.GetDirectoryName(modelPath)?.Replace('\\', '/') ?? "";
                string avatarAssetPath = $"{modelDir}/Meshes/{Path.GetFileNameWithoutExtension(modelPath)}-avatar.asset";
                modelAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(avatarAssetPath);
            }

            // 5. Cau hinh Animator tren Body
            Animator bodyAnimator = body.GetComponent<Animator>();
            if (bodyAnimator == null) bodyAnimator = body.gameObject.AddComponent<Animator>();

            if (modelAvatar != null)
            {
                bodyAnimator.avatar = modelAvatar;
                logs.Add($"Assigned Avatar: {modelAvatar.name}");
            }

            if (!string.IsNullOrEmpty(p.animatorControllerPath))
            {
                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(p.animatorControllerPath);
                if (controller != null)
                {
                    bodyAnimator.runtimeAnimatorController = controller;
                    logs.Add($"Assigned Controller: {controller.name}");
                }
            }

            // 6. Gan Weapon vao bone tay phai va cap nhat _effectSpawnPoint
            Transform effectSpawnTarget = null;
            if (!string.IsNullOrEmpty(p.weaponPrefabPath))
            {
                var weaponPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(p.weaponPrefabPath);
                Transform targetBone = FindDeepChild(modelInstance.transform, p.weaponBoneName)
                                    ?? FindDeepChild(modelInstance.transform, "prop_r")
                                    ?? FindDeepChild(modelInstance.transform, "hand_r")
                                    ?? FindDeepChild(modelInstance.transform, "ring_01_r");

                if (weaponPrefab != null && targetBone != null)
                {
                    GameObject weaponInstance = (GameObject)PrefabUtility.InstantiatePrefab(weaponPrefab, targetBone);
                    weaponInstance.name = weaponPrefab.name;

                    Vector3 localPos = p.weaponLocalPos;
                    Vector3 localEuler = p.weaponLocalEuler;
                    if ((p.className == "Adjudicator" || p.className == "Herbalist") && p.weaponLocalEuler == new Vector3(-2.5f, -94f, 88f))
                    {
                        localPos = new Vector3(-0.014f, -0.15f, -0.033f);
                        localEuler = new Vector3(180f, 0f, 0f);
                    }

                    Transform attachPoint = weaponInstance.transform.Find("AttachPoint");
                    if (attachPoint != null && p.weaponLocalPos == new Vector3(0.033f, 0.016f, 0.002f))
                    {
                        Quaternion invRot = Quaternion.Inverse(attachPoint.localRotation);
                        weaponInstance.transform.localRotation = invRot;
                        weaponInstance.transform.localPosition = -(invRot * attachPoint.localPosition);
                    }
                    else
                    {
                        weaponInstance.transform.localPosition = localPos;
                        weaponInstance.transform.localEulerAngles = localEuler;
                    }
                    weaponInstance.transform.localScale = p.weaponLocalScale;
                    effectSpawnTarget = weaponInstance.transform;
                    logs.Add($"Attached weapon '{weaponPrefab.name}' to bone '{targetBone.name}'");
                }
            }

            // 7. Gan UnitData, RotationNode va Skills
            var unitCtrl = root.GetComponent<UnitController>();
            if (unitCtrl != null)
            {
                var soCtrl = new SerializedObject(unitCtrl);
                var rotNodeProp = soCtrl.FindProperty("RotationNode");
                if (rotNodeProp != null) rotNodeProp.objectReferenceValue = body;

                if (!string.IsNullOrEmpty(p.unitDataPath))
                {
                    var unitData = AssetDatabase.LoadAssetAtPath<UnitData>(p.unitDataPath);
                    if (unitData != null)
                    {
                        var unitDataProp = soCtrl.FindProperty("unitData");
                        if (unitDataProp != null) unitDataProp.objectReferenceValue = unitData;

                        if (p.skillPaths != null && p.skillPaths.Length > 0)
                        {
                            var soData = new SerializedObject(unitData);
                            var skillsProp = soData.FindProperty("startingSkills") ?? soData.FindProperty("StartingSkills");
                            if (skillsProp != null)
                            {
                                skillsProp.ClearArray();
                                for (int i = 0; i < p.skillPaths.Length; i++)
                                {
                                    var skill = AssetDatabase.LoadAssetAtPath<SkillBase>(p.skillPaths[i]);
                                    if (skill != null)
                                    {
                                        skillsProp.InsertArrayElementAtIndex(i);
                                        skillsProp.GetArrayElementAtIndex(i).objectReferenceValue = skill;
                                    }
                                }
                                soData.ApplyModifiedPropertiesWithoutUndo();
                                EditorUtility.SetDirty(unitData);
                            }
                        }
                        logs.Add($"Assigned UnitData: {unitData.name}");
                    }
                }
                soCtrl.ApplyModifiedPropertiesWithoutUndo();
            }

            // 8. Cap nhat UnitAnimator references (nam tren Body hoac Root)
            var unitAnimator = root.GetComponentInChildren<UnitAnimator>(true);
            if (unitAnimator != null)
            {
                var soAnim = new SerializedObject(unitAnimator);
                var animProp = soAnim.FindProperty("animator");
                if (animProp != null) animProp.objectReferenceValue = bodyAnimator;

                var spawnProp = soAnim.FindProperty("_effectSpawnPoint");
                if (spawnProp != null)
                {
                    if (effectSpawnTarget == null)
                        effectSpawnTarget = root.transform.Find("EffectSpawnPoint") ?? body;
                    spawnProp.objectReferenceValue = effectSpawnTarget;
                }
                soAnim.ApplyModifiedPropertiesWithoutUndo();
                logs.Add("Updated UnitAnimator references");
            }

            // 9. Save Unit Prefab
            PrefabUtility.SaveAsPrefabAsset(root, outputPath, out bool saved);
            if (!saved)
                return new { success = false, error = $"Failed to save prefab at {outputPath}", logs };
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 10. Rebuild MatchContentCatalog
        MatchContentCatalogBuilder.Rebuild();
        logs.Add("MatchContentCatalog rebuilt");

        return new
        {
            success = true,
            className = p.className,
            outputPrefabPath = outputPath,
            modelPrefabPath = modelPath,
            logs
        };
    }

    private string GenerateSyntyModelPrefab(BuildParams p, string modelPrefabPath, List<string> logs)
    {
        var dbManager = new DatabaseManager();
        try
        {
            if (dbManager.GetCurrentDbConnection() == null)
                dbManager.GetDbConnection(true);

            GameObject baseModel = Resources.Load<GameObject>("Meshes/SK_BaseModel");
            Material baseMaterial = Resources.Load<Material>("Materials/M_BaseMaterial");
            if (baseModel == null || baseMaterial == null)
                return "Could not load Synty SK_BaseModel or M_BaseMaterial from Resources";

            string modelDir = Path.GetDirectoryName(modelPrefabPath)?.Replace('\\', '/') ?? $"Assets/MyGame/Prefabs/Model/{p.className}_M";
            string baseFilename = Path.GetFileNameWithoutExtension(modelPrefabPath);
            string texDir = $"{modelDir}/Textures";
            string matDir = $"{modelDir}/Materials";
            string meshDir = $"{modelDir}/Meshes";

            Directory.CreateDirectory(modelDir);
            Directory.CreateDirectory(texDir);
            Directory.CreateDirectory(matDir);
            Directory.CreateDirectory(meshDir);

            Material workingMaterial = new Material(baseMaterial.shader);
            workingMaterial.CopyPropertiesFromMaterial(baseMaterial);

            // Clone ColorMap texture so we can paint colors onto it
            Texture2D srcColorTex = (Texture2D)baseMaterial.GetTexture(ColorMapProp);
            Texture2D writableColorTex = new Texture2D(srcColorTex.width, srcColorTex.height, TextureFormat.RGB24, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            writableColorTex.SetPixels(srcColorTex.GetPixels());
            writableColorTex.Apply();
            workingMaterial.SetTexture(ColorMapProp, writableColorTex);

            var runtime = new SidekickRuntime(baseModel, workingMaterial, null, dbManager);
            SidekickRuntime.PopulateToolData(runtime).GetAwaiter().GetResult();

            // Set BlendShapes according to workflow: Body Type -10, Size +15, Muscle +15
            runtime.BodyTypeBlendValue = p.bodyTypeBlend;
            if (p.bodySizeBlend >= 0)
            {
                runtime.BodySizeHeavyBlendValue = p.bodySizeBlend;
                runtime.BodySizeSkinnyBlendValue = 0f;
            }
            else
            {
                runtime.BodySizeHeavyBlendValue = 0f;
                runtime.BodySizeSkinnyBlendValue = -p.bodySizeBlend;
            }
            runtime.MusclesBlendValue = p.musclesBlend;

            // Part List: lay tu BuildParams hoac fallback sang bo mac dinh
            string[] partNames = (p.partNames != null && p.partNames.Length > 0)
                ? p.partNames
                : new[]
                {
                    "SK_HUMN_BASE_02_01HEAD_HU01", // Khuôn mặt nghiêm nghị, góc cạnh
                    "SK_HUMN_BASE_03_03EBRL_HU01",
                    "SK_HUMN_BASE_03_04EBRR_HU01",
                    "SK_HUMN_BASE_01_05EYEL_HU01",
                    "SK_HUMN_BASE_01_06EYER_HU01",
                    "SK_HUMN_BASE_01_07EARL_HU01",
                    "SK_HUMN_BASE_01_08EARR_HU01",
                    "SK_HUMN_BASE_02_35NOSE_HU01",
                    "SK_HUMN_BASE_01_36TETH_HU01",
                    "SK_HUMN_BASE_01_37TONG_HU01",
                    "SK_HUMN_BASE_09_09FCHR_HU01", // Râu cằm ngắn nghiêm nghị
                    "SK_HORR_VILN_01_22AHED_HU01", // Mũ tư tế / Vương miện phán quyết cổ áo đứng
                    "SK_SCFI_CIVL_09_10TORS_HU01", // Áo choàng tư tế cổ cao trang trọng
                    "SK_SCFI_CIVL_09_11AUPL_HU01",
                    "SK_SCFI_CIVL_09_12AUPR_HU01",
                    "SK_FANT_KNGT_17_13ALWL_HU01", // Giáp cẳng tay viền kim loại
                    "SK_FANT_KNGT_17_14ALWR_HU01",
                    "SK_FANT_KNGT_17_15HNDL_HU01", // Găng tay phép thuật/giáp nhẹ
                    "SK_FANT_KNGT_17_16HNDR_HU01",
                    "SK_SCFI_CIVL_09_17HIPS_HU01", // Tà áo dài / Đai lưng bản lớn
                    "SK_SCFI_CIVL_09_18LEGL_HU01", // Quần thụng đồng bộ dưới tà áo
                    "SK_SCFI_CIVL_09_19LEGR_HU01",
                    "SK_FANT_KNGT_17_20FOTL_HU01", // Giày cổ cao viền kim loại
                    "SK_FANT_KNGT_17_21FOTR_HU01",
                    "SK_SCFI_CIVL_09_24ABAC_HU01", // Áo choàng sau lưng
                    "SK_FANT_KNGT_17_25AHPF_HU01", // Đai lưng / Tà trước ấn tín
                    "SK_SCFI_CIVL_10_26AHPB_HU01", // Tà áo sau hông
                    "SK_SCFI_CIVL_10_27AHPL_HU01", // Tà áo hông trái
                    "SK_SCFI_CIVL_10_28AHPR_HU01", // Tà áo hông phải
                    "SK_SCFI_CIVL_10_29ASHL_HU01", // Cầu vai bản rộng trái
                    "SK_SCFI_CIVL_10_30ASHR_HU01"  // Cầu vai bản rộng phải
                };

            var selectedParts = new List<SidekickPart>();
            var meshesToCombine = new List<SkinnedMeshRenderer>();

            foreach (string partName in partNames)
            {
                SidekickPart skPart = SidekickPart.SearchForByName(dbManager, partName);
                if (skPart == null) continue;
                GameObject partModel = skPart.GetPartModel();
                if (partModel == null) continue;
                SkinnedMeshRenderer smr = partModel.GetComponentInChildren<SkinnedMeshRenderer>();
                if (smr != null)
                {
                    selectedParts.Add(skPart);
                    meshesToCombine.Add(smr);
                }
            }

            // Giai ma mau sac (tu palette hoac flat properties, fallback Adjudicator)
            string sSkin = !string.IsNullOrEmpty(p.palette?.skinColor) ? p.palette.skinColor : p.skinColor;
            string sHair = !string.IsNullOrEmpty(p.palette?.hairColor) ? p.palette.hairColor : p.hairColor;
            string sEye = !string.IsNullOrEmpty(p.palette?.eyeColor) ? p.palette.eyeColor : p.eyeColor;
            string sPri = !string.IsNullOrEmpty(p.palette?.primaryColor) ? p.palette.primaryColor : p.primaryColor;
            string sSec = !string.IsNullOrEmpty(p.palette?.secondaryColor) ? p.palette.secondaryColor : p.secondaryColor;
            string sTrim = !string.IsNullOrEmpty(p.palette?.trimColor) ? p.palette.trimColor : p.trimColor;
            string sMetal = !string.IsNullOrEmpty(p.palette?.metalColor) ? p.palette.metalColor : p.metalColor;
            string sJewel = !string.IsNullOrEmpty(p.palette?.jewelColor) ? p.palette.jewelColor : p.jewelColor;

            Color colSkin = HexToColor(sSkin);
            Color colHair = HexToColor(sHair);
            Color colEye = HexToColor(sEye);
            Color colPrimary = HexToColor(sPri);
            Color colSecondary = HexToColor(sSec);
            Color colTrim = HexToColor(sTrim);
            Color colMetal = HexToColor(sMetal);
            Color colJewel = HexToColor(sJewel);

            var allProps = SidekickColorProperty.GetAll(dbManager);
            var colorRows = new List<SerializedColorRow>();

            foreach (var prop in allProps)
            {
                Color targetColor = writableColorTex.GetPixel(prop.U * 2, prop.V * 2);
                string pName = (prop.Name ?? "").ToLowerInvariant();

                bool overridden = false;
                if (p.colorOverrides != null)
                {
                    foreach (var ov in p.colorOverrides)
                    {
                        if (ov != null && !string.IsNullOrEmpty(ov.propertyName) && pName.Contains(ov.propertyName.ToLowerInvariant()))
                        {
                            targetColor = HexToColor(ov.colorHex);
                            overridden = true;
                            break;
                        }
                    }
                }

                if (!overridden)
                {
                    if (prop.Group == ColorGroup.Species)
                    {
                        if (pName.Contains("skin"))
                            targetColor = colSkin;
                        else if (pName.Contains("hair") || pName.Contains("eyebrow") || pName.Contains("beard") || pName.Contains("facial"))
                            targetColor = colHair;
                        else if (pName.Contains("eye") && !pName.Contains("sclera"))
                            targetColor = colEye;
                    }
                    else if (prop.Group == ColorGroup.Outfits || prop.Group == ColorGroup.Attachments || prop.Group == ColorGroup.Materials)
                    {
                        if (pName.Contains("trim") || pName.Contains("accent") || pName.Contains("gold") || pName.Contains("brass") || pName.Contains("buckle") || pName.Contains("edge") || pName.Contains("detail") || (prop.ID % 5 == 0))
                            targetColor = colTrim;
                        else if (pName.Contains("gem") || pName.Contains("jewel") || pName.Contains("crystal") || pName.Contains("lining") || pName.Contains("cape") || pName.Contains("inner") || (prop.ID % 5 == 3))
                            targetColor = colJewel;
                        else if (pName.Contains("metal") || pName.Contains("steel") || pName.Contains("iron") || pName.Contains("silver") || (prop.ID % 5 == 4))
                            targetColor = colMetal;
                        else if (prop.ID % 2 == 0)
                            targetColor = colPrimary;
                        else
                            targetColor = colSecondary;
                    }
                }

                runtime.UpdateTexture(writableColorTex, targetColor, prop.U, prop.V);
                colorRows.Add(new SerializedColorRow
                {
                    ColorProperty = prop.ID,
                    MainColor = ColorUtility.ToHtmlStringRGB(targetColor),
                    Metallic = "545454",
                    Smoothness = "545454",
                    Reflection = "000000",
                    Emission = "000000",
                    Opacity = "FFFFFF"
                });
            }

            // Save Texture PNG
            string texPath = $"{texDir}/T_{baseFilename}ColorMap.png";
            File.WriteAllBytes(texPath, writableColorTex.EncodeToPNG());
            AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);

            TextureImporter texImporter = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (texImporter != null)
            {
                texImporter.wrapMode = TextureWrapMode.Clamp;
                texImporter.filterMode = FilterMode.Point;
                texImporter.mipmapEnabled = false;
                texImporter.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                {
                    maxTextureSize = 32,
                    resizeAlgorithm = TextureResizeAlgorithm.Bilinear,
                    format = TextureImporterFormat.RGB24
                });
                EditorUtility.SetDirty(texImporter);
                texImporter.SaveAndReimport();
            }

            // Save .sk file
            var skChar = new SerializedCharacter
            {
                Name = baseFilename,
                Species = 1,
                Parts = selectedParts.Select(sp => new SerializedPart
                {
                    Name = sp.Name,
                    PartType = sp.Type,
                    PartVersion = "1"
                }).ToList(),
                ColorSet = new SerializedColorSet
                {
                    Species = 1,
                    Name = "Custom",
                    SourceColorPath = texPath,
                    SourceMetallicPath = "Assets/Synty/Tools/SidekickCharacters/Resources/Textures/T_MetallicMap.png",
                    SourceSmoothnessPath = "Assets/Synty/Tools/SidekickCharacters/Resources/Textures/T_SmoothnessMap.png",
                    SourceReflectionPath = "Assets/Synty/Tools/SidekickCharacters/Resources/Textures/T_ReflectionMap.png",
                    SourceEmissionPath = "Assets/Synty/Tools/SidekickCharacters/Resources/Textures/T_EmissionMap.png",
                    SourceOpacityPath = "Assets/Synty/Tools/SidekickCharacters/Resources/Textures/T_OpacityMap.png"
                },
                ColorRows = colorRows,
                BlendShapes = new SerializedBlendShapeValues
                {
                    BodyTypeValue = p.bodyTypeBlend,
                    BodySizeValue = p.bodySizeBlend,
                    MuscleValue = p.musclesBlend
                }
            };
            Serializer serializer = new Serializer();
            File.WriteAllBytes($"{modelDir}/{baseFilename}.sk", Encoding.ASCII.GetBytes(serializer.Serialize(skChar)));

            // Save Material asset
            string matPath = $"{matDir}/{baseFilename}.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(matPath) != null)
                AssetDatabase.DeleteAsset(matPath);

            Material savedMat = new Material(baseMaterial.shader);
            savedMat.CopyPropertiesFromMaterial(baseMaterial);
            savedMat.SetTexture(ColorMapProp, AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            AssetDatabase.CreateAsset(savedMat, matPath);

            // Create combined character
            GameObject createdModel = runtime.CreateCharacter(
                baseFilename,
                meshesToCombine,
                true,
                true,
                null,
                true,
                true
            );

            try
            {
                SkinnedMeshRenderer smr = createdModel.GetComponentInChildren<SkinnedMeshRenderer>();
                if (smr == null || smr.sharedMesh == null)
                    return "Failed to combine SkinnedMeshRenderer on generated character";

                // Save Mesh asset
                string meshAssetPath = $"{meshDir}/{baseFilename}.asset";
                if (AssetDatabase.LoadAssetAtPath<Mesh>(meshAssetPath) != null)
                    AssetDatabase.DeleteAsset(meshAssetPath);

                Mesh meshCopy = Object.Instantiate(smr.sharedMesh);
                meshCopy.name = baseFilename;
                AssetDatabase.CreateAsset(meshCopy, meshAssetPath);
                smr.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshAssetPath) ?? meshCopy;
                smr.sharedMaterial = savedMat;
                smr.updateWhenOffscreen = true;

                // Build and save Humanoid Avatar asset
                string avatarAssetPath = $"{meshDir}/{baseFilename}-avatar.asset";
                if (AssetDatabase.LoadAssetAtPath<Avatar>(avatarAssetPath) != null)
                    AssetDatabase.DeleteAsset(avatarAssetPath);

                Animator baseAnim = baseModel.GetComponentInChildren<Animator>();
                Avatar refAvatar = baseAnim != null ? baseAnim.avatar : null;
                Avatar newAvatar = BuildModelAvatar(createdModel, refAvatar, baseFilename);
                AssetDatabase.CreateAsset(newAvatar, avatarAssetPath);

                Animator createdAnim = createdModel.GetComponent<Animator>();
                if (createdAnim == null) createdAnim = createdModel.AddComponent<Animator>();
                createdAnim.avatar = AssetDatabase.LoadAssetAtPath<Avatar>(avatarAssetPath) ?? newAvatar;

                PrefabUtility.SaveAsPrefabAsset(createdModel, modelPrefabPath);
                logs.Add($"Generated Synty Sidekick Model Prefab at {modelPrefabPath}");
            }
            finally
            {
                Object.DestroyImmediate(createdModel);
                Object.DestroyImmediate(workingMaterial);
                Object.DestroyImmediate(writableColorTex);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return null;
        }
        finally
        {
            dbManager.CloseConnection();
        }
    }

    private static Avatar BuildModelAvatar(GameObject characterRoot, Avatar referenceAvatar, string baseName)
    {
        if (referenceAvatar != null && referenceAvatar.isHuman)
        {
            HumanDescription desc = referenceAvatar.humanDescription;
            var skeleton = new List<SkeletonBone>();
            var boneNames = new HashSet<string>();

            foreach (Transform t in characterRoot.GetComponentsInChildren<Transform>(true))
            {
                bool isRoot = t == characterRoot.transform;
                boneNames.Add(t.name);
                skeleton.Add(new SkeletonBone
                {
                    name = t.name,
                    position = isRoot ? Vector3.zero : t.localPosition,
                    rotation = isRoot ? Quaternion.identity : t.localRotation,
                    scale = isRoot ? Vector3.one : t.localScale
                });
            }

            desc.skeleton = skeleton.ToArray();
            desc.human = desc.human.Where(hb => boneNames.Contains(hb.boneName)).ToArray();

            Avatar built = AvatarBuilder.BuildHumanAvatar(characterRoot, desc);
            if (built != null && built.isValid && built.isHuman)
            {
                built.name = $"{baseName}-avatar";
                return built;
            }
        }

        Avatar fallback = Object.Instantiate(referenceAvatar);
        fallback.name = $"{baseName}-avatar";
        return fallback;
    }

    private static Color HexToColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString("#" + hex.TrimStart('#'), out Color c))
            return c;
        return Color.white;
    }

    private object ValidateUnitPrefab(BuildParams p)
    {
        string path = string.IsNullOrEmpty(p.outputPrefabPath)
            ? $"Assets/MyGame/Resource/Unit/{p.className}.prefab"
            : p.outputPrefabPath;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var errors = new List<string>();
        if (prefab == null)
        {
            errors.Add($"Prefab not found: {path}");
            return new { valid = false, errors };
        }

        Transform body = prefab.transform.Find("Body");
        if (body == null) errors.Add("Missing 'Body' child transform");
        else if (Vector3.Distance(body.localScale, new Vector3(1.5f, 1.5f, 1.5f)) > 0.01f)
            errors.Add($"Body scale is {body.localScale}, expected (1.5, 1.5, 1.5)");

        var anim = body != null ? body.GetComponent<Animator>() : null;
        if (anim == null || anim.runtimeAnimatorController == null || anim.avatar == null)
            errors.Add("Animator on Body is missing Controller or Avatar");
        else if (!anim.avatar.isValid || !anim.avatar.isHuman)
            errors.Add("Animator Avatar on Body is not a valid Humanoid avatar");

        var smr = body != null ? body.GetComponentInChildren<SkinnedMeshRenderer>() : null;
        if (smr == null || smr.sharedMesh == null || smr.sharedMaterial == null)
            errors.Add("SkinnedMeshRenderer on Model is missing Mesh or Material");

        var ctrl = prefab.GetComponent<UnitController>();
        if (ctrl == null || ctrl.UnitData == null)
            errors.Add("UnitController or UnitData is null");
        else if (ctrl.UnitData.StartingSkills == null || ctrl.UnitData.StartingSkills.Count == 0)
            errors.Add("UnitData.StartingSkills is empty");

        var unitAnim = prefab.GetComponentInChildren<UnitAnimator>(true);
        if (unitAnim == null)
            errors.Add("UnitAnimator is missing");
        else
        {
            var soAnim = new SerializedObject(unitAnim);
            if (soAnim.FindProperty("_effectSpawnPoint")?.objectReferenceValue == null)
                errors.Add("UnitAnimator._effectSpawnPoint is null");
        }

        return new { valid = errors.Count == 0, prefabPath = path, errors };
    }

    private object CheckAnimation(BuildParams p)
    {
        string path = string.IsNullOrEmpty(p.outputPrefabPath)
            ? $"Assets/MyGame/Resource/Unit/{p.className}.prefab"
            : p.outputPrefabPath;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            return new { success = false, error = $"Prefab not found: {path}" };

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = new Vector3(50f, 0f, 50f);
        instance.SetActive(true);
        foreach (var smr in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            smr.updateWhenOffscreen = true;

        var logs = new List<string>();
        var clipReports = new List<object>();

        try
        {
            Transform body = instance.transform.Find("Body");
            if (body == null)
                return new { success = false, error = "Child 'Body' not found" };

            foreach (var s in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                logs.Add($"SMR: name={s.name}, enabled={s.enabled}, activeInHierarchy={s.gameObject.activeInHierarchy}, mesh={(s.sharedMesh != null ? s.sharedMesh.name : "null")}, vCount={(s.sharedMesh != null ? s.sharedMesh.vertexCount : 0)}, mat={(s.sharedMaterial != null ? s.sharedMaterial.name : "null")}, bones={(s.bones != null ? s.bones.Length : 0)}, rootBone={(s.rootBone != null ? s.rootBone.name : "null")}, bounds={s.bounds}");
            }

            var anim = body.GetComponent<Animator>();
            if (anim == null || anim.runtimeAnimatorController == null)
                return new { success = false, error = "Animator or Controller missing on Body" };

            Transform weaponTransform = null;
            Mesh weaponMesh = null;
            foreach (var mf in instance.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.name.StartsWith("Weapon_") || mf.name.Contains("Staff") || mf.name.Contains(p.className))
                {
                    weaponTransform = mf.transform;
                    weaponMesh = mf.sharedMesh;
                    break;
                }
            }

            Transform handR = FindDeepChild(body, "hand_r") ?? FindDeepChild(body, "RightHand");
            Transform propR = FindDeepChild(body, "prop_r");
            Transform index01 = FindDeepChild(body, "index_01_r");
            Transform middle01 = FindDeepChild(body, "middle_01_r");
            Transform ring01 = FindDeepChild(body, "ring_01_r");
            Transform thumb01 = FindDeepChild(body, "thumb_01_r");

            string currentParent = weaponTransform != null && weaponTransform.parent != null ? weaponTransform.parent.name : "null";
            Vector3 currentLocalPos = weaponTransform != null ? weaponTransform.localPosition : Vector3.zero;
            Vector3 currentLocalEuler = weaponTransform != null ? weaponTransform.localEulerAngles : Vector3.zero;
            Vector3 currentLocalScale = weaponTransform != null ? weaponTransform.localScale : Vector3.one;

            Bounds meshBounds = weaponMesh != null ? weaponMesh.bounds : new Bounds();

            logs.Add($"Weapon current parent: '{currentParent}'");
            logs.Add($"Weapon current localPos: ({currentLocalPos.x:F4}, {currentLocalPos.y:F4}, {currentLocalPos.z:F4})");
            logs.Add($"Weapon current localEuler: ({currentLocalEuler.x:F2}, {currentLocalEuler.y:F2}, {currentLocalEuler.z:F2})");
            logs.Add($"Weapon mesh bounds: center={meshBounds.center}, size={meshBounds.size}, min={meshBounds.min}, max={meshBounds.max}");

            Directory.CreateDirectory("Artifacts");
            GameObject camObj = new GameObject("TempAnimCamera");
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.15f, 0.16f, 0.20f, 1f);
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 100f;
            cam.fieldOfView = 45f;

            GameObject lightObj = new GameObject("TempAnimLight");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var clips = anim.runtimeAnimatorController.animationClips;
            logs.Add($"Controller has {clips.Length} animation clip(s)");

            AnimationMode.StartAnimationMode();

            Vector3 avgPalmInProp = Vector3.zero;
            int sampledCount = 0;

            foreach (var clip in clips)
            {
                if (!clip.name.Contains("Idle") && !clip.name.Contains("Attack") && !clip.name.Contains("Hammer") && !clip.name.Contains("Cast") && !clip.name.Contains("Run"))
                    continue;

                float sampleTime = (clip.name.Contains("Attack") || clip.name.Contains("Hammer")) ? clip.length * 0.4f : (clip.length > 0 ? clip.length * 0.2f : 0f);
                AnimationMode.SampleAnimationClip(body.gameObject, clip, sampleTime);

                Vector3 palmGrip = propR != null ? propR.position : handR.position;
                if (index01 != null && middle01 != null && ring01 != null && thumb01 != null)
                {
                    palmGrip = (index01.position + middle01.position + ring01.position + thumb01.position) * 0.25f;
                }

                Vector3 palmInProp = propR != null ? propR.InverseTransformPoint(palmGrip) : Vector3.zero;
                avgPalmInProp += palmInProp;
                sampledCount++;

                Vector3 staffDir = weaponTransform.up;
                Vector3 pToStaff = weaponTransform.position - palmGrip;
                Vector3 perpOffset = pToStaff - Vector3.Dot(pToStaff, staffDir) * staffDir;
                float radialDist = perpOffset.magnitude;
                float alongStaffOffset = Vector3.Dot(palmGrip - weaponTransform.position, staffDir);

                Vector3 handWorldPos = handR != null ? handR.position : instance.transform.position + Vector3.up * 1.2f;
                Vector3 weaponWorldPos = weaponTransform != null ? weaponTransform.position : Vector3.zero;

                Vector3 focusPoint = palmGrip;
                Vector3 camPos = focusPoint + new Vector3(0.5f, 0.3f, 1.8f);
                cam.transform.position = camPos;
                cam.transform.LookAt(focusPoint);

                string safeClipName = clip.name.Replace(" ", "_").Replace("@", "_");
                string shotName = $"Artifacts/anim_{safeClipName}.png";
                CaptureCamera(cam, shotName, 1024, 1024);

                // Close-up front on hand & weapon grip
                Vector3 closeCamPos = focusPoint + new Vector3(0.2f, 0.1f, 0.7f);
                cam.transform.position = closeCamPos;
                cam.transform.LookAt(focusPoint);
                string closeShotName = $"Artifacts/anim_close_{safeClipName}.png";
                CaptureCamera(cam, closeShotName, 1024, 1024);

                // Close-up side
                Vector3 sideCamPos = focusPoint + new Vector3(0.7f, 0.1f, 0.1f);
                cam.transform.position = sideCamPos;
                cam.transform.LookAt(focusPoint);
                string sideShotName = $"Artifacts/anim_side_{safeClipName}.png";
                CaptureCamera(cam, sideShotName, 1024, 1024);

                string fullBodyShotName = null;
                if (clip.name.Contains("Idle"))
                {
                    Vector3 fullBodyFocus = instance.transform.position + Vector3.up * 1.35f;
                    Vector3 fullBodyCamPos = fullBodyFocus + new Vector3(0.6f, 0.3f, 4.3f);
                    cam.transform.position = fullBodyCamPos;
                    cam.transform.LookAt(fullBodyFocus);
                    fullBodyShotName = $"Artifacts/{p.className}_Preview.png";
                    CaptureCamera(cam, fullBodyShotName, 1024, 1024);
                    logs.Add($"FullBody shot captured. Focus: {fullBodyFocus}, Cam: {fullBodyCamPos}, Body pos: {body.position}");
                }

                var eventList = clip.events != null
                    ? clip.events.Select(e => $"{e.functionName}@{e.time:F3}s({(clip.length > 0 ? (e.time / clip.length * 100f).ToString("F1") + "%" : "0%")})").ToArray()
                    : new string[0];

                clipReports.Add(new
                {
                    clip = clip.name,
                    length = clip.length,
                    events = eventList,
                    sampleTime = sampleTime,
                    palmInProp = palmInProp.ToString("F4"),
                    radialDistanceToStaff = radialDist,
                    alongStaffOffset = alongStaffOffset,
                    preview = shotName,
                    closeup = closeShotName,
                    side = sideShotName
                });
            }

            if (sampledCount > 0) avgPalmInProp /= sampledCount;
            logs.Add($"Average palm grip in prop_r space: ({avgPalmInProp.x:F4}, {avgPalmInProp.y:F4}, {avgPalmInProp.z:F4})");

            AnimationMode.StopAnimationMode();
            Object.DestroyImmediate(camObj);
            Object.DestroyImmediate(lightObj);

            return new
            {
                success = true,
                className = p.className,
                currentParent,
                currentLocalPos = new { x = currentLocalPos.x, y = currentLocalPos.y, z = currentLocalPos.z },
                currentLocalEuler = new { x = currentLocalEuler.x, y = currentLocalEuler.y, z = currentLocalEuler.z },
                meshBoundsCenter = new { x = meshBounds.center.x, y = meshBounds.center.y, z = meshBounds.center.z },
                meshBoundsSize = new { x = meshBounds.size.x, y = meshBounds.size.y, z = meshBounds.size.z },
                meshBoundsMin = new { x = meshBounds.min.x, y = meshBounds.min.y, z = meshBounds.min.z },
                meshBoundsMax = new { x = meshBounds.max.x, y = meshBounds.max.y, z = meshBounds.max.z },
                clips = clipReports,
                logs
            };
        }
        finally
        {
            if (AnimationMode.InAnimationMode())
                AnimationMode.StopAnimationMode();
            Object.DestroyImmediate(instance);
        }
    }

    private object SetWeaponTransform(BuildParams p)
    {
        string path = string.IsNullOrEmpty(p.outputPrefabPath)
            ? $"Assets/MyGame/Resource/Unit/{p.className}.prefab"
            : p.outputPrefabPath;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            return new { success = false, error = $"Prefab not found: {path}" };

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Transform body = root.transform.Find("Body");
            if (body == null)
                return new { success = false, error = "Child 'Body' not found" };

            Transform weaponTransform = null;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.name.StartsWith("Weapon_") || mf.name.Contains("Staff") || mf.name.Contains(p.className))
                {
                    weaponTransform = mf.transform;
                    break;
                }
            }

            if (weaponTransform == null)
                return new { success = false, error = "Weapon transform not found in prefab" };

            if (!string.IsNullOrEmpty(p.weaponBoneName))
            {
                Transform newBone = FindDeepChild(body, p.weaponBoneName);
                if (newBone != null && weaponTransform.parent != newBone)
                {
                    weaponTransform.SetParent(newBone, false);
                }
            }

            weaponTransform.localPosition = p.weaponLocalPos;
            weaponTransform.localEulerAngles = p.weaponLocalEuler;
            if (p.weaponLocalScale != Vector3.zero)
                weaponTransform.localScale = p.weaponLocalScale;

            // Ensure UnitAnimator._effectSpawnPoint is linked to weapon
            var unitAnim = root.GetComponentInChildren<UnitAnimator>(true);
            if (unitAnim != null)
            {
                var soAnim = new SerializedObject(unitAnim);
                var prop = soAnim.FindProperty("_effectSpawnPoint");
                if (prop != null)
                {
                    prop.objectReferenceValue = weaponTransform;
                    soAnim.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            AssetDatabase.SaveAssets();

            return new
            {
                success = true,
                message = "Updated weapon transform successfully",
                bone = weaponTransform.parent != null ? weaponTransform.parent.name : "",
                localPos = new { x = weaponTransform.localPosition.x, y = weaponTransform.localPosition.y, z = weaponTransform.localPosition.z },
                localEuler = new { x = weaponTransform.localEulerAngles.x, y = weaponTransform.localEulerAngles.y, z = weaponTransform.localEulerAngles.z },
                localScale = new { x = weaponTransform.localScale.x, y = weaponTransform.localScale.y, z = weaponTransform.localScale.z }
            };
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void CaptureCamera(Camera cam, string outputPath, int width, int height)
    {
        RenderTexture rt = new RenderTexture(width, height, 24);
        RenderTexture prev = cam.targetTexture;
        RenderTexture active = RenderTexture.active;

        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(outputPath, bytes);

        cam.targetTexture = prev;
        RenderTexture.active = active;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase))
                return child;
            Transform found = FindDeepChild(child, childName);
            if (found != null) return found;
        }
        return null;
    }

    private object SetupAnimationEvents(BuildParams p)
    {
        string path = string.IsNullOrEmpty(p.outputPrefabPath)
            ? $"Assets/MyGame/Resource/Unit/{p.className}.prefab"
            : p.outputPrefabPath;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            return new { success = false, error = $"Prefab not found: {path}" };

        Transform body = prefab.transform.Find("Body");
        var anim = body != null ? body.GetComponent<Animator>() : null;
        if (anim == null || anim.runtimeAnimatorController == null)
            return new { success = false, error = "Animator or Controller missing on Body" };

        var logs = new List<string>();
        var updatedClips = new List<object>();

        // 1. Configure Animation Events on each clip in AnimatorController
        foreach (var clip in anim.runtimeAnimatorController.animationClips)
        {
            string clipAssetPath = AssetDatabase.GetAssetPath(clip);
            if (string.IsNullOrEmpty(clipAssetPath) || !clipAssetPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                continue;

            ModelImporter importer = AssetImporter.GetAtPath(clipAssetPath) as ModelImporter;
            if (importer == null) continue;

            ModelImporterClipAnimation[] clipAnimations = importer.clipAnimations;
            if (clipAnimations == null || clipAnimations.Length == 0)
                clipAnimations = importer.defaultClipAnimations;

            bool modified = false;
            foreach (var ca in clipAnimations)
            {
                if (ca.name != clip.name) continue;

                var eventsList = new List<AnimationEvent>();

                if (ca.name.Contains("Attack") || ca.name.Contains("Hammer"))
                {
                    // Detect impact normalized time from previous events or default
                    float impactTime = 0.5f;
                    if (ca.events != null)
                    {
                        foreach (var ev in ca.events)
                        {
                            if (ev.functionName == "Hit" || ev.functionName == "AnimEvent_Impact" || ev.functionName == "AnimEvent_SkillCue")
                            {
                                impactTime = ev.time;
                                break;
                            }
                        }
                    }

                    // Add AnimEvent_Impact
                    eventsList.Add(new AnimationEvent
                    {
                        time = impactTime,
                        functionName = "AnimEvent_Impact"
                    });

                    // Add AnimEvent_AttackComplete near end (0.95f)
                    eventsList.Add(new AnimationEvent
                    {
                        time = 0.95f,
                        functionName = "AnimEvent_AttackComplete"
                    });

                    ca.events = eventsList.ToArray();
                    modified = true;
                    logs.Add($"Updated '{ca.name}' in '{clipAssetPath}': Impact at {impactTime:F3}, Complete at 0.950");
                }
                else if (ca.name.Contains("Run"))
                {
                    // Remove FootR and FootL to avoid 'has no receiver' warning on UnitAnimator
                    if (ca.events != null && ca.events.Length > 0)
                    {
                        ca.events = new AnimationEvent[0];
                        modified = true;
                        logs.Add($"Cleared footstep events from '{ca.name}' in '{clipAssetPath}'");
                    }
                }
            }

            if (modified)
            {
                importer.clipAnimations = clipAnimations;
                importer.SaveAndReimport();
                updatedClips.Add(new { clip = clip.name, assetPath = clipAssetPath });
            }
        }

        // 2. Link UnitAnimator._effectSpawnPoint if null
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var unitAnim = root.GetComponentInChildren<UnitAnimator>(true);
            if (unitAnim != null)
            {
                var soAnim = new SerializedObject(unitAnim);
                var prop = soAnim.FindProperty("_effectSpawnPoint");
                if (prop != null && prop.objectReferenceValue == null)
                {
                    Transform weaponTransform = null;
                    foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
                    {
                        if (mf.name.StartsWith("Weapon_") || mf.name.Contains("Staff") || mf.name.Contains("Axe") || mf.name.Contains("Hammer") || mf.name.Contains(p.className))
                        {
                            weaponTransform = mf.transform;
                            break;
                        }
                    }

                    if (weaponTransform != null)
                    {
                        prop.objectReferenceValue = weaponTransform;
                        soAnim.ApplyModifiedPropertiesWithoutUndo();
                        logs.Add($"Linked UnitAnimator._effectSpawnPoint to {weaponTransform.name}");
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                    }
                }
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return new
        {
            success = true,
            className = p.className,
            updatedClips,
            logs
        };
    }
}
#endif
