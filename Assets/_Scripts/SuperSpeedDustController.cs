using UnityEngine;

/// <summary>
/// Emits small dust bursts while the player runs on the ground with super speed.
/// The authored particle systems are reused and kept disabled between bursts.
/// </summary>
public sealed class SuperSpeedDustController : MonoBehaviour
{
    [SerializeField, Min(0.05f)] private float emissionInterval = 0.12f;
    [SerializeField, Range(1, 8)] private int particlesPerSystem = 2;

    private PlayerController player;
    private BaffController buffs;
    private ParticleSystem[] particleSystems;
    private float nextEmissionTime;
    private bool wasEmitting;

    private void Awake()
    {
        player = GetComponentInParent<PlayerController>();
        buffs = GetComponentInParent<BaffController>();
        particleSystems = GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem particles in particleSystems)
        {
            ParticleSystem.MainModule main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = Mathf.Min(main.maxParticles, 24);

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = false;

            ParticleSystem.LightsModule lights = particles.lights;
            lights.enabled = false;

            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void Update()
    {
        bool hasMoveInput = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f ||
                            Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f;
        bool shouldEmit = player != null && buffs != null &&
                          buffs.IsSuperSpeedActive && player.isGrounded && hasMoveInput;

        if (!shouldEmit)
        {
            if (wasEmitting)
            {
                foreach (ParticleSystem particles in particleSystems)
                    particles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }

            wasEmitting = false;
            return;
        }

        if (!wasEmitting)
        {
            foreach (ParticleSystem particles in particleSystems)
                particles.Play(false);

            nextEmissionTime = 0f;
            wasEmitting = true;
        }

        if (Time.time < nextEmissionTime)
            return;

        foreach (ParticleSystem particles in particleSystems)
            particles.Emit(particles.name.Contains("dust") ? particlesPerSystem : 1);

        nextEmissionTime = Time.time + emissionInterval;
    }
}
