
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

        private bool m_isLhTrigger = false;
        private bool m_isRhTrigger = false;
        private bool m_isLhGrip = false;
        private bool m_isRhGrip = false;

        public bool isLhTrigger {
            get {
                return m_isLhTrigger;
            }
            set {
                m_isLhTrigger = value;
                SelectionType = SelectionType.INFO;
                activate = value;
                _buttonEvent.OnNext(this);
            }
        }
        public bool isRhTrigger {
            get {
                return m_isRhTrigger;
            }
            set {
                m_isRhTrigger = value;
                SelectionType = SelectionType.SELECT;
                activate = value;
                _buttonEvent.OnNext(this);
            }
        }

        public bool isLhGrip {
            get {
                return m_isLhGrip;
            }
            set {
                m_isLhGrip = value;
                activate = value;
                if (value) {
                    if (m_isRhGrip)
                        SelectionType = SelectionType.MOVEAXIS;
                } else {
                    if (m_isRhGrip) {
                        activate = true;
                        SelectionType = SelectionType.SELECTALL;
                    }
                }
                _buttonEvent.OnNext(this);
            }
        }
        public bool isRhGrip {
            get {
                return m_isRhGrip;
            }
            set {
                m_isRhGrip = value;
                activate = value;
                if (value) {
                    if (m_isLhGrip)
                        SelectionType = SelectionType.MOVEAXIS;
                    else
                        SelectionType = SelectionType.SELECTALL;
                }
                _buttonEvent.OnNext(this);
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
