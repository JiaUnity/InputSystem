using UnityEngine;

public class Save : MonoBehaviour
{
    private static Save _instance;
    public static Save Instance { get { return _instance; } }

    public static int DeviceValue { get; set; }
    public static int OtherValue { get; set; }

    private void Awake()
    {
        if (_instance != null && _instance != this)
            Destroy(gameObject);
        else
        {
            DontDestroyOnLoad(gameObject);
            _instance = this;
        }        
    }
}
