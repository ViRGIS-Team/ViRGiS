using UnityEngine;
using UnityEngine.Events;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System;

namespace Virgis {

    /// <summary>
    /// Event type for Zoom Change Events
    /// </summary>
    public class ZoomEvent  {

        private float _scale;

        private readonly Subject<float> _zoomEvent = new Subject<float>();


        public IObservable<float> Event {
            get {
                return _zoomEvent.AsObservable();
            }
        }


        public void Set(float zoom) {
            if (zoom != 0) {
                AppState.instance.map.transform.localScale = Vector3.one / zoom;
                _scale = AppState.instance.map.transform.InverseTransformVector(Vector3.right).magnitude;
                _zoomEvent.OnNext(_scale);
            }
        }

        public float Get() {
            return _scale;
        }
    }
}
