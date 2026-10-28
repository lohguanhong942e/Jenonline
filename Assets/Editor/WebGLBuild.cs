#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Vesperhold.EditorTools
{
    // One click (or one command line) WebGL build: creates the empty start scene the first time (the game
    // builds itself at runtime), applies size and compatibility settings, and writes Builds/WebGL.
    //   Unity -batchmode -quit -projectPath . -executeMethod Vesperhold.EditorTools.WebGLBuild.Build
    public static class WebGLBuild
    {
        [MenuItem("Vesperhold/Build WebGL")]
        public static void Build()
        {
            const string scenePath = "Assets/Scenes/Main.unity";
            if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
            if (!File.Exists(scenePath))
            {
                var sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(sc, scenePath);
                AssetDatabase.Refresh();
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };

            PlayerSettings.productName = "Vesperhold";
            PlayerSettings.colorSpace = ColorSpace.Gamma;                                   // widest mobile browser support
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;                              // works on hosts that cannot set Content-Encoding
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
            PlayerSettings.stripEngineCode = true;

            var opts = new BuildPlayerOptions { scenes = new[] { scenePath }, locationPathName = "Builds/WebGL", target = BuildTarget.WebGL, options = BuildOptions.None };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("Vesperhold WebGL build: " + report.summary.result + ", " + (report.summary.totalSize / (1024 * 1024)) + " MB -> Builds/WebGL");
        }
    }
}
#endif
