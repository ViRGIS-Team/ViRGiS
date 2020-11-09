using System.Reactive.Subjects;
using System.Reactive.Linq;
using System;
using UnityEngine;

namespace Virgis {

    public class OrientEvent {

        private readonly Subject<Vector3> _orientEvent = new Subject<Vector3>();

        public void Set(Vector3 orientation) {
            _orientEvent.OnNext(orientation);
        }

        public IObservable<Vector3> Event {
            get {
                return _orientEvent.AsObservable();
            }
        }

    }

}