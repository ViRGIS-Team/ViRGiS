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
