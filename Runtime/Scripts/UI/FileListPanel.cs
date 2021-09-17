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

using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.IO;

public class FileListPanel : MonoBehaviour {

    [System.Serializable]
    public class FileSelectedEvent : UnityEvent<string> {
    }

    private string _file;
    private FileSelectedEvent _fileSelected = new FileSelectedEvent();

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public string File {
        get => _file;
        set {
            _file = value;

            // name to be displayed is the filename part without extension, 

            string displayName = Path.GetFileName(_file);
            GetComponentInChildren<Text>().text = displayName;
        }
    }

    public void addFileSelectedListerner(UnityAction<string> action) {
        _fileSelected.AddListener(action);
    }

    public void onFileSelected() {
        _fileSelected.Invoke(_file);
    }
}
