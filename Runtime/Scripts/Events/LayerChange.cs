using UniRx;
using System;
using Project;

namespace Virgis {

    public class LayerChange {

        private readonly Subject<IVirgisLayer> _layerEvent = new Subject<IVirgisLayer>();

        public void AddLayer(IVirgisLayer layer) {
            _layerEvent.OnNext(layer);
        }

        public IObservable<IVirgisLayer> Event {
            get {
                return _layerEvent.AsObservable();
            }
        }

    }

}
