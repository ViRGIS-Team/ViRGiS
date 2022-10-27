/* MIT License

Copyright (c) 2020 - 21 Runette Software

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice (and subsidiary notices) shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. */

using UnityEngine;
using UnityEngine.Events;
using UniRx;
using System;

namespace Virgis {

    /// <summary>
    /// Event type for Zoom Change Events
    /// </summary>
    public class ZoomEvent  {

        private float _scale = 1;

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
