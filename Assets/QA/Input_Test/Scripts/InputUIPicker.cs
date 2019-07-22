using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class InputUIPicker : MonoBehaviour
{
    public Dropdown m_deviceDropdown;
    public Dropdown m_otherDropdown;

    [Header("Device Test GameObject")]
    public GameObject m_windowsKeyboardMouse;
    public GameObject m_macKeyboardMouse;
    public GameObject m_controllerDiagram;
    public GameObject m_xboxController;
    public GameObject m_dualShockController;
    public GameObject m_joystick;
    public GameObject m_pen;
    public GameObject m_touch;

    [Header("Other Test GameObject")]
    public GameObject m_interactions;
    public GameObject m_processors;

    // Current displayed diagram
    private GameObject m_currentDisplay;

    void Start()
    {
        if (Save.DeviceValue == 0 && Save.OtherValue == 0)
            SwitchToKeyMouse();
        else
        {
            SwitchToDeviceTest(Save.DeviceValue);
            SwitchToOtherTest(Save.OtherValue);
        }
    }

    void Update()
    {
        // Only Shortcut for New ISX
        if (InputSystem.GetDevice<Keyboard>() == null) return;

        Keyboard currentKeyboard = InputSystem.GetDevice<Keyboard>();
        if (currentKeyboard.leftCtrlKey.isPressed || currentKeyboard.rightCtrlKey.isPressed)
        {
            if (currentKeyboard.digit1Key.wasPressedThisFrame)
                m_deviceDropdown.value = 1;
            else if (currentKeyboard.digit2Key.wasPressedThisFrame)
                m_deviceDropdown.value = 2;
            else if (currentKeyboard.digit3Key.wasPressedThisFrame)
                m_deviceDropdown.value = 3;
            else if (currentKeyboard.digit4Key.wasPressedThisFrame)
                m_deviceDropdown.value = 4;
            else if (currentKeyboard.digit5Key.wasPressedThisFrame)
                m_deviceDropdown.value = 5;
            else if (currentKeyboard.digit6Key.wasPressedThisFrame)
                m_deviceDropdown.value = 6;
            else if (currentKeyboard.digit7Key.wasPressedThisFrame)
                m_deviceDropdown.value = 7;
        }
        if (currentKeyboard.leftShiftKey.isPressed || currentKeyboard.rightShiftKey.isPressed)
        {
            if (currentKeyboard.digit1Key.wasPressedThisFrame)
                m_otherDropdown.value = 1;
            else if (currentKeyboard.digit2Key.wasPressedThisFrame)
                m_otherDropdown.value = 2;
        }
    }

    public void SwitchToDeviceTest(int value)
    {
        switch (value)
        {
            case 1:
                SwitchToKeyMouse();
                break;
            case 2:
                SwitchToTestObject(m_xboxController);
                break;
            case 3:
                SwitchToTestObject(m_dualShockController);
                break;
            case 4:
                SwitchToTestObject(m_controllerDiagram);
                break;
            case 5:
                SwitchToTestObject(m_joystick);
                break;
            case 6:
                SwitchToTestObject(m_pen);
                break;
            case 7:
                SwitchToTestObject(m_touch);
                break;
            default:
                break;
        }
        m_otherDropdown.value = 0;
        Save.DeviceValue = value;
    }

    public void SwitchToOtherTest(int value)
    {
        if (value == 1)
            SwitchToTestObject(m_interactions);
        else if (value == 2)
            SwitchToTestObject(m_processors);
        m_deviceDropdown.value = 0;
        Save.OtherValue = value;
    }

    private void SwitchToKeyMouse()
    {
#if (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IOS )
        SwitchToTestObject(m_macKeyboardMouse);
#else
        SwitchToTestObject(m_windowsKeyboardMouse);
#endif
    }

    private void SwitchToTestObject(GameObject newDiagram)
    {
        if (m_currentDisplay != newDiagram)
        {
            if (m_currentDisplay != null)
                m_currentDisplay.SetActive(false);
            m_currentDisplay = newDiagram;
            m_currentDisplay.SetActive(true);
        }
    }
}
