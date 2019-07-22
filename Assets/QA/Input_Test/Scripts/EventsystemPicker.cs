using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class EventsystemPicker : MonoBehaviour
{
    [Header("Event Systems")]
    public StandaloneInputModule m_oldSystem;
    public InputSystemUIInputModule m_newSystem;

    [Header("Toggles")]
    public Toggle m_oldToggle;
    public Toggle m_newToggles;    
        

    // Start is called before the first frame update
    void Start()
    {
        m_oldToggle.onValueChanged.AddListener(delegate { OnToggleChanged(); });
        m_newToggles.isOn = true;
    }

    void Update()
    {
        // Only Shortcut for New ISX
        Keyboard currentKeyboard = InputSystem.GetDevice<Keyboard>();
        if (currentKeyboard != null && currentKeyboard.tabKey.isPressed)
        {
            if (currentKeyboard.tabKey.wasPressedThisFrame)
            {
                if (m_oldToggle.isOn)
                    m_newToggles.isOn = true;
                else
                    m_oldToggle.isOn = true;
            }            
        }            
    }

    public void OnToggleChanged()
    {
        m_oldSystem.enabled = m_oldToggle.isOn;
        m_newSystem.enabled = m_newToggles.isOn;        
    }
}
