using GeoJSON.Net.Feature;
using System.Collections.Generic;
using UnityEngine;

namespace Virgis
{

	/// <summary>
	/// Class for holding PointCloud data as a Particle cloud
	/// 
	/// Note - this extends FeatureCollection. This is a hack to allow typing. this type WILL NOT SAVE succesfully to GeoJSON as a FeatureCollection 
	/// </summary>
	public class ParticleData : FeatureCollection
	{
		public List<Vector3> vertices;
		public List<Vector3> normals;
		public List<Color32> colors;
		public int vertexCount;
		public Bounds bounds;
	}
}

