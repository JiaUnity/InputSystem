using UnityEngine;
using UnityEngine.SceneManagement;

public class RefreshScene : MonoBehaviour
{
    public void OnClick()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
