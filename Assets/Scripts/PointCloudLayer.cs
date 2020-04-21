using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using Project;
using g3;
using Pcx;

public class PointCloudLayer : MonoBehaviour, ILayer
{
    public string filename;
    public Material material;
    public GameObject handle;
    public GameObject pointCloud;
    public List<GameObject> meshes;
    public bool changed { get; set; }
    public RecordSet layer { get; set; }

    private ParticleData data;
    private GameObject model;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public async Task<GameObject> Init(GeographyCollection layer)
    {
        this.layer = layer;
        string source = layer.Source;
        Quaternion rotate = layer.Transform.Rotate;
        Vector3 scale = layer.Transform.Scale;
        Vector3 translate = (Vector3)layer.Transform.Position * Global._map.WorldRelativeScale;
        Dictionary<string, Unit> symbology = layer.Properties.Units;

        if (source != null)
        {
            PlyImport reader = new PlyImport();
            data = await reader.Load(source);
        }

        model = Instantiate(pointCloud);
        model.transform.parent = gameObject.transform;
        model.transform.localPosition = Vector3.zero;
        model.transform.Translate(translate);
        model.transform.rotation = rotate;
        model.transform.localScale = scale;


        BakedPointCloud cloud = ScriptableObject.CreateInstance<BakedPointCloud>();
        cloud.Initialize(data.vertices, data.colors);

        VisualEffect vfx = model.GetComponent<VisualEffect>();
        vfx.SetTexture("_Positions", cloud.positionMap);
        vfx.SetTexture("_Colors", cloud.colorMap);
        vfx.SetInt("_pointCount", cloud.pointCount);
        vfx.SetFloat("_pointSize", symbology["point"].Transform.Scale.magnitude);
        vfx.Play();

        GameObject centreHandle = Instantiate(handle, gameObject.transform);
        centreHandle.transform.Translate(translate);
        centreHandle.SendMessage("SetColor", (Color)symbology["handle"].Color);
        changed = false;
        return gameObject;
    }

    public void Translate(MoveArgs args)
    {

        if (args.translate != Vector3.zero) model.transform.Translate(args.translate, Space.World);
        changed = true;
    }

    /// <summary>
    /// received when a Move Axis request is made by the user
    /// </summary>
    /// <param name="delta"> Vector representing this channge to the transform</param>
    /// https://answers.unity.com/questions/14170/scaling-an-object-from-a-different-center.html
    public void MoveAxis(MoveArgs args)
    {
        args.rotate.ToAngleAxis(out float angle, out Vector3 axis);
        transform.RotateAround(args.pos, axis, angle);
        Vector3 A = transform.localPosition;
        Vector3 B = transform.InverseTransformPoint(args.pos);
        Vector3 C = A - B;
        float RS = args.scale;
        Vector3 FP = B + C * RS;
        if (FP.magnitude < float.MaxValue)
        {
            transform.localScale = transform.localScale * RS;
            transform.localPosition = FP;
        }
        changed = true;
    }

    public void Save()
    {
        layer.Transform.Position = model.transform.localPosition / Global._map.WorldRelativeScale;
    }
}
