/* SimpleOBJ 1.4                        */
/* august 18, 2015                      */
/* By Orbcreation BV                    */
/* Richard Knol                         */
/* info@orbcreation.com                 */
/* games, components and freelance work */

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

using OrbCreationExtensions;

public class ObjException: ApplicationException
 {
     public ObjException(string Message, 
                  Exception innerException): base(Message,innerException) {}
     public ObjException(string Message) : base(Message) {}
     public ObjException() {}
 }

public class ObjImporter {

	private static string str_objString = "objString";
	private static string str_gameObjectPerGroup = "gameObjectPerGroup";
	private static string str_subMeshPerGroup = "subMeshPerGroup";
	private static string str_usesRightHandedCoordinates = "usesRightHandedCoordinates";
	private static string str_ready = "ready";
	private static string str_geometries = "geometries";
	private static string str_triangles = "triangles";
	private static string str_name = "name";
	private static string str_topLevelName = "topLevelName";
	private static string str_rawVs = "rawVs";
	private static string str_rawNs = "rawNs";
	private static string str_rawUs = "rawUs";
	private static string str_vertices = "vertices";
	private static string str_normals = "normals";
	private static string str_uvs = "uvs";
	private static string str_subMeshes = "subMeshes";
	private static string str_obj = "obj";
	private static string str_usemtl = "usemtl ";
	private static string str_newmtl = "newmtl ";
	private static string str_diffuse = "diffuse";
	private static string str_specular = "specular";
	private static string str_Kd = "Kd ";
	private static string str_Ks = "Ks ";
	private static string str_Tr = "Tr ";
	private static string str_Ns = "Ns ";
	private static string str_map_Kd = "map_Kd ";
	private static string str_map_d = "map_d ";
	private static string str_specularity = "specularity";
	private static string str_mainTexName = "mainTexName";
	private static string str_mainTex = "mainTex";
	private static string str_alphaTexName = "alphaTexName";
	private static string str_Standard = "Standard";
	private static string str__Color = "_Color";
	private static string str_mat = "mat";
	private static string str__MainTex = "_MainTex";
	private static string str__SpecColor = "_SpecColor";
	private static string str__Glossyness = "_Glossyness";
	private static string str__part = "_part";
	private static string str_empty = "";
	
	public static GameObject Import(string objString) {
		return Import(objString, Quaternion.identity, new Vector3(1,1,1), Vector3.zero);
	}

	public static GameObject Import(string objString, Quaternion rotate, Vector3 scale, Vector3 translate) {
		return Import(objString, null, null, rotate, scale, translate);
	}

	public static GameObject Import(string objString, string mtlString, Hashtable textures) {
		return Import(objString, mtlString, textures, Quaternion.identity, Vector3.one, Vector3.zero);
	}

	public static GameObject Import(string objString, string mtlString, Hashtable textures, Quaternion rotate, Vector3 scale, Vector3 translate, bool gameObjectPerGroup = false, bool subMeshPerGroup = false, bool usesRightHandedCoordinates = false) {
		List<Hashtable> geometries = ImportGeometry(objString, gameObjectPerGroup, subMeshPerGroup);
		Debug.Log("geometries:"+geometries.Count);
		Hashtable[] matSpecs = ImportMaterialSpecs(mtlString);
		PutTexturesInMaterialSpecs(matSpecs, textures);
		return MakeGameObject(geometries, matSpecs, rotate, scale, translate, usesRightHandedCoordinates);
	}

	public static GameObject Import(string objString, string mtlString, Texture2D[] textures) {
		return Import(objString, Quaternion.identity, Vector3.one, Vector3.zero, mtlString, textures);
	}

	public static GameObject Import(string objString, Quaternion rotate, Vector3 scale, Vector3 translate, string mtlString, Texture2D[] textures, bool gameObjectPerGroup = false, bool subMeshPerGroup = false, bool usesRightHandedCoordinates = false) {
		List<Hashtable> geometries = ImportGeometry(objString, gameObjectPerGroup, subMeshPerGroup);
		Debug.Log("2 geometries:"+geometries);
		Hashtable[] matSpecs = ImportMaterialSpecs(mtlString);
		PutTexturesInMaterialSpecs(matSpecs, textures);
		return MakeGameObject(geometries, matSpecs, rotate, scale, translate, usesRightHandedCoordinates);
	}

	public static IEnumerator ImportInBackground(string objString, string mtlString, Hashtable textures, System.Action<GameObject> result, bool gameObjectPerGroup = false, bool subMeshPerGroup = false, bool usesRightHandedCoordinates = false) {
		yield return null;
		Hashtable info = new Hashtable();
		info[str_objString] = objString;
		info[str_gameObjectPerGroup] = gameObjectPerGroup;
		info[str_subMeshPerGroup] = subMeshPerGroup;
		info[str_usesRightHandedCoordinates] = usesRightHandedCoordinates;

        Thread thread = new Thread(ImportGeometryInBackground);
		thread.Start(info);
		while(!info.ContainsKey(str_ready)) {
			yield return new WaitForSeconds(0.1f);
		}

		Hashtable[] matSpecs = ImportMaterialSpecs(mtlString);
		yield return null;
		PutTexturesInMaterialSpecs(matSpecs, textures);
		yield return null;

		GameObject importedGameObject = MakeGameObject(((List<Hashtable>)info[str_geometries]), matSpecs, Quaternion.identity, Vector3.one, Vector3.zero, usesRightHandedCoordinates);
       	result(importedGameObject);
	}

	public static IEnumerator ImportInBackground(string objString, string mtlString, Hashtable textures, Quaternion rotate, Vector3 scale, Vector3 translate, System.Action<GameObject> result, bool gameObjectPerGroup = false, bool subMeshPerGroup = false, bool usesRightHandedCoordinates = false) {
		yield return null;
		Hashtable info = new Hashtable();
		info[str_objString] = objString;
		info[str_gameObjectPerGroup] = gameObjectPerGroup;
		info[str_subMeshPerGroup] = subMeshPerGroup;

        Thread thread = new Thread(ImportGeometryInBackground);
		thread.Start(info);
		while(!info.ContainsKey(str_ready)) {
			yield return new WaitForSeconds(0.1f);
		}

		Hashtable[] matSpecs = ImportMaterialSpecs(mtlString);
		yield return null;
		PutTexturesInMaterialSpecs(matSpecs, textures);
		yield return null;

		GameObject importedGameObject = MakeGameObject(((List<Hashtable>)info[str_geometries]), matSpecs, rotate, scale, translate, usesRightHandedCoordinates);
       	result(importedGameObject);
	}

	private static void ImportGeometryInBackground(object data) {
		string objString = (string)((Hashtable)data)[str_objString];
		bool gameObjectPerGroup = (bool)((Hashtable)data)[str_gameObjectPerGroup];
		bool subMeshPerGroup = (bool)((Hashtable)data)[str_subMeshPerGroup];
//		ImportGeometry(objString, gameObjectPerGroup, subMeshPerGroup, usesRightHandedCoordinates);
		((Hashtable)data)[str_geometries] = ImportGeometry(objString, gameObjectPerGroup, subMeshPerGroup);
		((Hashtable)data)[str_ready] = true;
	}

	private static List<Hashtable> ImportGeometry(string objString, bool gameObjectPerGroup, bool subMeshPerGroup) {
		// groupsAsGameObject - set this to true if you want to make a new gameObject for each group
		// subMeshPerGroup - set this to true if you want to make a new submesh per group and not just per material

		objString = objString+"\n";  // should always end with a newline otherwse last line is not processed
		List<Hashtable> geometries = new List<Hashtable>();
		List<Vector3> rawVs = new List<Vector3>();
		List<Vector3> rawNs = new List<Vector3>();
		List<Vector2> rawUs = new List<Vector2>();
		List<Vector3> vertices = new List<Vector3>();
		List<Vector3> normals = new List<Vector3>();
		List<Vector2> uvs =  new List<Vector2>();
		List<Hashtable> subMeshes = new List<Hashtable>(); // create a submesh per object per group per material
		Hashtable subMeshData = new Hashtable();
		List<int> triangles = new List<int>();
		subMeshData[str_triangles] = triangles;
		string topLevelName = str_empty;
		string objectName = str_empty;
		string groupName = str_empty;
		string materialName = str_empty;
		subMeshData[str_name] = materialName;
		subMeshes.Add(subMeshData);

		Hashtable geometry = new Hashtable();
		geometry[str_topLevelName] = topLevelName;
		geometry[str_name] = objectName;
		geometry[str_rawVs] = rawVs;
		geometry[str_rawNs] = rawNs;
		geometry[str_rawUs] = rawUs;
		geometry[str_vertices] = vertices;
		geometry[str_normals] = normals;
		geometry[str_uvs] = uvs;
		geometry[str_subMeshes] = subMeshes;

		int[] old2NewVIndex = null;

		bool beginOfLine = true;
		for(int i=0;i<objString.Length;i++) {
			char c = objString[i];

			// process char
			if(c=='\n') {
				beginOfLine = true;

			} else if(beginOfLine && c=='o' && i<objString.Length-2 && objString[i+1]==' ') {  // object info follows
				int e = objString.IndexOfEndOfLine(i+2);
				if(e>i+2) {
					objectName = objString.Substring(i+2,e-i-2).Trim();
					if(topLevelName.Length <= 0) topLevelName = objectName;
					geometry[str_topLevelName] = topLevelName;
//					geometry[str_name] = objectName;
					if(rawVs.Count > 0) {
						geometries.Add(geometry);
						geometry = new Hashtable();
						geometry[str_topLevelName] = topLevelName;
						geometry[str_name] = objectName;
						geometry[str_rawVs] = rawVs;
						geometry[str_rawNs] = rawNs;
						geometry[str_rawUs] = rawUs;
						geometry[str_vertices] = vertices;
						geometry[str_normals] = normals;
						geometry[str_uvs] = uvs;
						subMeshes = new List<Hashtable>();
						geometry[str_subMeshes] = subMeshes;
						subMeshData = new Hashtable();
						triangles = new List<int>();
						subMeshData[str_triangles] = triangles;
						subMeshes.Add(subMeshData);
					} else {
						geometry[str_name] = objectName;
					}
					i=e-1;
				}

			} else if(beginOfLine && c=='g' && i<objString.Length-2 && objString[i+1]==' ') {  // group info follows
				int e = objString.IndexOfEndOfLine(i+2);
				if(e>i+2) {
					if(gameObjectPerGroup) {
						if(triangles.Count>0 && rawVs.Count>0) {
							geometries.Add(geometry);
							geometry = new Hashtable();
							geometry[str_topLevelName] = topLevelName;
							geometry[str_rawVs] = rawVs;
							geometry[str_rawNs] = rawNs;
							geometry[str_rawUs] = rawUs;
							geometry[str_vertices] = vertices;
							geometry[str_normals] = normals;
							geometry[str_uvs] = uvs;
							subMeshes = new List<Hashtable>();
							geometry[str_subMeshes] = subMeshes;
							subMeshData = new Hashtable();
							triangles = new List<int>();
							subMeshData[str_triangles] = triangles;
							subMeshes.Add(subMeshData);
							objectName = str_empty;
						}
					} else if(triangles.Count>0 && subMeshPerGroup) {
						subMeshData = new Hashtable();
						triangles = new List<int>();
						subMeshData[str_triangles] = triangles;
						subMeshes.Add(subMeshData);
					}
					groupName = objString.Substring(i+2,e-i-2).Trim();
					if(objectName.Length <= 0) objectName = groupName;
					geometry[str_name] = objectName;
					i=e-1;
				}

			} else if(beginOfLine && c=='u' && i<objString.Length-7 && objString.Substring(i,7)==str_usemtl) {  // material changes
				int e = objString.IndexOfEndOfLine(i+7);
				if(e>i+7) {
					string newMaterialName = objString.Substring(i+7,e-i-7).Trim();
					int j = 0;
					for(;j<subMeshes.Count;j++) {
						Hashtable row = (Hashtable)subMeshes[j];
						if(((string)row[str_name]) == newMaterialName) {
							subMeshData = row;
							triangles = (List<int>)row[str_triangles];
							break;
						}
					}
					if(triangles.Count>0 && j>=subMeshes.Count) {
						subMeshData = new Hashtable();
						triangles = new List<int>();
						subMeshData[str_triangles] = triangles;
						subMeshes.Add(subMeshData);
					}
					materialName = newMaterialName;
					subMeshData[str_name] = materialName;
					i=e-1;
				}

			} else if(beginOfLine && c=='v' && i<objString.Length-2 && objString[i+1]==' ') {  // vertex info follows
				i++;
				int e = objString.IndexOfEndOfLine(i);
				if(e>i) {
					Vector3 v = GetVector3FromObjString(objString.Substring(i,e-i).Trim()); // ignore vertex weight for now
					rawVs.Add(v);
					i=e-1;
				}

			} else if(beginOfLine && c=='v' && i<objString.Length-2 && objString[i+1]=='n' && objString[i+2]==' ') {  // normal info follows
				i+=2;
				int e = objString.IndexOfEndOfLine(i);
				if(e>i) {
					Vector3 n = GetVector3FromObjString(objString.Substring(i,e-i).Trim());
					rawNs.Add(n);
					i=e-1;
				}

			} else if(beginOfLine && c=='v' && i<objString.Length-2 && objString[i+1]=='t' && objString[i+2]==' ') {  // uv info follows
				i+=2;
				int e = objString.IndexOfEndOfLine(i);
				if(e>i) {
					Vector2 u = GetVector2FromObjString(objString.Substring(i,e-i).Trim());
					rawUs.Add(u);
					i=e-1;
				}

			} else if(beginOfLine && c=='f' && i<objString.Length-2 && objString[i+1]==' ') {  // face info follows
				i++;
				int e = objString.IndexOfEndOfLine(i);
				if(e>i) {
					if(old2NewVIndex==null) {
						old2NewVIndex = new int[rawVs.Count];
						for(int j=0;j<old2NewVIndex.Length;j++) old2NewVIndex[j]=-1;
					}
					if(old2NewVIndex.Length<rawVs.Count) {
						int oldSize = old2NewVIndex.Length;
						Array.Resize(ref old2NewVIndex, rawVs.Count);
						for(int j=oldSize;j<old2NewVIndex.Length;j++) old2NewVIndex[j]=-1;
					}

					List<int[]> rawIndexes = GetFaceIndexesFromObjString(objString.Substring(i,e-i).Trim());
					Vector3 v = new Vector3(0,0,0);
					Vector3 n = new Vector3(0,0,-1);
					Vector2 u = new Vector2(0,0);
					List<int> faceIndexes = new List<int>();

					for(int r=0;r<rawIndexes.Count;r++) {
						int[] indexes = rawIndexes[r];
						if(indexes.Length > 0) {
							int rawVertexIdx = indexes[0];
							if(rawVertexIdx < 0) rawVertexIdx += rawVs.Count;  // relative indexes
							if(rawVertexIdx >= 0 && rawVertexIdx < rawVs.Count) {
								v = rawVs[rawVertexIdx]; 
								if(indexes[1] < 0) indexes[1] += rawUs.Count;  // relative indexes
								if(indexes[1] >= 0 && indexes[1] < rawUs.Count) u = rawUs[indexes[1]];
//								else Log("Bad uv index:"+indexes[1]+" at:"+r);
								if(indexes[2] < 0) indexes[2] += rawNs.Count;  // relative indexes
								if(indexes[2] >= 0 && indexes[2] < rawNs.Count) n = rawNs[indexes[2]]; 

								int newVIndex = old2NewVIndex[rawVertexIdx];
								if(newVIndex>=0) {  // this vertex was already used in a triangle
									Vector3 newV = v;
									Vector3 newN = n;
									Vector3 newU = u;
									if(normals.Count>newVIndex) newN = normals[newVIndex];
									if(uvs.Count>newVIndex) newU = uvs[newVIndex];
									// test if the vertex, normal, uv combination is already used
									if(vertices.Count>newVIndex) {
										newV = vertices[newVIndex];
										if(!IsSameVertex(v,n,u, newV, newN, newU)) {
											newVIndex = vertices.Count;
										}
									}
								} else {
									newVIndex = vertices.Count;
								}

								if(newVIndex >= vertices.Count) {
									vertices.Add(v);
									if(rawNs.Count>0) normals.Add(n);
									uvs.Add(u);
									old2NewVIndex[rawVertexIdx] = newVIndex;  // remember where the vertex went to
								}
								faceIndexes.Add(newVIndex);
							} else {
								Log("Bad vertex index:"+rawVertexIdx+" at:"+r);
							}
						}
					}
					if(faceIndexes.Count>2) PolygonIntoTriangle(faceIndexes.ToArray(), ref triangles);

					i=e-1;
				}
			}

			if(c!=' ' && c!='\r' && c!='\n' && c!='\t') beginOfLine = false;
		}
		if(vertices.Count>0) geometries.Add(geometry);
		return geometries;
	}

	public static Hashtable[] ImportMaterialSpecs(string mtlString) {
		List<Hashtable> materials = new List<Hashtable>();
		Hashtable material = new Hashtable();
		bool beginOfLine = true;
		if(mtlString == null) mtlString = str_empty;
		mtlString = mtlString+"\n";  // should always end with a newline otherwse last line is not processed
		for(int i=0;i<mtlString.Length;i++) {
			char c = mtlString[i];

			// process char
			if(c=='\n') {
				beginOfLine = true;
			} else if(beginOfLine && c=='n' && i<mtlString.Length-7 && mtlString.Substring(i,7)==str_newmtl) {  // material def starts
				i+=7;
				int e = mtlString.IndexOfEndOfLine(i);
				if(e>i) {
					if(material.ContainsKey(str_name)) {
						materials.Add(material);
						material = new Hashtable();
						material[str_diffuse] = Color.white;
					}
					material[str_name] = mtlString.Substring(i,e-i).Trim();
					i=e-1;
				}
			} else if(beginOfLine && c=='K' && i<mtlString.Length-3 && mtlString.Substring(i,3)==str_Kd) {  // diffuse color
				i+=2;
				int e = mtlString.IndexOfEndOfLine(i);
				if(e>i) {
					Vector3 v = GetVector3FromObjString(mtlString.Substring(i,e-i).Trim());
					Vector4 clr = v;
					clr.w = 1f;
					material[str_diffuse] = (Color)clr;
					i=e-1;
				}
			} else if(beginOfLine && c=='K' && i<mtlString.Length-3 && mtlString.Substring(i,3)==str_Ks) {  // specular color
				i+=2;
				int e = mtlString.IndexOfEndOfLine(i);
				if(e>i) {
					Vector3 v = GetVector3FromObjString(mtlString.Substring(i,e-i).Trim());
					Vector4 clr = v;
					clr.w = 1f;
					material[str_specular] = (Color)clr;
					i=e-1;
				}
			} else if(beginOfLine && c=='d' && i<mtlString.Length-2 && mtlString[i+1]==' ') {  // transparency
				i+=2;
				int e = mtlString.IndexOfEndOfLine(i);
				if(e>i) {
					float a = mtlString.Substring(i,e-i).Trim().MakeFloat();
					Color clr = Color.white;
					if(material.ContainsKey(str_diffuse)) clr = (Color)material[str_diffuse];
					clr.a = a;
					material[str_diffuse] = clr;
					i=e-1;
				}
			} else if(beginOfLine && c=='T' && i<mtlString.Length-3 && mtlString.Substring(i,3)==str_Tr) {  // transparency
				i+=3;
				int e = mtlString.IndexOfEndOfLine(i);
				if(e>i) {
					float a = mtlString.Substring(i,e-i).Trim().MakeFloat();
					Color clr = Color.white;
					if(material.ContainsKey(str_diffuse)) clr = (Color)material[str_diffuse];
					clr.a = a;
					material[str_diffuse] = clr;
					i=e-1;
				}
			} else if(beginOfLine && c=='N' && i<mtlString.Length-3 && mtlString.Substring(i,3)==str_Ns) {  // transparency
				i+=3;
				int e = mtlString.IndexOfEndOfLine(i);
				if(e>i) {
					float s = mtlString.Substring(i,e-i).Trim().MakeFloat();
					if(s > 0f) material[str_specularity] = 1f / s;
					i=e-1;
				}
			} else if(beginOfLine && c=='m' && i<mtlString.Length-7 && mtlString.Substring(i,7)==str_map_Kd) {  // transparency
				i+=7;
				int e = mtlString.IndexOfEndOfLine(i);
				if(e>i) {
					material[str_mainTexName] = mtlString.Substring(i,e-i).Trim();
					i=e-1;
				}
			} else if(beginOfLine && c=='m' && i<mtlString.Length-6 && mtlString.Substring(i,7)==str_map_d) {  // transparency
				i+=6;
				int e = mtlString.IndexOfEndOfLine(i);
				if(e>i) {
					material[str_alphaTexName] = mtlString.Substring(i,e-i).Trim();
					i=e-1;
				}
			}
			if(c!=' ' && c!='\r' && c!='\n' && c!='\t') beginOfLine = false;
		}
		if(material.ContainsKey(str_name)) materials.Add(material);
		return materials.ToArray();
	}

	public static void PutTexturesInMaterialSpecs(Hashtable[] matSpecs, Hashtable textures) {
		for(int i=0;textures!=null && i<matSpecs.Length;i++) {
			if(matSpecs[i].ContainsKey(str_mainTexName)) {
				string texName = (string)matSpecs[i][str_mainTexName];
				if(textures.ContainsKey(texName)) matSpecs[i][str_mainTex] = (Texture2D)textures[texName];
			}
		}
	}

	public static void PutTexturesInMaterialSpecs(Hashtable[] matSpecs, Texture2D[] textures) {
		int textureIndex = 0;
		for(int i=0;textures!=null && i<matSpecs.Length;i++) {
			if(matSpecs[i].ContainsKey(str_mainTexName)) {
				if(textures.Length > textureIndex) matSpecs[i][str_mainTex] = textures[textureIndex++];
			}
		}
	}

	public static void PutMaterialSpecsInMaterial(Material mat, Hashtable[] matSpecs) {
    	for(int ms=0;ms<matSpecs.Length;ms++) {
    		string matName = ((string)matSpecs[ms][str_name]);
    		if(matName == mat.name) {
    			string shaderName = str_Standard;
//    			if(matSpecs[ms].ContainsKey(str_specular) || matSpecs[ms].ContainsKey(str_specularity)) shaderName = str_specular;
//    			if(matSpecs[ms].ContainsKey(str_diffuse) && ((Color)matSpecs[ms][str_diffuse]).a < 1f) shaderName = "Transparent/"+shaderName;
//    			else if(matSpecs[ms].ContainsKey(str_mainTex) && ((Texture2D)matSpecs[ms][str_mainTex]).HasTransparency()) shaderName = "Transparent/"+shaderName;
    			mat.shader = Shader.Find(shaderName);
	   			if(matSpecs[ms].ContainsKey(str_mainTex) && mat.HasProperty(str__MainTex)) mat.SetTexture(str__MainTex, (Texture2D)matSpecs[ms][str_mainTex]);
    			if(matSpecs[ms].ContainsKey(str_diffuse) && mat.HasProperty(str__Color)) {
    				mat.SetColor(str__Color, (Color)matSpecs[ms][str_diffuse]);
    			}
    			if(matSpecs[ms].ContainsKey(str_specular) && mat.HasProperty(str__SpecColor)) mat.SetColor(str__SpecColor, (Color)matSpecs[ms][str_specular]);
    			if(matSpecs[ms].ContainsKey(str_specularity) && mat.HasProperty(str__Glossyness)) mat.SetFloat(str__Glossyness, (float)matSpecs[ms][str_specularity]);
    		}
    	}
    }

	private static GameObject MakeGameObject(List<Hashtable> geometries, Hashtable[] matSpecs, Quaternion rotate, Vector3 scale, Vector3 translate, bool usesRightHandedCoordinates) {
		GameObject importedGameObject = null;
		int maxVertices = 65534; // for some reason Unity's limit is 65534 and not 64K (65536)
		string topLevelName = str_empty;

		if(geometries.Count > 0) {
			Hashtable geometry = geometries[0];
			topLevelName = geometry.GetString(str_topLevelName);
			if(topLevelName.Length <= 0) topLevelName = "Imported OBJ file";
		}
		if(geometries.Count > 1) {
			importedGameObject = new GameObject(topLevelName);
		}
		for(int i=0;i<geometries.Count;i++) {
			Hashtable geometry = geometries[i];
			string objectName = geometry.GetString(str_name);
			if(objectName.Length <= 0) objectName = str_obj + i;
			List<Vector3> vertices = (List<Vector3>)geometry[str_vertices];
			vertices = vertices.GetRange(0, vertices.Count);  // get a copy
			List<Vector3> normals = (List<Vector3>)geometry[str_normals];
			List<Vector2> uvs = (List<Vector2>)geometry[str_uvs];
			List<Hashtable> subMeshes = (List<Hashtable>)geometry[str_subMeshes];
			if(usesRightHandedCoordinates) {
				FlipXAxis(ref vertices);
				if(normals != null) FlipXAxis(ref normals);
			}
			if(rotate != Quaternion.identity) RotateVertices(ref vertices, rotate);
			if(scale != Vector3.zero) ScaleVertices(ref vertices, scale);
			if(translate != Vector3.zero) TranslateVertices(ref vertices, translate);

			int subMeshIdx = 0;
			int triangleIdx = 0;
			int meshPart = 0;
			bool hasMultipleParts = false;
			while(true) {
				bool sizeLimitReached = false;
				int[] o2n = new int[vertices.Count];  // old to new vertex index
				for(int j=0;j<o2n.Length;j++) o2n[j] = -1;
				List<int> n2o = new List<int>();   // new to old vertex idx
				List<List<int>> meshTriangles = new List<List<int>>();
				List<Material> meshMaterials = new List<Material>();

				for( ;subMeshIdx<subMeshes.Count && !sizeLimitReached;subMeshIdx++) {
					Hashtable subMeshData = subMeshes[subMeshIdx];
					List<int> subMeshTriangles = (List<int>)subMeshData[str_triangles];
					List<int> meshTrianglesForSubMesh = new List<int>();
					int triCounter = 0;

					for(;triangleIdx<subMeshTriangles.Count;triangleIdx+=3) {
						int t0 = subMeshTriangles[triangleIdx];
						int t1 = subMeshTriangles[triangleIdx+1];
						int t2 = subMeshTriangles[triangleIdx+2];
						if(usesRightHandedCoordinates) {
							t1 = t0;
							t0 = subMeshTriangles[triangleIdx+1];
						}
						if(o2n[t0] < 0) {
							if(n2o.Count > maxVertices - 3) {
								sizeLimitReached = true;
								break;
							}
							o2n[t0] = n2o.Count;
							n2o.Add(t0);
						}
						if(o2n[t1] < 0) {
							if(n2o.Count > maxVertices - 2) {
								sizeLimitReached = true;
								break;
							}
							o2n[t1] = n2o.Count;
							n2o.Add(t1);
						}
						if(o2n[t2] < 0) {
							if(n2o.Count > maxVertices - 1) {
								sizeLimitReached = true;
								break;
							}
							o2n[t2] = n2o.Count;
							n2o.Add(t2);
						}
						meshTrianglesForSubMesh.Add(o2n[t0]);
						meshTrianglesForSubMesh.Add(o2n[t1]);
						meshTrianglesForSubMesh.Add(o2n[t2]);
						triCounter+=3;
					}

					if(meshTrianglesForSubMesh.Count > 0) {
						Material mat = new Material(Shader.Find(str_Standard));
		        		mat.SetColor(str__Color, Color.white);
			        	mat.name = (string)subMeshData[str_name];
		    	    	if(mat.name.Length <= 0) mat.name = str_mat + meshMaterials.Count;
			        	meshMaterials.Add(mat);
						PutMaterialSpecsInMaterial(mat, matSpecs);
						meshTriangles.Add(meshTrianglesForSubMesh);
					}
					triangleIdx += 3;
					if(triangleIdx >= subMeshTriangles.Count) triangleIdx = 0;
					else break;  // this submesh will continue in the next mesh
				}
				if(sizeLimitReached) hasMultipleParts = sizeLimitReached;

				if(n2o.Count > 0) {
					string goName = topLevelName;
					if(geometries.Count > 1) goName = objectName;
					if(hasMultipleParts && (importedGameObject == null)) {
						importedGameObject = new GameObject(goName);
					}
					Mesh mesh = new Mesh();
					Vector3[] meshVertices = new Vector3[n2o.Count];
					for(int j=0;j<n2o.Count;j++) {
						meshVertices[j] = vertices[n2o[j]];
					}
					mesh.vertices = meshVertices;
//					Debug.Log("objectName:"+objectName+" vertices:"+vertices.Count+" normals:"+normals.Count);
					if(normals.Count > 0){
						Vector3[] meshNormals = new Vector3[n2o.Count];
						for(int j=0;j<n2o.Count;j++) {
							if(n2o[j] >=0 && n2o[j] < normals.Count)
								meshNormals[j] = normals[n2o[j]];
							else {
//								Debug.LogError("index:"+n2o[j]+" normals.Count:"+normals.Count);
								meshNormals[j] = Vector3.up;
							}
						}
						mesh.normals = meshNormals;
					}
					if(uvs.Count > 0) {
						Vector2[] meshUvs = new Vector2[n2o.Count];
						for(int j=0;j<n2o.Count;j++) {
							meshUvs[j] = uvs[n2o[j]];
						}
						mesh.uv = meshUvs;
					}
					mesh.subMeshCount = meshTriangles.Count;
					for(int j=0;j<meshTriangles.Count;j++) {
						mesh.SetTriangles(meshTriangles[j].ToArray(), j);
					}
					if(normals.Count <= 0) {
						mesh.RecalculateNormals();
					}
					mesh.RecalculateTangents();
					mesh.RecalculateBounds();

					if(hasMultipleParts  && importedGameObject != null) goName = goName + str__part + meshPart;
					mesh.name = goName;
					GameObject go = new GameObject(goName);
					MeshRenderer mr = go.AddComponent<MeshRenderer>();
					MeshFilter mf = go.AddComponent<MeshFilter>();
					mf.sharedMesh = mesh;
			        mr.sharedMaterials = meshMaterials.ToArray();

			        if(importedGameObject==null) importedGameObject = go;
			        else {
			        	#if UNITY_4_5
				        	go.transform.parent = importedGameObject.transform;
				        #else
					        go.transform.SetParent(importedGameObject.transform);
				        #endif
			        }
				}

				if(!sizeLimitReached) break;
				meshPart++;
			}
		}
		return importedGameObject;
	}

	private static void FlipXAxis(ref List<Vector3> vs) {
		for(int i=0;i<vs.Count;i++) {
			Vector3 v = vs[i];
			v.x *= -1;
			vs[i] = v;
		}
	}

	private static Vector3 GetVector3FromObjString(string str) {
		Vector3 vec = new Vector3(0,0,0);
		int	i = 0;
		for(int elem=0;elem<3;elem++) {
			int e = str.IndexOf(' ',i);
			if(e < 0) e = str.Length;
			vec[elem] = str.Substring(i,e-i).MakeFloat();
			i = str.EndOfCharRepetition(e);
		}
		return vec;
	}
	
	private static Vector2 GetVector2FromObjString(string str) {
		Vector2 vec = new Vector2(0,0);
		int	i = 0;
		for(int elem=0;elem<2;elem++) {
			int e = str.IndexOf(' ',i);
			if(e < 0) e = str.Length;
			vec[elem] = str.Substring(i,e-i).MakeFloat();
			i = str.EndOfCharRepetition(e);
		}
		return vec;
	}
	
	private static List<int[]> GetFaceIndexesFromObjString(string str) {
		List<int[]> corners = new List<int[]>();
		int i = 0;
		while(i<str.Length) {
			int e = str.IndexOf(' ',i);
			if(e < 0) e = str.Length;
			corners.Add(GetFaceCornerIndexesFromObjString(str.Substring(i,e-i).Trim()));
			i = str.EndOfCharRepetition(e);
		}
		return corners;
	}

	private static int[] GetFaceCornerIndexesFromObjString(string str) {
		int[] indexes = new int[3];
		int elem = 0;
		int i = 0;
		for(elem=0;elem<3;elem++) indexes[elem]=-1;
		elem=0;
		while(i<str.Length) {
			int e = str.IndexOf('/',i);
			if(e < 0) e = str.Length;
			indexes[elem] = str.Substring(i,e-i).MakeInt();
			if(indexes[elem]>0) indexes[elem]--;  // -1 is because OBJ indexes start at 1 instead of 0
			elem++;
			i = e+1;
		}
		return indexes;
	}


	private static void TranslateVertices(ref List<Vector3> vertices, Vector3 translate) {
		for(int i=0;i<vertices.Count;i++) {
			vertices[i]+=translate;
		}
	}
	private static void ScaleVertices(ref List<Vector3> vertices, Vector3 scale) {
		for(int i=0;i<vertices.Count;i++) {
			Vector3 v = vertices[i];
			v.x *= scale.x;
			v.y *= scale.y;
			v.z *= scale.z;
			vertices[i] = v;
		}
	}
	private static void RotateVertices(ref List<Vector3> vertices, Quaternion rotate) {
		for(int i=0;i<vertices.Count;i++) {
			vertices[i] = rotate * vertices[i];
		}
	}

	private static bool IsSameVertex(Vector3 v1, Vector3 n1, Vector2 u1, Vector3 v2, Vector3 n2, Vector2 u2) {
		return (v1==v2 && n1==n2 && u1==u2);
	}

	private static void PolygonIntoTriangle(int[] polygon, ref List<int> triangles) {
		if(polygon.Length < 3) return; // no lines supported
		else if(polygon.Length == 3) {  // the polygon is already a triangle
			for(int i=0;i<3;i++) triangles.Add(polygon[i]);
		} else {
			// we divide the polygon in half until it is composed of only triangles
			int i = 0;
			int p = 0;
			int halfSize = polygon.Length / 2;  // index of the corner on the other side
			int[] p1 = new int[halfSize+1];  // set new polygons size 
			int[] p2 = new int[(polygon.Length - halfSize)+1];
			for(i=0;i<p1.Length;i++) {  // copy into p1
				p1[i] = polygon[p++];
			}
			p2[0] = polygon[p-1];
			for(i=1;i<p2.Length-1;i++) {  // copy the rest into p2
				p2[i] = polygon[p++];
			}
			p2[i] = polygon[0];
			PolygonIntoTriangle(p1, ref triangles);  // if our polygons are not ye triangles, the process is repeated
			PolygonIntoTriangle(p2, ref triangles);
		}
	}

	private static void Log(int[] vs) {
		string str = str_empty;
		for(int i=0;i<vs.Length;i++) {
			str = str + vs[i]+"\n";
		}
		Debug.Log(str+DateTime.Now.ToString("yyy/MM/dd hh:mm:ss.fff"));
	}
	private static void Log(Vector3[] vs) {
		string str = str_empty;
		for(int i=0;i<vs.Length;i++) {
			str = str + vs[i]+"\n";
		}
		Debug.Log(str+DateTime.Now.ToString("yyy/MM/dd hh:mm:ss.fff"));
	}
	private static void Log(string str) {
		Debug.Log(str+"\n"+DateTime.Now.ToString("yyy/MM/dd hh:mm:ss.fff"));
	}
}
