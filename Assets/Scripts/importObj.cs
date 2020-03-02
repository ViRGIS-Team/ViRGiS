using UnityEngine;
using System.Collections;

public class importObj : MonoBehaviour
{
    GameObject importedObject;


    void Load(string url)
    {
        StartCoroutine(DownloadAndImportFile(url, Quaternion.Euler(270, 0, 0), Vector3.one, Vector3.zero, false, false, false));
    }


    private IEnumerator DownloadAndImportFile(string url, Quaternion rotate, Vector3 scale, Vector3 translate, bool gameObjectPerGrp, bool subMeshPerGrp, bool usesRightHanded)
    {
        string objString = null;
        string mtlString = null;
        Hashtable textures = null;

        yield return StartCoroutine(DownloadFile(url, retval => objString = retval));
        yield return StartCoroutine(DownloadFile(url.Substring(0, url.Length - 4) + ".mtl", retval => mtlString = retval));
        if (mtlString != null && mtlString.Length > 0)
        {
            string path = url;
            int lastSlash = path.LastIndexOf('/', path.Length - 1);
            if (lastSlash >= 0) path = path.Substring(0, lastSlash + 1);
            Hashtable[] mtls = ObjImporter.ImportMaterialSpecs(mtlString);
            for (int i = 0; i < mtls.Length; i++)
            {
                if (mtls[i].ContainsKey("mainTexName"))
                {
                    Texture2D texture = null;
                    string texUrl = path + mtls[i]["mainTexName"];
                    yield return StartCoroutine(DownloadTexture(texUrl, retval => texture = retval));
                    if (texture != null)
                    {
                        if (textures == null) textures = new Hashtable();
                        textures[mtls[i]["mainTexName"]] = texture;
                    }
                }
            }
        }

        if (objString != null && objString.Length > 0)
        {
            yield return StartCoroutine(ObjImporter.ImportInBackground(objString, mtlString, textures, rotate, scale, translate, retval => importedObject = retval, gameObjectPerGrp, subMeshPerGrp, usesRightHanded));
            AddToLog("Done importing model");

            MeshFilter[] meshFilters = importedObject.GetComponentsInChildren<MeshFilter>();
            for (int i = 0; i < meshFilters.Length; i++)
            {
                Mesh mesh = meshFilters[i].sharedMesh;
                mesh.RecalculateNormals();
            }
        }
    }

    private IEnumerator DownloadFile(string url, System.Action<string> result)
    {
        AddToLog("Downloading " + url);
        WWW www = new WWW(url);
        yield return www;
        if (www.error != null)
        {
            AddToLog(www.error);
        }
        else
        {
            AddToLog("Downloaded " + www.bytesDownloaded + " bytes");
        }
        result(www.text);
    }

    private IEnumerator DownloadTexture(string url, System.Action<Texture2D> result)
    {
        AddToLog("Downloading " + url);
        WWW www = new WWW(url);
        yield return www;
        if (www.error != null)
        {
            AddToLog(www.error);
        }
        else
        {
            AddToLog("Downloaded " + www.bytesDownloaded + " bytes");
        }
        result(www.texture);
    }

    public void AddToLog(string text){
        Debug.Log(text);
    }
}