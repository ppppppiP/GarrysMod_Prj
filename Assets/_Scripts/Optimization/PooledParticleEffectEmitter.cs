using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PooledParticleEffectEmitter : MonoBehaviour
{
    private sealed class PooledEffect
    {
        public GameObject prefab;
        public GameObject instance;
        public float releaseTime;
    }

    [SerializeField] private GameObject effectPrefab;
    [SerializeField] private Material overrideMaterial;
    [SerializeField] private Vector3 localPosition;
    [SerializeField] private Vector3 localEulerAngles;
    [SerializeField] private Vector3 localScale = Vector3.one;
    [SerializeField, Min(0.1f)] private float visibilityScale = 1.6f;
    [SerializeField, Range(1, 16)] private int maxPoolSize = 8;
    [SerializeField, Range(0, 4)] private int prewarmCount = 2;

    private static readonly List<PooledEffect> Effects = new List<PooledEffect>();
    private static PooledParticleEffectRunner runner;

    public GameObject EffectPrefab { get => effectPrefab; set => effectPrefab = value; }
    public Material OverrideMaterial { get => overrideMaterial; set => overrideMaterial = value; }
    public Vector3 LocalPosition { get => localPosition; set => localPosition = value; }
    public Vector3 LocalEulerAngles { get => localEulerAngles; set => localEulerAngles = value; }
    public Vector3 LocalScale { get => localScale; set => localScale = value; }
    public float VisibilityScale { get => visibilityScale; set => visibilityScale = Mathf.Max(0.1f, value); }
    public int MaxPoolSize { get => maxPoolSize; set => maxPoolSize = Mathf.Clamp(value, 1, 16); }
    public int PrewarmCount { get => prewarmCount; set => prewarmCount = Mathf.Clamp(value, 0, 4); }

    private void OnEnable()
    {
        if (effectPrefab != null && prewarmCount > 0)
            EnsureCapacity(effectPrefab, overrideMaterial, Mathf.Min(prewarmCount, maxPoolSize));
    }

    public void Play()
    {
        if (effectPrefab == null)
            return;

        EnsureRunner();
        PooledEffect effect = GetAvailableEffect();
        if (effect == null || effect.instance == null)
            return;

        Transform effectTransform = effect.instance.transform;
        effect.instance.SetActive(false);
        effectTransform.position = transform.TransformPoint(localPosition);
        effectTransform.rotation = transform.rotation * Quaternion.Euler(localEulerAngles);
        effectTransform.localScale = Vector3.Scale(transform.lossyScale, localScale) * Mathf.Max(0.1f, visibilityScale);
        effect.instance.SetActive(true);

        float lifetime = 0.1f;
        foreach (ParticleSystem particleSystem in effect.instance.GetComponentsInChildren<ParticleSystem>(true))
        {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Play(true);

            ParticleSystem.MainModule main = particleSystem.main;
            float systemLifetime = main.duration + main.startDelay.constantMax + main.startLifetime.constantMax;
            lifetime = Mathf.Max(lifetime, systemLifetime);
        }

        effect.releaseTime = Time.time + lifetime;
    }

    private PooledEffect GetAvailableEffect()
    {
        int matchingCount = 0;
        PooledEffect oldest = null;

        for (int i = Effects.Count - 1; i >= 0; i--)
        {
            PooledEffect effect = Effects[i];
            if (effect == null || effect.instance == null)
            {
                Effects.RemoveAt(i);
                continue;
            }

            if (effect.prefab != effectPrefab)
                continue;

            matchingCount++;
            if (!effect.instance.activeSelf)
                return effect;

            if (oldest == null || effect.releaseTime < oldest.releaseTime)
                oldest = effect;
        }

        if (matchingCount < maxPoolSize)
            return CreateEffect(effectPrefab, overrideMaterial);

        return oldest;
    }

    private static void EnsureCapacity(GameObject prefab, Material material, int capacity)
    {
        EnsureRunner();
        int existing = 0;
        for (int i = Effects.Count - 1; i >= 0; i--)
        {
            if (Effects[i] == null || Effects[i].instance == null)
            {
                Effects.RemoveAt(i);
                continue;
            }

            if (Effects[i].prefab == prefab)
                existing++;
        }

        while (existing < capacity)
        {
            CreateEffect(prefab, material);
            existing++;
        }
    }

    private static PooledEffect CreateEffect(GameObject prefab, Material material)
    {
        EnsureRunner();
        GameObject instance = Object.Instantiate(prefab, runner.transform);
        instance.name = prefab.name + " (Pooled)";
        foreach (ParticleSystem particleSystem in instance.GetComponentsInChildren<ParticleSystem>(true))
        {
            MobileSceneOptimizer.OptimizeParticleSystem(particleSystem);
            ParticleSystem.MainModule main = particleSystem.main;
            main.startLifetimeMultiplier *= 2.5f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.78f, 0.12f, 1f));

            ParticleSystemRenderer particleRenderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            if (particleRenderer != null)
            {
                if (material != null)
                    particleRenderer.sharedMaterial = material;
                particleRenderer.sortingOrder = Mathf.Max(particleRenderer.sortingOrder, 20);
            }
        }
        instance.SetActive(false);

        PooledEffect effect = new PooledEffect
        {
            prefab = prefab,
            instance = instance,
            releaseTime = 0f
        };
        Effects.Add(effect);
        return effect;
    }

    private static void EnsureRunner()
    {
        if (runner != null)
            return;

        GameObject runnerObject = new GameObject("Pooled Particle Effects");
        if (Application.isPlaying)
            Object.DontDestroyOnLoad(runnerObject);
        runner = runnerObject.AddComponent<PooledParticleEffectRunner>();
    }

    internal static void Tick()
    {
        float currentTime = Time.time;
        for (int i = Effects.Count - 1; i >= 0; i--)
        {
            PooledEffect effect = Effects[i];
            if (effect == null || effect.instance == null)
            {
                Effects.RemoveAt(i);
                continue;
            }

            if (effect.instance.activeSelf && currentTime >= effect.releaseTime)
                effect.instance.SetActive(false);
        }
    }
}

internal sealed class PooledParticleEffectRunner : MonoBehaviour
{
    private void Update()
    {
        PooledParticleEffectEmitter.Tick();
    }
}
