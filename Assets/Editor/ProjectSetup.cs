#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Vesperhold.EditorTools
{
    // Creates a URP 2D renderer asset the first time the project opens, so lights and bloom work
    // without any manual pipeline setup. Skipped when the project already has a render pipeline.
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        static ProjectSetup() { EditorApplication.delayCall += Run; }

        [MenuItem("Vesperhold/Set up URP 2D")]
        static void Run()
        {
            if (GraphicsSettings.defaultRenderPipeline != null) return;
            if (!AssetDatabase.IsValidFolder("Assets/Settings")) AssetDatabase.CreateFolder("Assets", "Settings");
            var data = ScriptableObject.CreateInstance<Renderer2DData>();
            AssetDatabase.CreateAsset(data, "Assets/Settings/Vesperhold2DRenderer.asset");
            var pipe = UniversalRenderPipelineAsset.Create(data);
            AssetDatabase.CreateAsset(pipe, "Assets/Settings/VesperholdURP.asset");
            GraphicsSettings.defaultRenderPipeline = pipe;
            for (int i = 0; i < QualitySettings.names.Length; i++) { QualitySettings.SetQualityLevel(i); QualitySettings.renderPipeline = pipe; }
            AssetDatabase.SaveAssets();
            Debug.Log("Vesperhold: created URP 2D renderer and assigned it as the default pipeline.");
        }
    }
}
#endif
