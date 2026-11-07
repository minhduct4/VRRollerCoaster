using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager.UI;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

namespace VRRoller.EditorTools
{
    /// <summary>
    /// T1.2–T1.4: bật OpenXR, import XR Device Simulator, tạo scene Main.
    /// Chạy một lần bằng -executeMethod hoặc menu VR Roller.
    /// </summary>
    public static class Phase1Setup
    {
        const string OpenXrLoader = "UnityEngine.XR.OpenXR.OpenXRLoader";

        public static void Run()
        {
            try
            {
                ConfigurePlayerSettings();
                EnsureFolders();
                EnableOpenXr(BuildTargetGroup.Standalone);
                EnableOpenXr(BuildTargetGroup.Android);
                ConfigureXrStartupOnPlay();
                EnableOpenXrFeatures(BuildTargetGroup.Standalone, questSupport: false);
                EnableOpenXrFeatures(BuildTargetGroup.Android, questSupport: true);
                ImportDeviceSimulatorSample();
                CreateMainScene();
                AssetDatabase.SaveAssets();
                Debug.Log("Phase1Setup completed.");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("Phase1Setup failed: " + exception);
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("VR Roller/Setup Platform (Phase 1)")]
        static void RunFromMenu()
        {
            ConfigurePlayerSettings();
            EnsureFolders();
            EnableOpenXr(BuildTargetGroup.Standalone);
            EnableOpenXr(BuildTargetGroup.Android);
            ConfigureXrStartupOnPlay();
            EnableOpenXrFeatures(BuildTargetGroup.Standalone, questSupport: false);
            EnableOpenXrFeatures(BuildTargetGroup.Android, questSupport: true);
            ImportDeviceSimulatorSample();
            CreateMainScene();
            AssetDatabase.SaveAssets();
            Debug.Log("Phase1Setup completed.");
        }

        static void ConfigurePlayerSettings()
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
        }

        static void EnsureFolders()
        {
            CreateFolder("Assets", "Scripts");
            CreateFolder("Assets", "Prefabs");
            CreateFolder("Assets", "Materials");
            CreateFolder("Assets", "Audio");
            CreateFolder("Assets", "Settings");
            CreateFolder("Assets", "Scenes");
            CreateFolder("Assets", "XR");
        }

        static void CreateFolder(string parent, string name)
        {
            var path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }

        static void EnableOpenXr(BuildTargetGroup group)
        {
            if (XRPackageMetadataStore.IsLoaderAssigned(OpenXrLoader, group))
            {
                Debug.Log("OpenXR already assigned for " + group);
                return;
            }

            var settings = GetOrCreateSettings(group);
            var assigned = XRPackageMetadataStore.AssignLoader(settings.Manager, OpenXrLoader, group);
            if (!assigned)
                throw new InvalidOperationException("Could not assign OpenXR loader for " + group);

            Debug.Log("Assigned OpenXR loader for " + group);
        }

        /// <summary>
        /// PC không gắn headset: tránh OpenXR init lúc Play (XR_ERROR_FORM_FACTOR_UNAVAILABLE).
        /// Android vẫn init khi chạy APK trên Quest.
        /// </summary>
        static void ConfigureXrStartupOnPlay()
        {
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget perTarget);
            if (perTarget == null)
                return;

            var standalone = perTarget.SettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (standalone != null)
            {
                standalone.InitManagerOnStart = false;
                EditorUtility.SetDirty(standalone);
            }

            var android = perTarget.SettingsForBuildTarget(BuildTargetGroup.Android);
            if (android != null)
            {
                android.InitManagerOnStart = true;
                EditorUtility.SetDirty(android);
            }

            EditorUtility.SetDirty(perTarget);
            Debug.Log("XR Init on Start: Standalone=off, Android=on.");
        }

        static XRGeneralSettings GetOrCreateSettings(BuildTargetGroup group)
        {
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget perTarget);
            if (perTarget == null)
            {
                perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perTarget, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
            }

            var settings = perTarget.SettingsForBuildTarget(group);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<XRGeneralSettings>();
                settings.name = group + " Settings";
                var manager = ScriptableObject.CreateInstance<XRManagerSettings>();
                manager.name = group + " Providers";
                settings.Manager = manager;
                AssetDatabase.AddObjectToAsset(settings, perTarget);
                AssetDatabase.AddObjectToAsset(manager, perTarget);
                perTarget.SetSettingsForBuildTarget(group, settings);
                EditorUtility.SetDirty(perTarget);
            }
            else if (settings.Manager == null)
            {
                var manager = ScriptableObject.CreateInstance<XRManagerSettings>();
                manager.name = group + " Providers";
                AssetDatabase.AddObjectToAsset(manager, perTarget);
                settings.Manager = manager;
                EditorUtility.SetDirty(settings);
            }

            AssetDatabase.SaveAssets();
            return settings;
        }

        static void EnableOpenXrFeatures(BuildTargetGroup group, bool questSupport)
        {
            RefreshFeatures(group);
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
            if (settings == null)
                throw new InvalidOperationException("OpenXR settings missing for " + group);

            var touch = settings.GetFeature<OculusTouchControllerProfile>();
            if (touch == null)
                throw new InvalidOperationException("Oculus Touch Controller Profile missing for " + group);

            touch.enabled = true;
            EditorUtility.SetDirty(touch);

            if (questSupport)
            {
                var quest = settings.GetFeature<MetaQuestFeature>();
                if (quest == null)
                    throw new InvalidOperationException("Meta Quest Support feature missing for Android.");

                quest.enabled = true;
                EditorUtility.SetDirty(quest);
            }

            EditorUtility.SetDirty(settings);
            Debug.Log("OpenXR features configured for " + group);
        }

        static void RefreshFeatures(BuildTargetGroup group)
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(item => item.GetName().Name == "Unity.XR.OpenXR.Editor");
            var helpers = assembly?.GetType("UnityEditor.XR.OpenXR.Features.FeatureHelpers");
            var method = helpers?.GetMethods()
                .FirstOrDefault(item => item.Name == "RefreshFeatures" && item.GetParameters().Length == 1);
            if (method == null)
            {
                Debug.LogWarning("FeatureHelpers.RefreshFeatures was not found.");
                return;
            }

            method.Invoke(null, new object[] { group });
        }

        static void ImportDeviceSimulatorSample()
        {
            var package = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages()
                .FirstOrDefault(item => item.name == "com.unity.xr.interaction.toolkit");
            if (package == null)
                throw new InvalidOperationException("XR Interaction Toolkit is not installed.");

            var samples = Sample.FindByPackage(package.name, package.version).ToList();
            foreach (var sample in samples)
                Debug.Log("XRI sample: " + sample.displayName + " imported=" + sample.isImported);

            var simulator = samples.FirstOrDefault(sample =>
                sample.displayName != null &&
                sample.displayName.IndexOf("Device Simulator", StringComparison.OrdinalIgnoreCase) >= 0);
            if (string.IsNullOrEmpty(simulator.displayName))
                throw new InvalidOperationException("XR Device Simulator sample was not found.");

            if (!simulator.isImported)
                simulator.Import();

            Debug.Log("XR Device Simulator sample ready.");
        }

        static void CreateMainScene()
        {
            const string scenePath = "Assets/Scenes/Main.unity";
            if (!File.Exists(scenePath))
            {
                var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                    UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
                    UnityEditor.SceneManagement.NewSceneMode.Single);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(scenePath, true)
            };
            Debug.Log("Build scene set to " + scenePath);
        }
    }
}
