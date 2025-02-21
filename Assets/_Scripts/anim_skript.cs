using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationsController : MonoBehaviour
{
    [SerializeField] Animator anim;
    private float _fadeSpeed = 0.2f;
    public void SetAnimFadeSpeed(float fadeSpeed)
    {
        _fadeSpeed = fadeSpeed;
    }

    public void SetAnimByName(string animName)
    {
        anim.CrossFade(animName, _fadeSpeed);
    }
}
