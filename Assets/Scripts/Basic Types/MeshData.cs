using g3;
using GeoJSON.Net.Feature;


namespace Virgis
{

	/// <summary>
	/// Class for holding Mesh data as a SimpleMeshBuilder
	/// 
	/// Note - this extends FeatureCollection. This is a hack to allow typing. this type WILL NOT SAVE succesfully to GeoJSON as a FeatureCollection 
	/// </summary>


	public class MeshData : FeatureCollection
	{
		public SimpleMeshBuilder Mesh;
	}
}
