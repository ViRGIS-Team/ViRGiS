using UnityEngine;

namespace virgis {

    public class OcculusAdapter : MonoBehaviour {

        // Update is called once per frame
        void Update() {
            OVRInput.Update();
        }

        private void FixedUpdate() {
            OVRInput.FixedUpdate();
        }
    }
}
