using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniversalMobileController;
public class ButtonEventHandler : MonoBehaviour
{
    SpecialButton SpButton;
    [SerializeField] GameObject VisiableIcone;
    public static ButtonEventHandler instance;

    private void Awake()
    {
        instance = this;
    }

    public void Start()
    {
        SpButton= GetComponent<SpecialButton>();
        VisiableIcone.SetActive(false);
    }

    public void RemoveListener()
    {
        if( SpButton != null ) 
        SpButton.onButtondown.RemoveAllListeners();
    }

    public void SetListener(Action action)
    {
        if (SpButton != null)
            SpButton.onButtondown.AddListener(action.Invoke);
    }

    public void Enable()
    {
        VisiableIcone.SetActive(true);
    }
    public void Disable()
    {
        VisiableIcone.SetActive(false);
    }
}
