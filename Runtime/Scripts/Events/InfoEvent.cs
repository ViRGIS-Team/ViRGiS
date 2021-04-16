using UniRx;
using System;

namespace Virgis {

    public class InfoEvent {

        private readonly Subject<string> _infoEvent = new Subject<string>();

        public void Set(string info) {
            _infoEvent.OnNext(info);
        }

        public IObservable<string> Event {
            get {
                return _infoEvent.AsObservable();
            }
        }

    }

}
