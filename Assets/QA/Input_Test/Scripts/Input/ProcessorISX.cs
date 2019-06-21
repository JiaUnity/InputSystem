﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class ProcessorISX : MonoBehaviour
{
    [Header("The Input Action with Processors")]
    public InputAction m_inputAction;

    [Header("UI element for more info")]
    public Text m_originalText;
    public Text m_resultText;

    // Start is called before the first frame update
    void Start()
    {
        m_inputAction.Rename(gameObject.name);
    }

    private void OnEnable()
    {
        m_inputAction.Enable();
    }

    private void OnDisable()
    {
        m_inputAction?.Disable();
    }
}
