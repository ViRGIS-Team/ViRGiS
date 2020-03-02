/* SimpleOBJ 1.4                        */
/* august 18, 2015                      */
/* By Orbcreation BV                    */
/* Richard Knol                         */
/* info@orbcreation.com                 */
/* games, components and freelance work */

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using System.IO;

public class SimpleObjDemo : MonoBehaviour {
	public GameObject cameraGameObject;
	public Texture2D defaultTexture;
	public GameObject rulerIndicatorPrototype;
	public Color[] demoColors;

//	private string url = "http://orbcreation.com/SimpleObj/Ayumi.obj";

//	Use this when you want to use your own local files
  	private string url = "http://orbcreation.com/SimpleObj/apple.obj";

	private string downloadedPath = "";
	private float importScale = 0.01f;
	private float prevScale = 0f;
	private float importedScale = 1f;
	private Vector3 importTranslation = new Vector3(0,0,0);
	private Vector3 importedTranslation = new Vector3(0,0,0);
	private Vector3 importRotation = new Vector3(0,0,0);
	private Vector3 importedRotation = new Vector3(0,0,0);
	private bool gameObjectPerGroup = false;
	private bool subMeshPerGroup = false;
	private bool usesRightHanded = false;

	private string logMsgs = "";

	private GameObject targetObject;
	private Bounds overallBounds;
	private float cameraMovement = 0f;

	private GUIStyle rightAligned = null;
	private int screenshotCounter = 0;

	public Text modelInfoText;
	public Text logText;
	public Slider scaleSlider;

	void Start() {
		overallBounds = new Bounds(Vector3.zero, Vector3.zero);

		// set up ruler
		for(int i=-100;i<=100;i++) {
			if(i!=0 && (i%10==0 || (i>-10 && i<10))) {
				GameObject go = (GameObject)Instantiate(rulerIndicatorPrototype);
				go.GetComponent<TextMesh>().text = ""+i+"m.";
				go.transform.position = new Vector3(i,0,0);
				go.name = "x"+i+"m.";
			}
		}
		for(int i=-100;i<=100;i++) {
			if(i!=0 && (i%10==0 || (i>-10 && i<10))) {
				GameObject go = (GameObject)Instantiate(rulerIndicatorPrototype);
				go.GetComponent<TextMesh>().text = ""+i+"m.";
				go.transform.position = new Vector3(0,0,i);
				go.name = "z"+i+"m.";
			}
		}
		ResetCameraPosition();
	}

	void Update() {
		// slowly rotate model
		if(targetObject!=null) {
			Vector3 rotVec = targetObject.transform.rotation.eulerAngles;
			rotVec.y += Time.deltaTime * 20f;
			targetObject.transform.rotation = Quaternion.Euler(rotVec);
		}
		PositionCameraToShowTargetObject();

		if(Input.GetKeyDown(KeyCode.P)) StartCoroutine(Screenshot());
		if (!Input.anyKey)
		{
			if (importScale != prevScale)
			{
				scaleSlider.minValue = importScale * 0.1f;
				scaleSlider.maxValue = importScale * 4f;
			}
		}
	}

	public void UrlChanged(string newValue)
	{
		url = newValue;
	}
	public void RotateXChanged(float newValue)
	{
		importRotation.x = newValue;
	}
	public void RotateYChanged(float newValue)
	{
		importRotation.y = newValue;
	}
	public void RotateZChanged(float newValue)
	{
		importRotation.z = newValue;
	}
	public void ScaleChanged(float newValue)
	{
		importScale = newValue;
	}
	public void TranslateXChanged(float newValue)
	{
		importTranslation.x = newValue;
	}
	public void TranslateYChanged(float newValue)
	{
		importTranslation.y = newValue;
	}
	public void TranslateZChanged(float newValue)
	{
		importTranslation.z = newValue;
	}
	public void ObjPerGroupChanged(bool newValue)
	{
		gameObjectPerGroup = newValue;
	}
	public void SubMeshPerGroupChanged(bool newValue)
	{
		subMeshPerGroup = newValue;
	}
	public void RightHandedChanged(bool newValue)
	{
		usesRightHanded = newValue;
	}

	public void Import()
	{
		if (downloadedPath != url || importedTranslation != importTranslation || importedScale != importScale ||
		    importedRotation != importRotation)
		{
			//	Reset();
			// Clear previous model
			if (targetObject)
			{
				Destroy(targetObject);
				targetObject = null;
			}

			ResetCameraPosition();
			modelInfoText.text  = "";

			downloadedPath = url;
			importedTranslation = importTranslation;
			importedScale = importScale;
			importedRotation = importRotation;
			StartCoroutine(DownloadAndImportFile(url, Quaternion.Euler(importRotation),
				new Vector3(importScale, importScale, importScale), importTranslation, gameObjectPerGroup,
				subMeshPerGroup, usesRightHanded));
		}
	}

	/* ------------------------------------------------------------------------------------- */
	/* ------------------------------- Downloading files  ---------------------------------- */

	private IEnumerator DownloadAndImportFile(string url, Quaternion rotate, Vector3 scale, Vector3 translate, bool gameObjectPerGrp, bool subMeshPerGrp, bool usesRightHanded) {
		string objString = null;
		string mtlString = null;
		Hashtable textures = null;

		yield return StartCoroutine(DownloadFile(url, retval => objString = retval));
		yield return StartCoroutine(DownloadFile(url.Substring(0,url.Length-4)+".mtl", retval => mtlString = retval));
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

		if(objString!=null && objString.Length>0) {
//			yield return StartCoroutine(ObjImporter.ImportInBackground(objString, mtlString, textures, rotate, scale, translate, retval => targetObject = retval, gameObjectPerGrp, subMeshPerGrp, usesRightHanded));
			targetObject = ObjImporter.Import(objString, mtlString, textures, rotate, scale, translate);
			AddToLog("Done importing model:"+targetObject);
			if(targetObject!=null) {
				if(mtlString == null || mtlString.Length <= 0) {
					SetDftTextureInAllMaterials(targetObject, defaultTexture);
					SetDftColorInAllMaterials(targetObject, defaultTexture);
				}
				// rename the object if needed
				if(targetObject.name == "Imported OBJ file") {
					string[] path = url.Split(new char[] {'/', '.'});
					if(path.Length > 1) targetObject.name = path[path.Length - 2];
				}

				// place the bottom on the floor
				overallBounds = GetBounds(targetObject);
				targetObject.transform.position = new Vector3(0, overallBounds.min.y * -1f, 0);
				overallBounds = GetBounds(targetObject);

				modelInfoText.text = GetModelInfo(targetObject, overallBounds);
				ResetCameraPosition();
			}
		}
	}

	private IEnumerator DownloadFile(string url, System.Action<string> result) {
		AddToLog("Downloading "+url);
        WWW www = new WWW(url);
        yield return www;
        if(www.error!=null) {
        	AddToLog(www.error);
        } else {
        	AddToLog("Downloaded "+www.bytesDownloaded+" bytes");
        }
       	result(www.text);
	}
	
	private IEnumerator DownloadTexture(string url, System.Action<Texture2D> result) {
		AddToLog("Downloading "+url);
        WWW www = new WWW(url);
        yield return www;
        if(www.error!=null) {
        	AddToLog(www.error);
        } else {
        	AddToLog("Downloaded "+www.bytesDownloaded+" bytes");
        }
       	result(www.texture);
	}

	private void SetDftTextureInAllMaterials(GameObject go, Texture2D texture) {
		if(go!=null) {
			Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
	        foreach (Renderer r in renderers) {
	        	foreach (Material m in r.sharedMaterials) {
	        		m.mainTexture = texture;
	        	}
	        }
	    }
	}

	private void SetDftColorInAllMaterials(GameObject go, Texture2D texture) {
		int i=0;
		Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) {
        	foreach (Material m in r.sharedMaterials) {
        		m.color = demoColors[i++%demoColors.Length];
        	}
        }
	}

	private string GetModelInfo(GameObject go, Bounds bounds) {
		string infoString = "";
		int meshCount = 0;
		int subMeshCount = 0;
		int vertexCount = 0;
		int triangleCount = 0;

		MeshFilter[] meshFilters = go.GetComponentsInChildren<MeshFilter>();
		if(meshFilters!=null) meshCount = meshFilters.Length;
        foreach (MeshFilter mf in meshFilters) {
        	Mesh mesh = mf.mesh;
        	subMeshCount += mesh.subMeshCount;
        	vertexCount += mesh.vertices.Length;
        	triangleCount += mesh.triangles.Length / 3;
        }
        infoString = infoString + meshCount + " mesh(es)\n";
        infoString = infoString + subMeshCount + " sub meshes\n";
        infoString = infoString + vertexCount + " vertices\n";
        infoString = infoString + triangleCount + " triangles\n";
        infoString = infoString + bounds.size + " meters";
        return infoString;
	}
	/* ------------------------------------------------------------------------------------- */


	/* ------------------------------------------------------------------------------------- */
	/* --------------------- Position camera to include entire model ----------------------- */
	private Bounds GetBounds(GameObject go) {
        Bounds goBounds = new Bounds(go.transform.position, Vector3.zero);
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        if(renderers != null && renderers.Length > 0) goBounds = renderers[0].bounds;
        foreach (Renderer r in renderers) {
            Bounds bounds = r.bounds;
            goBounds.Encapsulate(bounds);
        }
        return goBounds;
    }

	private void ResetCameraPosition() {
		float nearClip = cameraGameObject.GetComponent<Camera>().nearClipPlane * 1.5f;
		Vector3 camPos = new Vector3(0, 0, nearClip * -1f);
		camPos.y = (overallBounds.size.magnitude)*0.3f;
		if(camPos.y<=nearClip) camPos.y = nearClip * 1.5f;
		cameraGameObject.transform.position = camPos;
		cameraMovement = 0f;
		QualitySettings.shadowDistance = 10f;
	}

	private void PositionCameraToShowTargetObject() {
		if(cameraGameObject!=null && targetObject!=null) {
			cameraGameObject.transform.rotation = Quaternion.LookRotation(overallBounds.center - cameraGameObject.transform.position);
			Vector3 p1 = cameraGameObject.GetComponent<Camera>().WorldToViewportPoint(overallBounds.min); 
			Vector3 p2 = cameraGameObject.GetComponent<Camera>().WorldToViewportPoint(overallBounds.max);
			float diff = 0f;
			if(p1.z<0.02f) diff = Mathf.Max(p1.z*-0.04f, diff);
			if(p2.z<0.02f) diff = Mathf.Max(p2.z*-0.04f, diff);
			if(p1.x<0.05f) diff = Mathf.Max(0.05f-p1.x, diff);
			if(p2.x<0.05f) diff = Mathf.Max(0.05f-p2.x, diff);
			if(p1.x>0.95f) diff = Mathf.Max(p1.x-0.95f, diff);
			if(p2.x>0.95f) diff = Mathf.Max(p2.x-0.95f, diff);
			if(p1.y<0.05f) diff = Mathf.Max(0.05f-p1.y, diff);
			if(p2.y<0.05f) diff = Mathf.Max(0.05f-p2.y, diff);
			if(p1.y>0.95f) diff = Mathf.Max(p1.y-0.95f, diff);
			if(p2.y>0.95f) diff = Mathf.Max(p2.y-0.95f, diff);
			if(diff>0f) {
				cameraMovement += diff * (overallBounds.size.magnitude) * 0.1f * Time.deltaTime;
				Vector3 camPos = cameraGameObject.transform.position;
				camPos.z -= cameraMovement;
				cameraGameObject.transform.position = camPos;

				QualitySettings.shadowDistance = Mathf.Max(10f, camPos.z * -2.2f);
			} else cameraMovement=0f;
		}
	}

	/* ------------------------------------------------------------------------------------- */


	/* ------------------------------------------------------------------------------------- */
	/* ------------------------------- Logging functions  ---------------------------------- */

	private void AddToLog(string msg) {
		Debug.Log(msg+"\n"+DateTime.Now.ToString("yyy/MM/dd hh:mm:ss.fff"));

		// for some silly reason the Editor will generate errors if the string is too long
		int lenNeeded = msg.Length + 1;
		if(logMsgs.Length + lenNeeded>4096) logMsgs = logMsgs.Substring(0,4096-lenNeeded);

		logMsgs = logMsgs + "\n" + msg;
		logText.text = logMsgs;
	}

    private string TruncateStringForEditor(string str) {
    	// for some silly reason the Editor will generate errors if the string is too long
		if(str.Length>4096) str = str.Substring(0,4000)+"\n .... display truncated ....\n";
		return str;
    }
	/* ------------------------------------------------------------------------------------- */

	// To make the screenshots used for the Asset Store submission
	private IEnumerator Screenshot() {
		yield return new WaitForEndOfFrame(); // wait for end of frame to include GUI

		Texture2D screenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
		screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
		screenshot.Apply(false);

		if(Application.platform==RuntimePlatform.OSXPlayer || Application.platform==RuntimePlatform.WindowsPlayer && Application.platform!=RuntimePlatform.LinuxPlayer || Application.isEditor) {
			byte[] bytes = screenshot.EncodeToPNG();
			FileStream fs = new FileStream("Screenshot"+screenshotCounter+".png", FileMode.OpenOrCreate);
			BinaryWriter w = new BinaryWriter(fs);
			w.Write(bytes);
			w.Close();
			fs.Close();
		}
		screenshotCounter++;

	}

}

