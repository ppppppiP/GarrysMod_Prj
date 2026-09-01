using UnityEngine;

public class AnimationsController : MonoBehaviour
{
    [SerializeField] Animator anim;
    [SerializeField] LightweightTransformAnimation lightweightAnimation;
    private float _fadeSpeed = 0.2f;

    private void Awake()
    {
        if (anim == null)
            anim = GetComponent<Animator>();

        if (lightweightAnimation == null)
            lightweightAnimation = GetComponent<LightweightTransformAnimation>();
    }

    public void SetAnimFadeSpeed(float fadeSpeed)
    {
        _fadeSpeed = fadeSpeed;
    }

    public void SetAnimByName(string animName)
    {
        if (lightweightAnimation != null)
        {
            lightweightAnimation.Play();
            return;
        }

        if (anim != null)
            anim.CrossFade(animName, _fadeSpeed);
    }
}
