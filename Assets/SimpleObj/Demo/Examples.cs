/* SimpleOBJ 1.4                        */
/* august 18, 2015                      */
/* By Orbcreation BV                    */
/* Richard Knol                         */
/* info@orbcreation.com                 */
/* games, components and freelance work */

using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class Examples : MonoBehaviour {
	public TextAsset objFile;
	public TextAsset mtlFile;
	public Texture2D[] textures;
	public string url = "http://orbcreation.com/SimpleObj/Ayumi.obj";

	// import a model
	public void Example1 () {
		string objString = objFile.text;
		GameObject importedObject = ObjImporter.Import (objString);
		Debug.Log("Imported the object:" + importedObject);
	}

	// import a model and materials
	public void Example2 () {
		string objString = objFile.text;
		string mtlString = mtlFile.text;
		GameObject importedObject = ObjImporter.Import (objString, mtlString, textures);
		Debug.Log("Imported the object:" + importedObject);
	}

	// import a model and materials and specify all parameters
	public void Example3 () {
		string objString = objFile.text;
		string mtlString = mtlFile.text;
		GameObject importedObject = ObjImporter.Import (
			objString, 
			Quaternion.identity, // rotate the object
			Vector3.one,	// scale of the object
			Vector3.zero,	// translate the object
			mtlString, 
			textures, 
			false, 			// create a child gameObject for every group tag in the OBJ file
			false			// create a submesh for every group tag in the OBJ file
			);
		Debug.Log("Imported the object:" + importedObject);
	}

	// download an OBJ file, import the model
	public void Example4 () {
		StartCoroutine( DownloadAndImportOBJ (url) );
	}

	// download an OBJ file, MTL file, Textures and import
	public void Example5 () {
		StartCoroutine( DownloadAndImportAll (url) );
	}

	// download an OBJ file, MTL file, Textures and import in a background thread
	public void Example6 () {
		StartCoroutine( DownloadAndImportAllInBackground (url) );
	}

	private IEnumerator DownloadAndImportOBJ (string url) {
		string objString = null;
		string mtlString = null;
		Hashtable textures = null;
		GameObject importedObject = null;

		yield return StartCoroutine( DownloadFile (url, retval => objString = retval) );

		if(objString!=null && objString.Length>0) {
			importedObject = ObjImporter.Import (objString, mtlString, textures);
		}
	}

	private IEnumerator DownloadAndImportAll (string url) {
		string objString = null;
		string mtlString = null;
		Hashtable textures = null;
		GameObject importedObject = null;

		yield return StartCoroutine (DownloadFile (url, retval => objString = retval) );
		yield return StartCoroutine (DownloadFile (url.Substring(0,url.Length-4)+".mtl", retval => mtlString = retval) );
		if(mtlString!=null && mtlString.Length>0) {
			string path = url;
			int lastSlash = path.LastIndexOf('/',path.Length-1);
			if(lastSlash>=0) path = path.Substring(0,lastSlash+1);
			Hashtable[] mtls = ObjImporter.ImportMaterialSpecs(mtlString);
			for(int i=0;i<mtls.Length;i++) {
				if(mtls[i].ContainsKey("mainTexName")) {
					Texture2D texture = null;
					string texUrl = path+mtls[i]["mainTexName"];
					yield return StartCoroutine(DownloadTexture(texUrl, retval => texture = retval));
					if(texture != null) {
						if(textures == null) textures = new Hashtable();
						textures[mtls[i]["mainTexName"]] = texture;
					}
				}
			}
		}

		yield return StartCoroutine(DownloadFile(url, retval => objString = retval));

		if(objString!=null && objString.Length>0) {
			importedObject = ObjImporter.Import(objString, mtlString, textures);
		}
	}

	private IEnumerator DownloadAndImportAllInBackground(string url) {
		string objString = null;
		string mtlString = null;
		Hashtable textures = null;
		GameObject importedObject = null;

		yield return StartCoroutine( DownloadFile( url, retval => objString = retval));
		yield return StartCoroutine( DownloadFile( url.Substring(0,url.Length-4)+".mtl", retval => mtlString = retval));
		if(mtlString!=null && mtlString.Length>0) {
			string path = url;
			int lastSlash = path.LastIndexOf('/',path.Length-1);
			if(lastSlash>=0) path = path.Substring(0,lastSlash+1);
			Hashtable[] mtls = ObjImporter.ImportMaterialSpecs(mtlString);
			for(int i=0;i<mtls.Length;i++) {
				if(mtls[i].ContainsKey("mainTexName")) {
					Texture2D texture = null;
					string texUrl = path+mtls[i]["mainTexName"];
					yield return StartCoroutine(DownloadTexture(texUrl, retval => texture = retval));
					if(texture != null) {
						if(textures == null) textures = new Hashtable();
						textures[mtls[i]["mainTexName"]] = texture;
					}
				}
			}
		}

		yield return StartCoroutine(DownloadFile(url, retval => objString = retval));

		if(objString!=null && objString.Length>0) {
			yield return StartCoroutine(ObjImporter.ImportInBackground(objString, mtlString, textures, retval => importedObject = retval));
		}
	}

	private IEnumerator DownloadFile(string url, System.Action<string> result) {
		string dlString = null;
		Debug.Log("Downloading "+url);
		using (UnityWebRequest www = UnityWebRequest.Get(UnityWebRequest.EscapeURL(url)))
		{
			yield return www.Send();

			if (www.isNetworkError || www.isHttpError)
			{
				Debug.Log(www.error);
			}
			else
			{
				Debug.Log("Downloaded "+www.downloadedBytes+" bytes");
				dlString = www.downloadHandler.text;
			}
		}
       	result(dlString);
	}

	private IEnumerator DownloadTexture(string url, System.Action<Texture2D> result) {
		Texture2D dlTex = null;
		Debug.Log("Downloading "+url);
		using (UnityWebRequest www = UnityWebRequest.Get(UnityWebRequest.EscapeURL(url)))
		{
			www.downloadHandler = new DownloadHandlerTexture();
			yield return www.Send();

			if (www.isNetworkError || www.isHttpError)
			{
				Debug.Log(www.error);
			}
			else
			{
				Debug.Log("Downloaded "+www.downloadedBytes+" bytes");
				dlTex = DownloadHandlerTexture.GetContent(www);
			}
		}
		result(dlTex);
	}

}

