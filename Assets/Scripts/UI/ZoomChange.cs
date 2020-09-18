using UnityEngine;
using UnityEngine.Events;

namespace Virgis {

    /// <summary>
    /// Event type for Zoom Change Events
    /// </summary>
    public class ZoomEvent : UnityEvent<float> {

        private float _scale;

        public ZoomEvent() {
            _scale = 1f;
            Invoke(_scale);
        }

        public void AddZoomChangeListener(UnityAction<float> action) {
            AddListener(action);
        }

        public void Change(float zoom) {
            if (zoom != 0) {
                AppState.instance.map.transform.localScale = Vector3.one / zoom;
                _scale = AppState.instance.map.transform.InverseTransformVector(Vector3.right).magnitude;
                Invoke(_scale);
            }
        }

        public float GetScale() {
            return _scale;
        }
    }
}
