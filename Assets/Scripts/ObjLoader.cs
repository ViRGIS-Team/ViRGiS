using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;
using System.Text;

public class ObjLoader : MonoBehaviour
{

    public string filename;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private async Task<string> loadObj( string filename)
    {
        char[] result;
        StringBuilder builder = new StringBuilder();
        using (StreamReader reader = File.OpenText(filename))
        {
            result = new char[reader.BaseStream.Length];
            await reader.ReadAsync(result, 0, (int)reader.BaseStream.Length);
        }

        foreach (char c in result)
        {
            builder.Append(c);
        }
        return builder.ToString();
    }

    public async Task<GameObject> Init( string source, Quaternion rotate, Vector3 scale, Vector3 translate)
    {
        if (source != null)
        {
            string objString = await loadObj(source);
            GameObject myGameObject = ObjImporter.Import(objString, rotate, scale, translate);
            myGameObject.transform.parent = gameObject.transform;
        }
        return gameObject;
    }
}
