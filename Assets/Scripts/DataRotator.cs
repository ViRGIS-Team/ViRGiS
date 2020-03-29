using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataRotator : MonoBehaviour
{
    public Color color;
    public Color anticolor;
    public Vector3 position;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetColor(Color newCol)
    {
        if (newCol != color)
        {
            color = newCol;
            gameObject.BroadcastMessage("SetColor", newCol);
        }
    }

    public void Selected(int button)
    {
        if (button != 100)
        {
            gameObject.BroadcastMessage("Selected", 100, SendMessageOptions.DontRequireReceiver);
        }
    }

    public void UnSelected(int button)
    {
        if (button != 100)
        {
            gameObject.BroadcastMessage("UnSelected", 100, SendMessageOptions.DontRequireReceiver);
        }
    }

    public void Translate(MoveArgs args)
    {
        if (args.translate != Vector3.zero) gameObject.transform.Translate(args.translate);
        if (args.rotate != Quaternion.identity) gameObject.transform.rotation = gameObject.transform.rotation * args.rotate;
        if (args.scale != 0.0f) gameObject.transform.localScale = gameObject.transform.localScale * args.scale;
    } 
}
