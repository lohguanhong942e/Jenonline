#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Vesperhold.EditorTools
{
    // Android and iOS builds. Menu: Vesperhold > Build Android (.aab for Google Play), Build Android (.apk) or Prepare iOS.
    // The signing key (Android) and the Apple team (iOS) are yours: create a keystore in Project Settings > Player >
    // Publishing Settings, and pick your team in Xcode. Set the bundle id below before the first store upload.
    public static class MobileBuild
    {
        const string BundleId = "com.yourname.vesperhold";     // change me
        const string ScenePath = "Assets/Scenes/Main.unity";

        static void Common()
        {
            if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
            if (!File.Exists(ScenePath)) { var sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); EditorSceneManager.SaveScene(sc, ScenePath); AssetDatabase.Refresh(); }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.productName = "Vesperhold";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false; PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true; PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.colorSpace = ColorSpace.Gamma;
        }

        static void Android()
        {
            Common();
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleId);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.High);
            PlayerSettings.Android.useCustomKeystore = false;     // switch to your own keystore for Play uploads
        }

        [MenuItem("Vesperhold/Build Android (.aab for Google Play)")]
        public static void BuildAab() { Android(); EditorUserBuildSettings.buildAppBundle = true; Run(BuildTarget.Android, "Builds/Android/Vesperhold.aab"); }

        [MenuItem("Vesperhold/Build Android (.apk to sideload)")]
        public static void BuildApk() { Android(); EditorUserBuildSettings.buildAppBundle = false; Run(BuildTarget.Android, "Builds/Android/Vesperhold.apk"); }

        [MenuItem("Vesperhold/Prepare iOS (Xcode project)")]
        public static void BuildIos()
        {
            Common();
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
            PlayerSettings.iOS.targetOSVersionString = "13.0";
            PlayerSettings.iOS.requiresFullScreen = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            Run(BuildTarget.iOS, "Builds/iOS");
        }

        static void Run(BuildTarget target, string path)
        {
            var opts = new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = path, target = target, options = BuildOptions.None };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("Vesperhold " + target + " build: " + report.summary.result + " -> " + path);
        }
    }
}
#endif
