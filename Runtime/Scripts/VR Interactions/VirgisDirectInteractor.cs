using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Virgis {

    public class VirgisDirectInteractor : VirgisInteractor
    {

        public override void UpdateUIModel(ref TrackedDeviceModel model)
        {
            // Start is called before the first frame update
            if (!isActiveAndEnabled)
                return;

            model.position = transform.TransformPoint(0, -1, 0);
            model.orientation = transform.rotation;
            //model.select = isUISelectActive;
            model.select = isUISelectActive;
            //model.raycastLayerMask = raycastMask;

            List<Vector3> raycastPoints = model.raycastPoints;
            raycastPoints.Clear();
            raycastPoints.Add(transform.TransformPoint(0, -1, 0));
            raycastPoints.Add(transform.TransformPoint(0,1,0));
        }
    }
}
