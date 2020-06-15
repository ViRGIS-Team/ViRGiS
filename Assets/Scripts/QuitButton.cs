using UnityEngine;

public class QuitButton : MonoBehaviour {

    public void OnClick() {
        Debug.Log("QuitButton: Application Quit");
        Application.Quit();
    }
}
