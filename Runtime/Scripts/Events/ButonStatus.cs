
using UniRx;
using System;

namespace Virgis {

    public class ButtonStatus {

        private bool _isLhTrigger = false;
        private bool _isRhTrigger = false;
        private bool _isLhGrip = false;
        private bool _isRhGrip = false;

        public bool isLhTrigger {
            get {
                return _isLhTrigger;
            }
            set {
                _isLhTrigger = value;
                SelectionType = SelectionType.INFO;
                activate = value;
                _buttonEvent.OnNext(this);
            }
        }
        public bool isRhTrigger {
            get {
                return _isRhTrigger;
            }
            set {
                _isRhTrigger = value;
                SelectionType = SelectionType.SELECT;
                activate = value;
                _buttonEvent.OnNext(this);
            }
        }

        public bool isLhGrip {
            get {
                return _isLhGrip;
            }
            set {
                _isLhGrip = value;
                SelectionType = SelectionType.SELECTALL;
                activate = value;
                _buttonEvent.OnNext(this);
            }
        }
        public bool isRhGrip {
            get {
                return _isRhGrip;
            }
            set {
                _isRhGrip = value;
                SelectionType = SelectionType.SELECTALL;
                activate = value;
                _buttonEvent.OnNext(this);
            }
        }

        public bool isAxisEdit {
            get {
                return _isLhGrip && _isRhGrip;
            }
        }

        public bool activate = false;

        public SelectionType SelectionType {
            get; private set;
        }

        private readonly Subject<ButtonStatus> _buttonEvent = new Subject<ButtonStatus>();

        public IObservable<ButtonStatus> Event {
            get {
                return _buttonEvent.AsObservable();
            }
        }

    }

}
