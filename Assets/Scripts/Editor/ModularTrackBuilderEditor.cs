using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;

namespace VRRoller.EditorTools
{
    /// <summary>
    /// Editor-only workflow cho ray mô-đun: tạo prefab mẫu, rải preview theo khoảng cách cung thực,
    /// và kiểm tra các điều kiện dễ làm ray hở/gãy khúc.
    /// </summary>
    [CustomEditor(typeof(ModularTrackBuilder))]
    public sealed class ModularTrackBuilderEditor : UnityEditor.Editor
    {
        const string MenuRoot = "VR Game/Track/";
        const string PrefabFolder = "Assets/Prefabs/Track";
        const string SettingsFolder = "Assets/Settings/Track";
        const string DefaultProfilePath = SettingsFolder + "/DefaultTrackModuleProfile.asset";
        const string DefaultPrefabPath = PrefabFolder + "/P_TrackModule_1m.prefab";
        const string RailMaterialPath = "Assets/Materials/M_TrackRail.mat";
        const string SleeperMaterialPath = "Assets/Materials/M_TrackSleeper.mat";
        const string SupportMaterialPath = "Assets/Materials/M_SupportPillar.mat";
        const string SupportIronMaterialPath = "Assets/Materials/M_SupportIron.mat";
        const string SupportRustMaterialPath = "Assets/Materials/M_SupportRust.mat";
        const string SupportSteelMaterialPath = "Assets/Materials/M_SupportSteel.mat";
        const string SupportBlackIronMaterialPath = "Assets/Materials/M_SupportBlackIron.mat";
        static readonly string[] SupportMaterialPaths =
        {
            SupportMaterialPath,
            SupportIronMaterialPath,
            SupportRustMaterialPath,
            SupportSteelMaterialPath,
            SupportBlackIronMaterialPath,
        };
        const string GeneratedRootName = "Track Modules (Generated)";
        const float MinVectorSqr = 0.0001f;
        const int MaxSamples = 8192;

        SerializedProperty splineContainerProperty;
        SerializedProperty profileProperty;
        SerializedProperty preferredLengthProperty;
        SerializedProperty samplesPerMeterProperty;
        SerializedProperty supportRadiusProperty;
        SerializedProperty supportMaterialProperty;
        SerializedProperty supportMaterialOptionsProperty;
        SerializedProperty supportMaterialIndexProperty;

        void OnEnable()
        {
            splineContainerProperty = serializedObject.FindProperty("splineContainer");
            profileProperty = serializedObject.FindProperty("profile");
            preferredLengthProperty = serializedObject.FindProperty("preferredModuleLengthMeters");
            samplesPerMeterProperty = serializedObject.FindProperty("samplesPerMeter");
            supportRadiusProperty = serializedObject.FindProperty("supportRadiusMeters");
            supportMaterialProperty = serializedObject.FindProperty("supportMaterial");
            supportMaterialOptionsProperty = serializedObject.FindProperty("supportMaterialOptions");
            supportMaterialIndexProperty = serializedObject.FindProperty("supportMaterialIndex");
        }

        public override void OnInspectorGUI()
        {
            var builder = (ModularTrackBuilder)target;
            EnsureSupportMaterialPalette(builder);
            serializedObject.Update();

            EditorGUILayout.PropertyField(splineContainerProperty);
            EditorGUILayout.PropertyField(profileProperty);
            EditorGUILayout.PropertyField(preferredLengthProperty);
            EditorGUILayout.PropertyField(samplesPerMeterProperty);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Support columns", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(supportRadiusProperty);
            DrawSupportMaterialLibrary(builder);
            DrawSupportMaterialPicker(builder);
            serializedObject.ApplyModifiedProperties();

            var report = Validate(builder);
            DrawReport(report);

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create / Update Default Assets"))
                    CreateOrUpdateDefaultAssets(builder);

                using (new EditorGUI.DisabledScope(!report.CanBuild))
                {
                    if (GUILayout.Button("Rebuild Preview"))
                        RebuildPreview(builder, report);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(builder.PreviewRoot == null))
                {
                    if (GUILayout.Button("Show Preview"))
                        SetPreviewVisible(builder, true);
                    if (GUILayout.Button("Hide Preview"))
                        SetPreviewVisible(builder, false);
                    if (GUILayout.Button("Clear Preview"))
                        ClearPreview(builder);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Preview chỉ dùng trong Editor và được gắn tag EditorOnly. Cart vẫn bám SplineContainer; tool này không thêm physics hoặc thay đổi CartMover.",
                MessageType.Info);

            EditorGUILayout.HelpBox(
                "Chọn một Module trong Hierarchy hoặc Scene view để hiện hai điểm cyan giữa hai rail. " +
                "Bấm một điểm, sau đó bấm Terrain hoặc Mesh Renderer trong Scene view để snap và tạo capsule support. Esc để hủy thao tác đang chọn.",
                MessageType.None);
        }

        void DrawSupportMaterialPicker(ModularTrackBuilder builder)
        {
            if (supportMaterialOptionsProperty.arraySize == 0)
            {
                EditorGUILayout.PropertyField(supportMaterialProperty, new GUIContent("Support Material"));
                EditorGUILayout.HelpBox(
                    "Bấm Create / Update Default Assets để tạo palette Iron, Rust, Steel và Black Iron.",
                    MessageType.Info);
                return;
            }

            var labels = new string[supportMaterialOptionsProperty.arraySize];
            for (var i = 0; i < labels.Length; i++)
            {
                var material = supportMaterialOptionsProperty.GetArrayElementAtIndex(i).objectReferenceValue as Material;
                labels[i] = material != null ? material.name : $"Missing material {i + 1}";
            }

            var currentIndex = Mathf.Clamp(supportMaterialIndexProperty.intValue, 0, labels.Length - 1);
            EditorGUI.BeginChangeCheck();
            var selectedIndex = EditorGUILayout.Popup("Support Material", currentIndex, labels);
            if (EditorGUI.EndChangeCheck())
            {
                supportMaterialIndexProperty.intValue = selectedIndex;
                supportMaterialProperty.objectReferenceValue =
                    supportMaterialOptionsProperty.GetArrayElementAtIndex(selectedIndex).objectReferenceValue;
                serializedObject.ApplyModifiedProperties();
                ApplySupportMaterial(builder);
                serializedObject.Update();
            }
        }

        void DrawSupportMaterialLibrary(ModularTrackBuilder builder)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(
                supportMaterialOptionsProperty,
                new GUIContent("Material Library", "Kéo-thả hoặc thêm bất kỳ Material nào vào đây để dùng cho cột support."),
                true);

            if (!EditorGUI.EndChangeCheck())
                return;

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            var options = builder.SupportMaterialOptions ?? Array.Empty<Material>();
            var selectedIndex = Array.IndexOf(options, builder.SupportMaterial);
            if (selectedIndex < 0)
            {
                for (var i = 0; i < options.Length; i++)
                {
                    if (options[i] != null)
                    {
                        selectedIndex = i;
                        break;
                    }
                }
            }

            if (selectedIndex >= 0)
            {
                Undo.RecordObject(builder, "Update Track Support Material Library");
                builder.SetSupportMaterialOptions(options, selectedIndex);
                EditorUtility.SetDirty(builder);
                ApplySupportMaterial(builder);
            }

            serializedObject.Update();
        }

        static void EnsureSupportMaterialPalette(ModularTrackBuilder builder)
        {
            if (builder.SupportMaterialOptions != null && builder.SupportMaterialOptions.Length > 0)
                return;

            var palette = new List<Material>();
            foreach (var path in SupportMaterialPaths)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material != null)
                    palette.Add(material);
            }

            if (palette.Count == 0)
                return;

            Undo.RecordObject(builder, "Assign Track Support Material Palette");
            var selectedIndex = palette.IndexOf(builder.SupportMaterial);
            builder.SetSupportMaterialOptions(palette.ToArray(), selectedIndex >= 0 ? selectedIndex : 0);
            EditorUtility.SetDirty(builder);
        }

        static void DrawReport(ValidationReport report)
        {
            if (!report.CanBuild)
            {
                EditorGUILayout.HelpBox(report.Error, MessageType.Error);
                return;
            }

            EditorGUILayout.HelpBox(
                $"Sẵn sàng: {report.TrackLengthMeters:0.##} m, {report.ModuleCount} module adaptive " +
                $"({report.ShortestModuleLengthMeters:0.###}–{report.LongestModuleLengthMeters:0.###} m), " +
                $"góc lớn nhất {report.LargestTurnAngle:0.#}°/module{report.ClosedSuffix}.",
                MessageType.Info);

            foreach (var warning in report.Warnings)
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
        }

        [MenuItem(MenuRoot + "Add Modular Track Builder To Selected Track", true)]
        static bool CanAddBuilderToSelectedTrack()
        {
            return Selection.activeGameObject != null &&
                   Selection.activeGameObject.GetComponent<SplineContainer>() != null &&
                   Selection.activeGameObject.GetComponent<ModularTrackBuilder>() == null;
        }

        [MenuItem(MenuRoot + "Add Modular Track Builder To Selected Track")]
        static void AddBuilderToSelectedTrack()
        {
            var builder = Undo.AddComponent<ModularTrackBuilder>(Selection.activeGameObject);
            Selection.activeObject = builder;
        }

        [MenuItem(MenuRoot + "Create / Update Default Track Module Assets")]
        static void CreateDefaultAssetsFromMenu()
        {
            CreateOrUpdateDefaultAssets(null);
        }

        static void CreateOrUpdateDefaultAssets(ModularTrackBuilder builder)
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(PrefabFolder);
            EnsureFolder("Assets/Settings");
            EnsureFolder(SettingsFolder);
            EnsureFolder("Assets/Materials");

            var profile = AssetDatabase.LoadAssetAtPath<TrackModuleProfile>(DefaultProfilePath);
            if (profile == null)
            {
                profile = CreateInstance<TrackModuleProfile>();
                AssetDatabase.CreateAsset(profile, DefaultProfilePath);
            }

            var railMaterial = LoadOrCreateMaterial(RailMaterialPath, new Color(0.18f, 0.2f, 0.24f));
            var sleeperMaterial = LoadOrCreateMaterial(SleeperMaterialPath, new Color(0.28f, 0.17f, 0.09f));
            var supportPalette = new[]
            {
                LoadOrCreateMaterial(SupportMaterialPath, new Color(0.48f, 0.5f, 0.54f), 0.8f, 0.55f),
                LoadOrCreateMaterial(SupportIronMaterialPath, new Color(0.18f, 0.19f, 0.2f), 0.9f, 0.35f),
                LoadOrCreateMaterial(SupportRustMaterialPath, new Color(0.38f, 0.11f, 0.035f), 0.55f, 0.22f),
                LoadOrCreateMaterial(SupportSteelMaterialPath, new Color(0.38f, 0.45f, 0.52f), 0.95f, 0.78f),
                LoadOrCreateMaterial(SupportBlackIronMaterialPath, new Color(0.035f, 0.04f, 0.045f), 0.8f, 0.18f),
            };
            var prefab = CreateOrUpdateModulePrefab(profile, railMaterial, sleeperMaterial);

            Undo.RecordObject(profile, "Assign Track Module Prefab");
            profile.SetGeneratedModulePrefab(prefab);
            EditorUtility.SetDirty(profile);

            if (builder != null)
            {
                Undo.RecordObject(builder, "Assign Track Module Profile");
                builder.SetProfile(profile);
                var materialLibrary = new List<Material>(builder.SupportMaterialOptions ?? Array.Empty<Material>());
                foreach (var material in supportPalette)
                {
                    if (material != null && !materialLibrary.Contains(material))
                        materialLibrary.Add(material);
                }

                if (builder.SupportMaterial != null && !materialLibrary.Contains(builder.SupportMaterial))
                    materialLibrary.Insert(0, builder.SupportMaterial);

                var paletteIndex = materialLibrary.IndexOf(builder.SupportMaterial);
                builder.SetSupportMaterialOptions(materialLibrary.ToArray(), paletteIndex >= 0 ? paletteIndex : 0);
                builder.SetSupportSettings(builder.SupportRadiusMeters, builder.SupportMaterial, builder.SupportsRoot);
                ApplySupportMaterial(builder);
                EditorUtility.SetDirty(builder);
                MarkSceneDirty(builder.gameObject.scene);
                Selection.activeObject = builder;
            }
            else
            {
                Selection.activeObject = profile;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Track tool: đã tạo/cập nhật {DefaultPrefabPath} và {DefaultProfilePath}.", profile);
        }

        static GameObject CreateOrUpdateModulePrefab(
            TrackModuleProfile profile,
            Material railMaterial,
            Material sleeperMaterial)
        {
            var root = new GameObject("P_TrackModule_1m");
            var length = profile.ModuleLengthMeters;
            var halfGauge = profile.RailGaugeMeters * 0.5f;

            CreateRail(root.transform, "Rail Left", -halfGauge, length, profile.RailRadiusMeters, railMaterial);
            CreateRail(root.transform, "Rail Right", halfGauge, length, profile.RailRadiusMeters, railMaterial);

            var sleeperCount = Mathf.Max(1, Mathf.RoundToInt(length / profile.SleeperSpacingMeters));
            for (var i = 0; i < sleeperCount; i++)
            {
                var normalized = (i + 0.5f) / sleeperCount;
                var z = Mathf.Lerp(-length * 0.5f, length * 0.5f, normalized);
                CreateSleeper(root.transform, $"Sleeper {i + 1:00}", z, profile.SleeperSizeMeters, sleeperMaterial);
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, DefaultPrefabPath);
            DestroyImmediate(root);
            return prefab;
        }

        static void CreateRail(
            Transform parent,
            string railName,
            float x,
            float length,
            float radius,
            Material material)
        {
            var rail = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rail.name = railName;
            rail.transform.SetParent(parent, false);
            rail.transform.localPosition = new Vector3(x, radius + 0.05f, 0f);
            rail.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            rail.transform.localScale = new Vector3(radius * 2f, length * 0.5f, radius * 2f);

            var renderer = rail.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            DestroyImmediate(rail.GetComponent<Collider>());
        }

        static void CreateSleeper(Transform parent, string sleeperName, float z, Vector3 size, Material material)
        {
            var sleeper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sleeper.name = sleeperName;
            sleeper.transform.SetParent(parent, false);
            sleeper.transform.localPosition = new Vector3(0f, 0f, z);
            sleeper.transform.localScale = size;
            sleeper.GetComponent<MeshRenderer>().sharedMaterial = material;
            DestroyImmediate(sleeper.GetComponent<Collider>());
        }

        static Material LoadOrCreateMaterial(string path, Color baseColor, float metallic = 0f, float smoothness = 0.5f)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null)
                throw new InvalidOperationException("Không tìm thấy URP Lit hoặc URP Simple Lit để tạo material ray.");

            material = new Material(shader)
            {
                name = System.IO.Path.GetFileNameWithoutExtension(path),
            };
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", baseColor);
            else if (material.HasProperty("_Color"))
                material.SetColor("_Color", baseColor);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static void RebuildPreview(ModularTrackBuilder builder, ValidationReport report)
        {
            if (!report.CanBuild)
            {
                Debug.LogError("Track tool: không thể Build Preview. " + report.Error, builder);
                return;
            }

            ClearPreview(builder);
            var root = new GameObject(GeneratedRootName).transform;
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Create Track Module Preview");
            root.SetParent(builder.transform, false);
            root.tag = "EditorOnly";

            Undo.RecordObject(builder, "Assign Track Module Preview");
            builder.SetPreviewRoot(root);

            var profile = builder.Profile;
            var distanceLookup = DistanceLookup.Create(builder.SplineContainer, builder.SamplesPerMeter);
            foreach (var segment in report.Plan.Segments)
            {
                var sample = distanceLookup.Evaluate(segment.StartDistanceMeters + segment.LengthMeters * 0.5f);
                var module = (GameObject)PrefabUtility.InstantiatePrefab(profile.ModulePrefab, root);
                module.name = $"Module {segment.Index + 1:000} ({segment.LengthMeters:0.###}m)";
                module.transform.SetPositionAndRotation(sample.Position, sample.Rotation);
                module.transform.localScale = new Vector3(1f, 1f, segment.LengthMeters / profile.ModuleLengthMeters);
                var moduleInstance = module.AddComponent<TrackModuleInstance>();
                moduleInstance.Configure(
                    builder,
                    segment.Index,
                    segment.StartDistanceMeters,
                    segment.LengthMeters,
                    profile.ModuleLengthMeters,
                    Mathf.Max(1, Mathf.RoundToInt(profile.ModuleLengthMeters / profile.SleeperSpacingMeters)),
                    profile.SleeperSizeMeters.x);
                foreach (var renderer in module.GetComponentsInChildren<MeshRenderer>())
                {
                    var part = renderer.gameObject.AddComponent<TrackModulePart>();
                    part.Configure(moduleInstance);
                }
                SetEditorOnlyRecursively(module.transform);
            }

            RefreshSupportStartPoints(builder, distanceLookup);

            EditorUtility.SetDirty(builder);
            MarkSceneDirty(builder.gameObject.scene);
            Selection.activeObject = root.gameObject;
            Debug.Log(
                $"Track tool: adaptive subdivision rải {report.ModuleCount} module " +
                $"({report.ShortestModuleLengthMeters:0.###}–{report.LongestModuleLengthMeters:0.###} m) trên {report.TrackLengthMeters:0.##} m ray.",
                builder);
        }

        static void ClearPreview(ModularTrackBuilder builder)
        {
            if (builder.PreviewRoot == null)
                return;

            var preview = builder.PreviewRoot.gameObject;
            Undo.RecordObject(builder, "Clear Track Module Preview");
            builder.SetPreviewRoot(null);
            Undo.DestroyObjectImmediate(preview);
            EditorUtility.SetDirty(builder);
            MarkSceneDirty(builder.gameObject.scene);
        }

        static void SetPreviewVisible(ModularTrackBuilder builder, bool visible)
        {
            if (builder.PreviewRoot == null)
                return;

            Undo.RecordObject(builder.PreviewRoot.gameObject, visible ? "Show Track Module Preview" : "Hide Track Module Preview");
            builder.PreviewRoot.gameObject.SetActive(visible);
            MarkSceneDirty(builder.gameObject.scene);
        }

        static void SetEditorOnlyRecursively(Transform node)
        {
            node.gameObject.tag = "EditorOnly";
            for (var i = 0; i < node.childCount; i++)
                SetEditorOnlyRecursively(node.GetChild(i));
        }

        static void RefreshSupportStartPoints(ModularTrackBuilder builder, DistanceLookup lookup)
        {
            var supports = builder.GetComponentsInChildren<TrackSupport>(true);
            foreach (var support in supports)
            {
                if (support.Builder != builder)
                    continue;

                EnsureCapsuleVisual(support);
                Undo.RecordObject(support, "Refresh Track Support");
                Undo.RecordObject(support.transform, "Refresh Track Support");
                var splineSample = lookup.Evaluate(support.AnchorDistanceMeters);
                support.ApplyStartPoint(
                    splineSample.Position + splineSample.Rotation * Vector3.right * support.LateralOffsetMeters);
                if (builder.SupportMaterial != null)
                {
                    foreach (var renderer in support.GetComponentsInChildren<MeshRenderer>())
                        renderer.sharedMaterial = builder.SupportMaterial;
                }
                EditorUtility.SetDirty(support);
            }
        }

        static void ApplySupportMaterial(ModularTrackBuilder builder)
        {
            if (builder.SupportMaterial == null)
                return;

            foreach (var support in builder.GetComponentsInChildren<TrackSupport>(true))
            {
                if (support.Builder != builder)
                    continue;

                foreach (var renderer in support.GetComponentsInChildren<MeshRenderer>())
                    renderer.sharedMaterial = builder.SupportMaterial;
            }

            MarkSceneDirty(builder.gameObject.scene);
        }

        static void EnsureCapsuleVisual(TrackSupport support)
        {
            if (support.HasCapsuleVisual)
                return;

            Undo.RecordObject(support, "Upgrade Track Support Capsule");
            var root = support.gameObject;
            var legacyFilter = root.GetComponent<MeshFilter>();
            var legacyRenderer = root.GetComponent<MeshRenderer>();
            var legacyCollider = root.GetComponent<Collider>();
            if (legacyFilter != null)
                Undo.DestroyObjectImmediate(legacyFilter);
            if (legacyRenderer != null)
                Undo.DestroyObjectImmediate(legacyRenderer);
            if (legacyCollider != null)
                Undo.DestroyObjectImmediate(legacyCollider);

            var body = CreateCapsulePart("Capsule Body", PrimitiveType.Cylinder, root.transform);
            var top = CreateCapsulePart("Capsule Top", PrimitiveType.Sphere, root.transform);
            var bottom = CreateCapsulePart("Capsule Bottom", PrimitiveType.Sphere, root.transform);
            support.SetCapsuleVisualParts(body.transform, top.transform, bottom.transform);
        }

        static GameObject CreateCapsulePart(string name, PrimitiveType primitive, Transform parent)
        {
            var part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            Undo.RegisterCreatedObjectUndo(part, "Create Track Support Capsule Part");
            part.transform.SetParent(parent, false);
            var collider = part.GetComponent<Collider>();
            if (collider != null)
                Undo.DestroyObjectImmediate(collider);
            return part;
        }

        static ValidationReport Validate(ModularTrackBuilder builder)
        {
            var report = new ValidationReport();
            if (builder.SplineContainer == null)
            {
                report.Error = "Thiếu Spline Container.";
                return report;
            }

            if (builder.Profile == null)
            {
                report.Error = "Thiếu Track Module Profile. Bấm Create / Update Default Assets để tạo cấu hình mẫu.";
                return report;
            }

            if (builder.Profile.ModulePrefab == null)
            {
                report.Error = "Profile chưa có Module Prefab. Bấm Create / Update Default Assets hoặc gán prefab riêng có trục Z là hướng chạy.";
                return report;
            }

            var spline = builder.SplineContainer.Spline;
            if (spline == null || spline.Count < (spline.Closed ? 3 : 2))
            {
                report.Error = "Spline cần ít nhất 3 knot khi Closed, hoặc 2 knot khi Open.";
                return report;
            }

            var lookup = DistanceLookup.Create(builder.SplineContainer, builder.SamplesPerMeter);
            report.TrackLengthMeters = lookup.TotalLengthMeters;
            if (report.TrackLengthMeters <= 0.001f)
            {
                report.Error = "Spline quá ngắn hoặc không có chiều dài hợp lệ.";
                return report;
            }

            report.ClosedSuffix = spline.Closed ? " (closed loop)" : " (open spline)";

            var scale = builder.transform.lossyScale;
            if (!ApproximatelyOne(scale.x) || !ApproximatelyOne(scale.y) || !ApproximatelyOne(scale.z))
            {
                report.Warnings.Add(
                    "Track đang có scale khác (1,1,1). Preview hiện dựa trên chiều dài world-space; nên Apply Scale về (1,1,1) trước khi bake/final.");
            }

            report.Plan = AdaptiveSegmentPlan.Create(
                lookup,
                builder.PreferredModuleLengthMeters,
                builder.Profile.MinimumModuleLengthMeters,
                builder.Profile.MaxTurnAnglePerModule,
                builder.SamplesPerMeter);
            report.ModuleCount = report.Plan.Segments.Count;
            report.ShortestModuleLengthMeters = report.Plan.ShortestLengthMeters;
            report.LongestModuleLengthMeters = report.Plan.LongestLengthMeters;
            report.LargestTurnAngle = report.Plan.LargestTurnAngle;

            if (builder.Profile.MinimumModuleLengthMeters > builder.PreferredModuleLengthMeters)
            {
                report.Warnings.Add(
                    "Minimum Module Length đang lớn hơn Preferred Module Length. Tool sẽ dùng Preferred Module Length làm giới hạn lớn nhất.");
            }

            if (report.Plan.MinLengthConstraintViolations > 0)
            {
                report.Warnings.Add(
                    $"Có {report.Plan.MinLengthConstraintViolations} mảnh vẫn vượt {builder.Profile.MaxTurnAnglePerModule:0.#}° " +
                    $"vì đã chạm Minimum Module Length ({builder.Profile.MinimumModuleLengthMeters:0.###} m). " +
                    "Giảm Minimum Module Length hoặc làm cua rộng hơn.");
            }

            if (report.Plan.ShortTailCount > 0)
            {
                report.Warnings.Add(
                    "Có mảnh cuối ngắn hơn Minimum Module Length để phủ kín toàn bộ spline; đây là seam của ray.");
            }

            report.CanBuild = true;
            return report;
        }

        static bool ApproximatelyOne(float value)
        {
            return Mathf.Abs(value - 1f) < 0.001f;
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            var parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
            var name = System.IO.Path.GetFileName(folder);
            if (string.IsNullOrEmpty(parent) || !AssetDatabase.IsValidFolder(parent))
                throw new InvalidOperationException($"Không tạo được folder {folder}: parent không tồn tại.");

            AssetDatabase.CreateFolder(parent, name);
        }

        static void MarkSceneDirty(Scene scene)
        {
            if (scene.IsValid() && scene.isLoaded)
                EditorSceneManager.MarkSceneDirty(scene);
        }

        sealed class ValidationReport
        {
            public bool CanBuild;
            public string Error;
            public float TrackLengthMeters;
            public int ModuleCount;
            public float ShortestModuleLengthMeters;
            public float LongestModuleLengthMeters;
            public float LargestTurnAngle;
            public string ClosedSuffix;
            public AdaptiveSegmentPlan Plan;
            public readonly List<string> Warnings = new List<string>();
        }

        readonly struct TrackSegment
        {
            public readonly int Index;
            public readonly float StartDistanceMeters;
            public readonly float LengthMeters;

            public TrackSegment(int index, float startDistanceMeters, float lengthMeters)
            {
                Index = index;
                StartDistanceMeters = startDistanceMeters;
                LengthMeters = lengthMeters;
            }
        }

        /// <summary>
        /// Chia spline theo độ cong cục bộ. Ước lượng curvature bằng tổng góc giữa các tangent:
        /// kappa ~= totalTurnRadians / segmentLength, từ đó giữ totalTurn <= maxTurnAngle.
        /// </summary>
        sealed class AdaptiveSegmentPlan
        {
            const float DistanceEpsilon = 0.0001f;
            const int BinarySearchIterations = 10;

            public readonly List<TrackSegment> Segments = new List<TrackSegment>();
            public float ShortestLengthMeters { get; private set; } = float.PositiveInfinity;
            public float LongestLengthMeters { get; private set; }
            public float LargestTurnAngle { get; private set; }
            public int MinLengthConstraintViolations { get; private set; }
            public int ShortTailCount { get; private set; }

            public static AdaptiveSegmentPlan Create(
                DistanceLookup lookup,
                float preferredLengthMeters,
                float minimumLengthMeters,
                float maxTurnAngleDegrees,
                int samplesPerMeter)
            {
                var plan = new AdaptiveSegmentPlan();
                var maximumLength = Mathf.Max(0.01f, preferredLengthMeters);
                var minimumLength = Mathf.Min(Mathf.Max(0.01f, minimumLengthMeters), maximumLength);
                var start = 0f;
                var index = 0;

                while (lookup.TotalLengthMeters - start > DistanceEpsilon)
                {
                    var remaining = lookup.TotalLengthMeters - start;
                    var candidate = Mathf.Min(maximumLength, remaining);
                    var length = plan.FindLongestAllowedLength(
                        lookup,
                        start,
                        candidate,
                        minimumLength,
                        maxTurnAngleDegrees,
                        samplesPerMeter,
                        out var minLengthWasInsufficient);

                    if (remaining < minimumLength - DistanceEpsilon)
                        plan.ShortTailCount++;
                    if (minLengthWasInsufficient)
                        plan.MinLengthConstraintViolations++;

                    var turn = CalculateTotalTurnAngle(lookup, start, length, samplesPerMeter);
                    plan.Segments.Add(new TrackSegment(index++, start, length));
                    plan.ShortestLengthMeters = Mathf.Min(plan.ShortestLengthMeters, length);
                    plan.LongestLengthMeters = Mathf.Max(plan.LongestLengthMeters, length);
                    plan.LargestTurnAngle = Mathf.Max(plan.LargestTurnAngle, turn);
                    start += length;
                }

                if (plan.Segments.Count == 0)
                    plan.ShortestLengthMeters = 0f;

                return plan;
            }

            float FindLongestAllowedLength(
                DistanceLookup lookup,
                float start,
                float candidateLength,
                float minimumLength,
                float maxTurnAngleDegrees,
                int samplesPerMeter,
                out bool minLengthWasInsufficient)
            {
                minLengthWasInsufficient = false;
                if (candidateLength <= minimumLength)
                {
                    minLengthWasInsufficient = CalculateTotalTurnAngle(lookup, start, candidateLength, samplesPerMeter) > maxTurnAngleDegrees;
                    return candidateLength;
                }

                if (CalculateTotalTurnAngle(lookup, start, candidateLength, samplesPerMeter) <= maxTurnAngleDegrees)
                    return candidateLength;

                var minimumTurn = CalculateTotalTurnAngle(lookup, start, minimumLength, samplesPerMeter);
                if (minimumTurn > maxTurnAngleDegrees)
                {
                    minLengthWasInsufficient = true;
                    return minimumLength;
                }

                var low = minimumLength;
                var high = candidateLength;
                for (var iteration = 0; iteration < BinarySearchIterations; iteration++)
                {
                    var middle = (low + high) * 0.5f;
                    if (CalculateTotalTurnAngle(lookup, start, middle, samplesPerMeter) <= maxTurnAngleDegrees)
                        low = middle;
                    else
                        high = middle;
                }

                return low;
            }

            static float CalculateTotalTurnAngle(
                DistanceLookup lookup,
                float startDistanceMeters,
                float lengthMeters,
                int samplesPerMeter)
            {
                var probeCount = Mathf.Clamp(Mathf.CeilToInt(lengthMeters * Mathf.Max(4, samplesPerMeter)), 2, 24);
                var previous = lookup.Evaluate(startDistanceMeters).Forward;
                var total = 0f;
                for (var probe = 1; probe <= probeCount; probe++)
                {
                    var distance = startDistanceMeters + lengthMeters * probe / probeCount;
                    var current = lookup.Evaluate(distance).Forward;
                    total += Vector3.Angle(previous, current);
                    previous = current;
                }

                return total;
            }
        }

        readonly struct SplineSample
        {
            public readonly Vector3 Position;
            public readonly Vector3 Forward;
            public readonly Quaternion Rotation;

            public SplineSample(Vector3 position, Vector3 forward, Quaternion rotation)
            {
                Position = position;
                Forward = forward;
                Rotation = rotation;
            }
        }

        /// <summary>
        /// Spline API nhận t chuẩn hóa, còn mảnh ray phải cách nhau theo mét.
        /// LUT dùng Evaluate world-space nên khớp cách CartMover đang đọc SplineContainer.
        /// </summary>
        sealed class DistanceLookup
        {
            readonly List<float> distances;
            readonly List<float> times;
            readonly SplineContainer container;
            readonly float totalLength;

            public float TotalLengthMeters => totalLength;

            DistanceLookup(List<float> distances, List<float> times, SplineContainer container)
            {
                this.distances = distances;
                this.times = times;
                this.container = container;
                totalLength = distances[distances.Count - 1];
            }

            public static DistanceLookup Create(SplineContainer container, int samplesPerMeter)
            {
                var estimatedLength = Mathf.Max(container.CalculateLength(), 0.001f);
                var sampleCount = Mathf.Clamp(Mathf.CeilToInt(estimatedLength * samplesPerMeter), 32, MaxSamples);
                var distances = new List<float>(sampleCount + 1) { 0f };
                var times = new List<float>(sampleCount + 1) { 0f };

                var previous = EvaluatePosition(container, 0f);
                var accumulated = 0f;
                for (var i = 1; i <= sampleCount; i++)
                {
                    var t = i / (float)sampleCount;
                    var position = EvaluatePosition(container, t);
                    accumulated += Vector3.Distance(previous, position);
                    distances.Add(accumulated);
                    times.Add(t);
                    previous = position;
                }

                return new DistanceLookup(distances, times, container);
            }

            public SplineSample Evaluate(float distance)
            {
                distance = Mathf.Clamp(distance, 0f, totalLength);
                var upper = distances.BinarySearch(distance);
                if (upper < 0)
                    upper = ~upper;

                var lower = Mathf.Max(0, upper - 1);
                upper = Mathf.Min(upper, distances.Count - 1);
                var span = distances[upper] - distances[lower];
                var fraction = span > 0.00001f ? (distance - distances[lower]) / span : 0f;
                var t = Mathf.Lerp(times[lower], times[upper], fraction);

                container.Evaluate(t, out float3 position, out float3 tangent, out float3 up);
                var forward = ((Vector3)tangent).normalized;
                if (forward.sqrMagnitude < MinVectorSqr)
                    forward = FindFallbackForward(t);

                var upVector = (Vector3)up;
                upVector -= Vector3.Project(upVector, forward);
                if (upVector.sqrMagnitude < MinVectorSqr)
                    upVector = Vector3.up - Vector3.Project(Vector3.up, forward);
                upVector.Normalize();

                return new SplineSample((Vector3)position, forward, Quaternion.LookRotation(forward, upVector));
            }

            Vector3 FindFallbackForward(float t)
            {
                const float step = 0.0005f;
                var before = EvaluatePosition(container, Mathf.Max(0f, t - step));
                var after = EvaluatePosition(container, Mathf.Min(1f, t + step));
                var forward = (after - before).normalized;
                return forward.sqrMagnitude >= MinVectorSqr ? forward : Vector3.forward;
            }

            static Vector3 EvaluatePosition(SplineContainer container, float t)
            {
                container.Evaluate(t, out float3 position, out _, out _);
                return position;
            }
        }
    }
}

namespace VRRoller.EditorTools
{
    /// <summary>Scene View UI của một module preview: hai anchor ở tâm giữa ray trái/phải.</summary>
    [CustomEditor(typeof(TrackModuleInstance))]
    public sealed class TrackModuleInstanceEditor : UnityEditor.Editor
    {
        void OnSceneGUI()
        {
            var module = (TrackModuleInstance)target;
            DrawSupportHandles(module);
        }

        internal static void DrawSupportHandles(TrackModuleInstance module)
        {
            if (module == null || module.Builder == null || module.Builder.Profile == null)
                return;

            for (var anchorIndex = 0; anchorIndex < module.SupportAnchorCount; anchorIndex++)
            {
                var position = module.GetSupportAnchorWorldPosition(anchorIndex);
                var size = HandleUtility.GetHandleSize(position) * 0.09f;
                Handles.color = TrackSupportPlacementSession.IsPendingFor(module, anchorIndex)
                    ? Color.yellow
                    : new Color(0.1f, 0.9f, 1f, 1f);

                if (Handles.Button(position, Quaternion.identity, size, size, Handles.SphereHandleCap))
                {
                    TrackSupportPlacementSession.Begin(
                        module,
                        anchorIndex,
                        position,
                        module.GetSupportAnchorDistanceMeters(anchorIndex),
                        module.GetSupportAnchorLateralOffsetMeters(anchorIndex));
                }

                Handles.Label(position + Vector3.up * size * 1.5f, module.GetSupportAnchorLabel(anchorIndex));
            }
        }

        public override void OnInspectorGUI()
        {
            var module = (TrackModuleInstance)target;
            EditorGUILayout.HelpBox(
                "Mỗi sleeper có hai điểm cyan ở hai đầu. Bấm một điểm, rồi bấm Terrain hoặc Mesh Renderer để snap điểm chân trụ.",
                MessageType.Info);
            EditorGUILayout.LabelField("Segment", (module.SegmentIndex + 1).ToString());
            EditorGUILayout.LabelField("Length", $"{module.LengthMeters:0.###} m");
        }
    }

    /// <summary>Cho phép click trực tiếp Rail Left/Rail Right hoặc tà vẹt để hiển thị anchor của module cha.</summary>
    [CustomEditor(typeof(TrackModulePart))]
    public sealed class TrackModulePartEditor : UnityEditor.Editor
    {
        void OnSceneGUI()
        {
            TrackModuleInstanceEditor.DrawSupportHandles(((TrackModulePart)target).Module);
        }

        public override void OnInspectorGUI()
        {
            var part = (TrackModulePart)target;
            EditorGUILayout.HelpBox(
                "Đây là một phần của module ray. Hai điểm cyan ở giữa hai rail thuộc module cha; bấm một điểm để đặt trụ.",
                MessageType.Info);
            if (part.Module != null && GUILayout.Button("Select Module Root"))
                Selection.activeGameObject = part.Module.gameObject;
        }
    }

    /// <summary>Endpoint handle cho phép chọn lại điểm snap của một trụ đã tạo.</summary>
    [CustomEditor(typeof(TrackSupport))]
    public sealed class TrackSupportEditor : UnityEditor.Editor
    {
        void OnSceneGUI()
        {
            var support = (TrackSupport)target;
            var endpoint = support.EndPoint;
            var size = HandleUtility.GetHandleSize(endpoint) * 0.1f;
            Handles.color = TrackSupportPlacementSession.IsEditing(support)
                ? Color.yellow
                : new Color(1f, 0.58f, 0.08f, 1f);

            if (Handles.Button(endpoint, Quaternion.identity, size, size, Handles.CubeHandleCap))
                TrackSupportPlacementSession.BeginEdit(support);

            Handles.Label(endpoint + Vector3.up * size * 1.5f, "Edit support end");
        }

        public override void OnInspectorGUI()
        {
            var support = (TrackSupport)target;
            EditorGUILayout.HelpBox(
                "Bấm handle cam tại chân trụ trong Scene View để snap lại endpoint. Capsule sẽ preview chiều dài mới trước khi click xác nhận.",
                MessageType.Info);
            if (GUILayout.Button("Edit End Point"))
                TrackSupportPlacementSession.BeginEdit(support);
        }
    }

    /// <summary>
    /// Session tạm thời sau khi người dùng chọn anchor. Nó raycast collider trước (TerrainCollider được hỗ trợ),
    /// sau đó raycast trực tiếp tất cả MeshFilter để vẫn snap được object không có collider.
    /// </summary>
    [InitializeOnLoad]
    static class TrackSupportPlacementSession
    {
        sealed class PendingSupport
        {
            public TrackModuleInstance Module;
            public int AnchorIndex;
            public Vector3 StartPosition;
            public float AnchorDistanceMeters;
            public float LateralOffsetMeters;
            public TrackSupport EditedSupport;
            public Vector3 OriginalEndPoint;
            public bool IsArmed;
        }

        static PendingSupport pending;
        static bool hasHoverPoint;
        static Vector3 hoverPoint;

        static TrackSupportPlacementSession()
        {
            SceneView.duringSceneGui += OnSceneGui;
        }

        public static bool IsPendingFor(TrackModuleInstance module, int anchorIndex)
        {
            return pending != null && pending.Module == module && pending.AnchorIndex == anchorIndex;
        }

        public static bool IsEditing(TrackSupport support)
        {
            return pending != null && pending.EditedSupport == support;
        }

        public static void Begin(
            TrackModuleInstance module,
            int anchorIndex,
            Vector3 startPosition,
            float anchorDistanceMeters,
            float lateralOffsetMeters)
        {
            pending = new PendingSupport
            {
                Module = module,
                AnchorIndex = anchorIndex,
                StartPosition = startPosition,
                AnchorDistanceMeters = anchorDistanceMeters,
                LateralOffsetMeters = lateralOffsetMeters,
            };
            hasHoverPoint = false;
            ArmOnNextEditorUpdate(pending);
        }

        public static void BeginEdit(TrackSupport support)
        {
            if (support == null || support.Builder == null)
                return;

            pending = new PendingSupport
            {
                Module = null,
                StartPosition = support.StartPoint,
                AnchorDistanceMeters = support.AnchorDistanceMeters,
                LateralOffsetMeters = support.LateralOffsetMeters,
                EditedSupport = support,
                OriginalEndPoint = support.EndPoint,
            };
            hasHoverPoint = false;
            ArmOnNextEditorUpdate(pending);
        }

        static void ArmOnNextEditorUpdate(PendingSupport session)
        {
            EditorApplication.delayCall += () =>
            {
                if (pending == session)
                    pending.IsArmed = true;
            };
        }

        static void OnSceneGui(SceneView sceneView)
        {
            if (pending == null)
                return;

            if (!pending.IsArmed)
                return;

            var builder = pending.EditedSupport != null
                ? pending.EditedSupport.Builder
                : pending.Module != null ? pending.Module.Builder : null;
            if (builder == null)
            {
                Cancel(true);
                return;
            }

            var currentEvent = Event.current;
            if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.Escape)
            {
                Cancel();
                currentEvent.Use();
                return;
            }

            if (currentEvent.type == EventType.MouseMove ||
                currentEvent.type == EventType.MouseDrag ||
                currentEvent.type == EventType.MouseDown)
            {
                var ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
                hasHoverPoint = TryFindSnapPoint(ray, out hoverPoint);
                if (pending.EditedSupport != null)
                    pending.EditedSupport.PreviewEndPoint(hasHoverPoint ? hoverPoint : pending.OriginalEndPoint);
            }

            if (currentEvent.type == EventType.Layout)
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            if (currentEvent.type == EventType.Repaint)
            {
                var fallbackPoint = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition).GetPoint(8f);
                var targetPoint = hasHoverPoint ? hoverPoint : fallbackPoint;
                Handles.color = hasHoverPoint ? Color.green : Color.red;
                Handles.DrawDottedLine(pending.StartPosition, targetPoint, 5f);
                Handles.SphereHandleCap(
                    0,
                    targetPoint,
                    Quaternion.identity,
                    HandleUtility.GetHandleSize(targetPoint) * 0.07f,
                    EventType.Repaint);
                Handles.Label(
                    pending.StartPosition + Vector3.up * HandleUtility.GetHandleSize(pending.StartPosition) * 0.2f,
                    hasHoverPoint
                        ? "Click điểm xanh để snap chân trụ (Esc: hủy)"
                        : "Rê chuột lên Terrain/Mesh Renderer (Esc: hủy)");
            }

            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && !currentEvent.alt)
            {
                if (hasHoverPoint)
                    CommitSupport(hoverPoint);
                else
                    Debug.LogWarning("Track support: không raycast được Terrain hoặc Mesh Renderer tại vị trí đã bấm.");

                currentEvent.Use();
            }

        }

        static void CommitSupport(Vector3 endPoint)
        {
            if (pending.EditedSupport != null)
            {
                var editedSupport = pending.EditedSupport;
                Undo.RecordObject(editedSupport, "Move Track Support Endpoint");
                Undo.RecordObject(editedSupport.transform, "Move Track Support Endpoint");
                editedSupport.CommitEndPoint(endPoint);
                EditorUtility.SetDirty(editedSupport);
                EditorSceneManager.MarkSceneDirty(editedSupport.gameObject.scene);
                Selection.activeGameObject = editedSupport.gameObject;
                Cancel(false);
                return;
            }

            var builder = pending.Module.Builder;
            var root = EnsureSupportsRoot(builder);
            var supportObject = new GameObject($"Support {pending.Module.SegmentIndex + 1:000}");
            Undo.RegisterCreatedObjectUndo(supportObject, "Create Track Support");
            supportObject.transform.SetParent(root, true);
            supportObject.isStatic = true;

            var support = supportObject.AddComponent<TrackSupport>();
            Undo.RecordObject(support, "Configure Track Support");
            CreateCapsuleVisual(support);
            support.Configure(
                builder,
                pending.AnchorDistanceMeters,
                pending.StartPosition,
                endPoint,
                pending.LateralOffsetMeters,
                builder.SupportRadiusMeters);

            if (builder.SupportMaterial != null)
            {
                foreach (var renderer in supportObject.GetComponentsInChildren<MeshRenderer>())
                    renderer.sharedMaterial = builder.SupportMaterial;
            }

            EditorUtility.SetDirty(support);
            EditorSceneManager.MarkSceneDirty(builder.gameObject.scene);
            Selection.activeGameObject = supportObject;
            Cancel(false);
        }

        static void CreateCapsuleVisual(TrackSupport support)
        {
            var body = CreateCapsulePart("Capsule Body", PrimitiveType.Cylinder, support.transform);
            var top = CreateCapsulePart("Capsule Top", PrimitiveType.Sphere, support.transform);
            var bottom = CreateCapsulePart("Capsule Bottom", PrimitiveType.Sphere, support.transform);
            support.SetCapsuleVisualParts(body.transform, top.transform, bottom.transform);
        }

        static GameObject CreateCapsulePart(string name, PrimitiveType primitive, Transform parent)
        {
            var part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            Undo.RegisterCreatedObjectUndo(part, "Create Track Support Capsule Part");
            part.transform.SetParent(parent, false);
            var collider = part.GetComponent<Collider>();
            if (collider != null)
                Undo.DestroyObjectImmediate(collider);
            return part;
        }

        static Transform EnsureSupportsRoot(ModularTrackBuilder builder)
        {
            if (builder.SupportsRoot != null)
                return builder.SupportsRoot;

            var root = new GameObject("Track Supports").transform;
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Create Track Supports Root");
            root.SetParent(builder.transform, false);
            root.gameObject.isStatic = true;

            Undo.RecordObject(builder, "Assign Track Supports Root");
            builder.SetSupportSettings(builder.SupportRadiusMeters, builder.SupportMaterial, root);
            EditorUtility.SetDirty(builder);
            return root;
        }

        static bool TryFindSnapPoint(Ray ray, out Vector3 point)
        {
            var closestDistance = float.PositiveInfinity;
            point = default;

            foreach (var hit in Physics.RaycastAll(ray, Mathf.Infinity, ~0, QueryTriggerInteraction.Ignore))
            {
                if (IsAllowedSnapTarget(hit.collider.gameObject) && hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    point = hit.point;
                }
            }

            // Đây chỉ chạy khi có event chuột, không phải mỗi Repaint; nhờ đó mesh không collider vẫn snap được
            // mà không gọi API Scene View có thể gây render lồng nhau.
            foreach (var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (filter.sharedMesh == null || !IsAllowedSnapTarget(filter.gameObject))
                    continue;

                if (TryIntersectMesh(ray, filter.sharedMesh, filter.transform.localToWorldMatrix, out var meshPoint, out var meshDistance) &&
                    meshDistance < closestDistance)
                {
                    closestDistance = meshDistance;
                    point = meshPoint;
                }
            }

            return closestDistance < float.PositiveInfinity;
        }

        /// <summary>Möller–Trumbore raycast trên một MeshFilter, dùng khi target không có collider.</summary>
        static bool TryIntersectMesh(
            Ray worldRay,
            Mesh mesh,
            Matrix4x4 localToWorld,
            out Vector3 worldPoint,
            out float worldDistance)
        {
            const float epsilon = 0.000001f;
            worldPoint = default;
            worldDistance = float.PositiveInfinity;

            var worldToLocal = localToWorld.inverse;
            var localOrigin = worldToLocal.MultiplyPoint(worldRay.origin);
            var localDirection = worldToLocal.MultiplyVector(worldRay.direction).normalized;
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            var found = false;

            for (var triangle = 0; triangle < triangles.Length; triangle += 3)
            {
                var a = vertices[triangles[triangle]];
                var b = vertices[triangles[triangle + 1]];
                var c = vertices[triangles[triangle + 2]];
                var edge1 = b - a;
                var edge2 = c - a;
                var p = Vector3.Cross(localDirection, edge2);
                var determinant = Vector3.Dot(edge1, p);
                if (Mathf.Abs(determinant) < epsilon)
                    continue;

                var inverseDeterminant = 1f / determinant;
                var offset = localOrigin - a;
                var u = Vector3.Dot(offset, p) * inverseDeterminant;
                if (u < 0f || u > 1f)
                    continue;

                var q = Vector3.Cross(offset, edge1);
                var v = Vector3.Dot(localDirection, q) * inverseDeterminant;
                if (v < 0f || u + v > 1f)
                    continue;

                var localDistance = Vector3.Dot(edge2, q) * inverseDeterminant;
                if (localDistance < epsilon)
                    continue;

                var candidateWorldPoint = localToWorld.MultiplyPoint(localOrigin + localDirection * localDistance);
                var candidateWorldDistance = Vector3.Distance(worldRay.origin, candidateWorldPoint);
                if (candidateWorldDistance >= worldDistance)
                    continue;

                worldDistance = candidateWorldDistance;
                worldPoint = candidateWorldPoint;
                found = true;
            }

            return found;
        }

        static bool IsAllowedSnapTarget(GameObject target)
        {
            if (!target.activeInHierarchy || target.CompareTag("EditorOnly"))
                return false;

            // Không cho support tự snap vào module preview hoặc support khác.
            return target.GetComponentInParent<TrackSupport>() == null &&
                   target.GetComponentInParent<TrackModuleInstance>() == null;
        }

        static void Cancel(bool restoreEditedSupport = true)
        {
            if (restoreEditedSupport && pending != null && pending.EditedSupport != null)
                pending.EditedSupport.ApplyStartPoint(pending.EditedSupport.StartPoint);
            pending = null;
            hasHoverPoint = false;
        }
    }
}
