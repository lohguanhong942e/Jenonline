using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Vesperhold
{
    // 2D lights and post-processing. Everything here is skipped when the project is not using URP,
    // so the game still plays (just flatter) on the built-in pipeline.
    public static class Lighting
    {
        public static bool Urp { get { return GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset; } }

        public static Light2D Global(Transform parent, float intensity, Color color)
        {
            if (!Urp) return null;
            var go = new GameObject("GlobalLight"); go.transform.SetParent(parent, false);
            var l = go.AddComponent<Light2D>(); l.lightType = Light2D.LightType.Global; l.intensity = intensity; l.color = color;
            return l;
        }

        public static Light2D Point(Transform parent, Vector3 localPos, float radius, Color color, float intensity)
        {
            if (!Urp) return null;
            var go = new GameObject("PointLight"); go.transform.SetParent(parent, false); go.transform.localPosition = localPos;
            var l = go.AddComponent<Light2D>(); l.lightType = Light2D.LightType.Point;
            l.pointLightOuterRadius = radius; l.pointLightInnerRadius = radius * 0.15f; l.intensity = intensity; l.color = color;
            return l;
        }
    }

    public static class PostFx
    {
        // Bloom makes the additive skill sprites glow; a light vignette and contrast give the dark-fantasy mood.
        public static void Setup(Camera cam)
        {
            if (!Lighting.Urp || cam == null) return;
            var go = new GameObject("PostFx");
            var vol = go.AddComponent<Volume>(); vol.isGlobal = true;
            var prof = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = prof.Add<Bloom>(true); bloom.threshold.Override(0.85f); bloom.intensity.Override(UiScale.Mobile ? 0.7f : 1.0f); bloom.scatter.Override(0.65f);
            if (!UiScale.Mobile) { var vig = prof.Add<Vignette>(true); vig.intensity.Override(0.3f); vig.smoothness.Override(0.6f); }
            var ca = prof.Add<ColorAdjustments>(true); ca.contrast.Override(10f); ca.saturation.Override(10f);
            vol.profile = prof;
            var data = cam.GetUniversalAdditionalCameraData(); data.renderPostProcessing = true;
        }
    }
}
