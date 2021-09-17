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


    public class EditSession {
        public enum EditMode {
            None, SnapGrid, SnapAnchor
        }

        private bool _active;
        private IVirgisLayer _editableLayer;

        /// <summary>
        /// StartEditSession is triggered whenever the edit session starts
        /// </summary>
        private readonly Subject<bool> _startEditSessionEvent;

        // EndEditSessionEvent is triggered whenever user ends
        // an edit session. This event carries one boolean parameter:
        // true if the edit session is stopped and saved,
        // false if the edit session is stopped and discarded.
        private readonly Subject<bool> _endEditSessionEvent;

        /// <summary>
        /// EditableLayerChangedEvent is triggered during an edit session,
        /// where the editable layer has been changed to another layer.
        /// </summary>
        /// 
        /// This event carries the new editable layer.
        private readonly Subject<IVirgisLayer> _editableLayerChangedEvent;
        private EditMode _editMode;

        public EditSession() {
            _active = false;
            _startEditSessionEvent = new Subject<bool>();
            _endEditSessionEvent = new Subject<bool>();
            _editableLayerChangedEvent = new Subject<IVirgisLayer>();
            _editMode = EditMode.None;
        }

        public IVirgisLayer editableLayer {
            get => _editableLayer;
            set {
                value?.SetEditable(true);
                _editableLayer?.SetEditable(false);
                _editableLayer = value;
                if (_active) _editableLayerChangedEvent.OnNext(_editableLayer);
            }
        }

        public bool IsActive() {
            return _active;
        }

        public void Start() {
            if (!_active) {
                _startEditSessionEvent.OnNext(true);
                _active = true;
            }
        }

        public void StopAndSave() {
            if (_active) {
                _active = false;
                _endEditSessionEvent.OnNext(true);
            }
        }

        public void StopAndDiscard() {
            if (_active) {
                _active = false;
                _endEditSessionEvent.OnNext(false);
            }
        }

        public EditMode mode {
            get => _editMode;
            set {
                _editMode = value;
            }
        }

        public IObservable<bool> StartEvent {
            get {
                return _startEditSessionEvent.AsObservable<bool>();
            }
        }

        public IObservable<bool> EndEvent {
            get {
                return _endEditSessionEvent.AsObservable<bool>();
            }
        }

        public IObservable<IVirgisLayer> ChangeLayerEvent {
            get {
                return _editableLayerChangedEvent.AsObservable<IVirgisLayer>();
            }
        }


    }
}