using System.Collections.Generic;
using UnityEngine;
using Project;
using g3;
using System;

namespace Virgis
{

    public abstract class MeshlayerProtoype : VirgisLayer<RecordSet, List<DMesh3>>
    {
        // The prefab for the data points to be instantiated
        public GameObject Mesh;
        public Material MeshMaterial;
        public Material WireframeMaterial;

        protected List<Transform> meshes;
        protected Dictionary<string, Unit> symbology;

        private void Start() {
            featureType = FeatureType.MESH;
            AppState appState = AppState.instance;
            appState.editSession.StartEvent.Subscribe(_onEditStart);
            appState.editSession.EndEvent.Subscribe(_onEditStop);
        }

        

        protected override VirgisFeature _addFeature(Vector3[] geometry)
        {
            throw new System.NotImplementedException();
        }
       

        public override void Translate(MoveArgs args) {
            changed = true;
        }

        /// https://answers.unity.com/questions/14170/scaling-an-object-from-a-different-center.html
        public override void MoveAxis(MoveArgs args)
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
                    if (T.GetComponent<Datapoint>() != null)
                    {
                        T.localScale /= RS;
                    }
                }
            }
            changed = true;
        }

        protected override void _checkpoint() { }


        private void _onEditStart(bool test) {
            if (IsEditable()) {
                EditableMesh[] meshes = GetComponentsInChildren<EditableMesh>();
                foreach (EditableMesh mesh in meshes) {
                    mesh.OnEdit(true);
                }
            }
        }

        private void _onEditStop(bool test) {
            if (IsEditable()) {
                EditableMesh[] meshes = GetComponentsInChildren<EditableMesh>();
                foreach (EditableMesh mesh in meshes) {
                    mesh.OnEdit(false);
                }
            }
        }

        protected override void _set_editable() {
            base._set_editable();
            if (AppState.instance.InEditSession()) {
                if (IsEditable()) {
                    EditableMesh[] meshes = GetComponentsInChildren<EditableMesh>();
                    foreach (EditableMesh mesh in meshes) {
                        mesh.OnEdit(true);
                    }
                } else {
                    EditableMesh[] meshes = GetComponentsInChildren<EditableMesh>();
                    foreach (EditableMesh mesh in meshes) {
                        mesh.OnEdit(false);
                    }
                }
            }
        }
    }
}

