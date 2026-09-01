using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LightweightTransformAnimation : MonoBehaviour
{
    [Serializable]
    public sealed class Track
    {
        public Transform target;
        public Vector3 basePosition;
        public Vector3 baseEulerAngles;
        public Vector3 baseScale = Vector3.one;
        public AnimationCurve positionX;
        public AnimationCurve positionY;
        public AnimationCurve positionZ;
        public AnimationCurve rotationX;
        public AnimationCurve rotationY;
        public AnimationCurve rotationZ;
        public AnimationCurve rotationW;
        public AnimationCurve scaleX;
        public AnimationCurve scaleY;
        public AnimationCurve scaleZ;

        public void Apply(float time)
        {
            if (target == null)
                return;

            bool hasPositionX = HasKeys(positionX);
            bool hasPositionY = HasKeys(positionY);
            bool hasPositionZ = HasKeys(positionZ);
            if (hasPositionX || hasPositionY || hasPositionZ)
            {
                Vector3 value = basePosition;
                if (hasPositionX) value.x = positionX.Evaluate(time);
                if (hasPositionY) value.y = positionY.Evaluate(time);
                if (hasPositionZ) value.z = positionZ.Evaluate(time);
                target.localPosition = value;
            }

            bool hasRotationX = HasKeys(rotationX);
            bool hasRotationY = HasKeys(rotationY);
            bool hasRotationZ = HasKeys(rotationZ);
            bool hasRotationW = HasKeys(rotationW);
            if (hasRotationW)
            {
                Quaternion value = new Quaternion(
                    hasRotationX ? rotationX.Evaluate(time) : 0f,
                    hasRotationY ? rotationY.Evaluate(time) : 0f,
                    hasRotationZ ? rotationZ.Evaluate(time) : 0f,
                    rotationW.Evaluate(time));
                target.localRotation = Normalize(value);
            }
            else if (hasRotationX || hasRotationY || hasRotationZ)
            {
                Vector3 value = baseEulerAngles;
                if (hasRotationX) value.x = rotationX.Evaluate(time);
                if (hasRotationY) value.y = rotationY.Evaluate(time);
                if (hasRotationZ) value.z = rotationZ.Evaluate(time);
                target.localEulerAngles = value;
            }

            bool hasScaleX = HasKeys(scaleX);
            bool hasScaleY = HasKeys(scaleY);
            bool hasScaleZ = HasKeys(scaleZ);
            if (hasScaleX || hasScaleY || hasScaleZ)
            {
                Vector3 value = baseScale;
                if (hasScaleX) value.x = scaleX.Evaluate(time);
                if (hasScaleY) value.y = scaleY.Evaluate(time);
                if (hasScaleZ) value.z = scaleZ.Evaluate(time);
                target.localScale = value;
            }
        }

        private static bool HasKeys(AnimationCurve curve)
        {
            return curve != null && curve.length > 0;
        }

        private static Quaternion Normalize(Quaternion value)
        {
            float magnitude = Mathf.Sqrt(value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w);
            if (magnitude < 0.0001f)
                return Quaternion.identity;

            float inverse = 1f / magnitude;
            return new Quaternion(value.x * inverse, value.y * inverse, value.z * inverse, value.w * inverse);
        }
    }

    [SerializeField, Min(0.001f)] private float duration = 1f;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private float speed = 1f;
    [SerializeField] private List<Track> tracks = new List<Track>();

    private float time;
    private bool playing;

    public float Duration { get => duration; set => duration = Mathf.Max(0.001f, value); }
    public bool Loop { get => loop; set => loop = value; }
    public bool PlayOnEnable { get => playOnEnable; set => playOnEnable = value; }
    public List<Track> Tracks => tracks;

    private void OnEnable()
    {
        LightweightAnimationScheduler.Register(this);
        if (playOnEnable)
            Play();
    }

    private void OnDisable()
    {
        LightweightAnimationScheduler.Unregister(this);
    }

    public void Play()
    {
        time = 0f;
        playing = true;
        Apply(0f);
    }

    public void Stop(bool resetToStart = false)
    {
        playing = false;
        if (resetToStart)
        {
            time = 0f;
            Apply(0f);
        }
    }

    internal void Tick(float deltaTime)
    {
        if (!playing || duration <= 0f)
            return;

        time += deltaTime * speed;
        if (loop)
        {
            time = Mathf.Repeat(time, duration);
        }
        else if (time >= duration)
        {
            time = duration;
            playing = false;
        }

        Apply(time);
    }

    private void Apply(float sampleTime)
    {
        for (int i = 0; i < tracks.Count; i++)
            tracks[i].Apply(sampleTime);
    }
}

[DefaultExecutionOrder(-1000)]
internal sealed class LightweightAnimationScheduler : MonoBehaviour
{
    private const float TickInterval = 1f / 30f;
    private static readonly List<LightweightTransformAnimation> Animations = new List<LightweightTransformAnimation>();
    private static readonly List<LightweightRotator> Rotators = new List<LightweightRotator>();
    private static LightweightAnimationScheduler instance;
    private float accumulatedTime;

    internal static void Register(LightweightTransformAnimation animation)
    {
        EnsureInstance();
        if (!Animations.Contains(animation))
            Animations.Add(animation);
    }

    internal static void Unregister(LightweightTransformAnimation animation)
    {
        Animations.Remove(animation);
    }

    internal static void Register(LightweightRotator rotator)
    {
        EnsureInstance();
        if (!Rotators.Contains(rotator))
            Rotators.Add(rotator);
    }

    internal static void Unregister(LightweightRotator rotator)
    {
        Rotators.Remove(rotator);
    }

    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        GameObject runner = new GameObject("Lightweight Animation Scheduler");
        DontDestroyOnLoad(runner);
        instance = runner.AddComponent<LightweightAnimationScheduler>();
    }

    private void Update()
    {
        accumulatedTime += Time.deltaTime;
        if (accumulatedTime < TickInterval)
            return;

        float delta = accumulatedTime;
        accumulatedTime = 0f;

        for (int i = Animations.Count - 1; i >= 0; i--)
        {
            LightweightTransformAnimation animation = Animations[i];
            if (animation == null)
            {
                Animations.RemoveAt(i);
                continue;
            }

            if (animation.isActiveAndEnabled)
                animation.Tick(delta);
        }

        for (int i = Rotators.Count - 1; i >= 0; i--)
        {
            LightweightRotator rotator = Rotators[i];
            if (rotator == null)
            {
                Rotators.RemoveAt(i);
                continue;
            }

            if (rotator.isActiveAndEnabled)
                rotator.Tick(delta);
        }
    }
}
