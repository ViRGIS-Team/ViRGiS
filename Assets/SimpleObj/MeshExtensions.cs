/* OrbCreationExtensions 1.0            */
/* By Orbcreation BV                    */
/* Richard Knol                         */
/* info@orbcreation.com                 */
/* March 31, 2015                       */
/* games, components and freelance work */

/* Note: if you also use other packages by Orbcreation,  */
/* you may end up with multiple copies of this file.     */
/* In that case, better delete/merge those files into 1. */

using UnityEngine;
using System.Collections;

namespace OrbCreationExtensions
{
	public static class MeshExtensions {

		public static void RecalculateTangents(this Mesh mesh) {
			int vertexCount = mesh.vertexCount;
			Vector3[] vertices = mesh.vertices;
			Vector3[] normals = mesh.normals;
			Vector2[] texcoords = mesh.uv;
			int[] triangles = mesh.triangles;
			int triangleCount = triangles.Length/3;
			Vector4[] tangents = new Vector4[vertexCount];
			Vector3[] tan1 = new Vector3[vertexCount];
			Vector3[] tan2 = new Vector3[vertexCount];
			int tri = 0;
			if(texcoords.Length<=0) return;
			for (int i = 0; i < (triangleCount); i++) {
				int i1 = triangles[tri];
				int i2 = triangles[tri+1];
				int i3 = triangles[tri+2];
				 
				Vector3 v1 = vertices[i1];
				Vector3 v2 = vertices[i2];
				Vector3 v3 = vertices[i3];
				 
				Vector2 w1 = texcoords[i1];
				Vector2 w2 = texcoords[i2];
				Vector2 w3 = texcoords[i3];
				 
				float x1 = v2.x - v1.x;
				float x2 = v3.x - v1.x;
				float y1 = v2.y - v1.y;
				float y2 = v3.y - v1.y;
				float z1 = v2.z - v1.z;
				float z2 = v3.z - v1.z;
				 
				float s1 = w2.x - w1.x;
				float s2 = w3.x - w1.x;
				float t1 = w2.y - w1.y;
				float t2 = w3.y - w1.y;
				 
				float div = s1 * t2 - s2 * t1;
    			float r = div == 0.0f ? 0.0f : 1.0f / div;
				Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
				Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);
				 
				tan1[i1] += sdir;
				tan1[i2] += sdir;
				tan1[i3] += sdir;
				 
				tan2[i1] += tdir;
				tan2[i2] += tdir;
				tan2[i3] += tdir;
				 
				tri += 3;
			}
			 
			for (int i = 0; i < (vertexCount); i++) {
				Vector3 n = normals[i];
				Vector3 t = tan1[i];
				 
				// Gram-Schmidt orthogonalize
				Vector3.OrthoNormalize(ref n, ref t );
				 
				tangents[i].x = t.x;
				tangents[i].y = t.y;
				tangents[i].z = t.z;
				 
				// Calculate handedness
				tangents[i].w = ( Vector3.Dot(Vector3.Cross(n, t), tan2[i]) < 0.0f ) ? -1.0f : 1.0f;
			}
			mesh.tangents = tangents;
		}
	}
}
