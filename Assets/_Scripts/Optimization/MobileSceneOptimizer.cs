using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MobileSceneOptimizer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        OptimizeLoadedScene(SceneManager.GetActiveScene());
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        OptimizeLoadedScene(scene);
    }

    private static void OptimizeLoadedScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Animator animator in root.GetComponentsInChildren<Animator>(true))
                animator.cullingMode = GetSafeCullingMode(animator);

            foreach (ParticleSystem particleSystem in root.GetComponentsInChildren<ParticleSystem>(true))
                OptimizeParticleSystem(particleSystem);

            foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                canvas.pixelPerfect = false;
                canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;
            }

            foreach (GraphicRaycaster raycaster in root.GetComponentsInChildren<GraphicRaycaster>(true))
            {
                if (!HasInteractiveHandlers(raycaster.transform))
                    raycaster.enabled = false;
            }
        }
    }

    private static AnimatorCullingMode GetSafeCullingMode(Animator animator)
    {
        // Animators that drive gameplay, VFX or objects without renderers must keep
        // updating even when Unity cannot determine their visibility.
        if (animator.GetComponentInParent<PlayerController>() != null ||
            animator.GetComponentInChildren<Renderer>(true) == null ||
            animator.GetComponentInChildren<ParticleSystem>(true) != null)
            return AnimatorCullingMode.AlwaysAnimate;

        return AnimatorCullingMode.CullUpdateTransforms;
    }

    internal static void OptimizeParticleSystem(ParticleSystem particleSystem)
    {
        ParticleSystem.MainModule main = particleSystem.main;
        if (main.maxParticles > 256)
            main.maxParticles = 256;

        ParticleSystem.CollisionModule collision = particleSystem.collision;
        if (collision.enabled)
        {
            collision.quality = ParticleSystemCollisionQuality.Low;
            collision.maxCollisionShapes = Mathf.Min(collision.maxCollisionShapes, 32);
        }

        ParticleSystemRenderer particleRenderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        if (particleRenderer == null)
            return;

        particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
        particleRenderer.receiveShadows = false;
        particleRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        particleRenderer.lightProbeUsage = LightProbeUsage.Off;
        particleRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        particleRenderer.allowOcclusionWhenDynamic = true;
    }

    private static bool HasInteractiveHandlers(Transform root)
    {
        if (root.GetComponentInChildren<Selectable>(true) != null || root.GetComponentInChildren<EventTrigger>(true) != null)
            return true;

        MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null)
                continue;

            Type type = behaviour.GetType();
            if (typeof(IPointerClickHandler).IsAssignableFrom(type) ||
                typeof(IPointerDownHandler).IsAssignableFrom(type) ||
                typeof(IPointerUpHandler).IsAssignableFrom(type) ||
                typeof(IBeginDragHandler).IsAssignableFrom(type) ||
                typeof(IDragHandler).IsAssignableFrom(type) ||
                typeof(IEndDragHandler).IsAssignableFrom(type))
                return true;
        }

        return false;
    }
}
