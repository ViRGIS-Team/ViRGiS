
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
