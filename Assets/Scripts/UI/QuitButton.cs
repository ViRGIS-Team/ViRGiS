using System.Threading.Tasks;
using UnityEngine;

namespace Virgis {
    public class QuitButton : MonoBehaviour {

        public async void OnClick() {
            Debug.Log("QuitButton.OnClick save before quit");
            MapInitialize mi = AppState.instance.map.GetComponent<MapInitialize>();
            await mi.Save(false);
            Debug.Log("QuitButton.OnClick now quit");
            Application.Quit();
        }
    }
}
