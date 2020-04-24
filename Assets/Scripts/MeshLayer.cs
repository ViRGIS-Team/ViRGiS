using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;
using Project;
using g3;
using GeoJSON.Net;

public class MeshLayer : MonoBehaviour, ILayer
{

    public string filename;
    public Material material;
    public GameObject handle;
    public List<GameObject> meshes;
    public bool changed { get; set; } = false;
    public RecordSet layer { get; set; }



    private async Task<SimpleMeshBuilder> loadObj(string filename)
    {
        using (StreamReader reader = File.OpenText(filename))
        {
            OBJReader objReader = new OBJReader();
            SimpleMeshBuilder meshBuilder = new SimpleMeshBuilder();
            IOReadResult result = objReader.Read(reader, new ReadOptions(), meshBuilder);
            return meshBuilder;
        }
    }

    public async Task<GameObject> Init(GeographyCollection layer)
    {
        this.layer = layer;
        string source = layer.Source;
        transform.position = Tools.Ipos2Vect((GeoJSON.Net.Geometry.Position)layer.Position.Coordinates);
        transform.Translate(Global.Map.transform.TransformVector((Vector3)layer.Transform.Position * Global._map.WorldRelativeScale));
        Dictionary<string, Unit> symbology = layer.Properties.Units;
        meshes = new List<GameObject>();

        if (source != null)
        {
            SimpleMeshBuilder meshBuilder = await loadObj(source);
            foreach (SimpleMesh simpleMesh in meshBuilder.Meshes)
            {
                GameObject meshGameObject = new GameObject();
                MeshFilter mf = meshGameObject.AddComponent<MeshFilter>();
                MeshRenderer renderer = meshGameObject.AddComponent<MeshRenderer>();
                renderer.material = material;
                meshGameObject.transform.parent = gameObject.transform;
                meshGameObject.transform.localPosition = Vector3.zero;
                mf.mesh = simpleMesh.ToMesh();
                meshes.Add(meshGameObject);
            }
            transform.rotation = layer.Transform.Rotate;
            transform.localScale = layer.Transform.Scale;
            GameObject centreHandle = Instantiate(handle, gameObject.transform.position, Quaternion.identity);
            centreHandle.transform.parent = transform;
            centreHandle.transform.localScale = transform.InverseTransformVector(Global.Map.transform.TransformVector((Vector3)symbology["handle"].Transform.Scale * Global._map.WorldRelativeScale));
            centreHandle.SendMessage("SetColor", (Color)symbology["handle"].Color);

        }
        return gameObject;
    }

    public void Translate(MoveArgs args)
    {
        foreach (GameObject mesh in meshes) {
            if (args.translate != Vector3.zero) transform.Translate(args.translate, Space.World);
            changed = true;
        }
    }

    /// <summary>
    /// received when a Move Axis request is made by the user
    /// </summary>
    /// <param name="delta"> Vector representing this channge to the transform</param>
    /// https://answers.unity.com/questions/14170/scaling-an-object-from-a-different-center.html
    public void MoveAxis(MoveArgs args)
    {
        if (args.translate != Vector3.zero) transform.Translate(args.translate, Space.World);
        args.rotate.ToAngleAxis(out float angle, out Vector3 axis);
        transform.RotateAround(args.pos, axis, angle);
        Vector3 A = transform.localPosition;
        Vector3 B = transform.parent.InverseTransformPoint(args.pos);
        Vector3 C = A - B;
        float RS = args.scale;
        Vector3 FP = B + C * RS;
        if (FP.magnitude < float.MaxValue)
        {
            transform.localScale = transform.localScale * RS;
            transform.localPosition = FP;
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform T = transform.GetChild(i);
                if (T.GetComponent<DatapointSphere>() != null)
                {
                    T.localScale /= RS;
                }
            }
        }
        changed = true;
    }

    public RecordSet Save()
    {
        if (changed)
        {
            layer.Position = new GeoJSON.Net.Geometry.Point(Tools.Vect2Ipos(transform.position));
            layer.Transform.Position = Vector3.zero;
            layer.Transform.Rotate = transform.rotation;
            layer.Transform.Scale = transform.localScale;
        }

        return layer;
    }
}

