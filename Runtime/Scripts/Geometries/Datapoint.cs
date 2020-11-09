
// copyright Runette Software Ltd, 2020. All rights reserved﻿using System.Collections;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using OSGeo.OGR;


namespace Virgis
{
    /// <summary>
    /// Controls an instance of a data pointor handle
    /// </summary>
    public class Datapoint : VirgisFeature
    {
        private Renderer thisRenderer; // convenience link to the rendere for this marker
        public Feature feature; // Feature tht was the source for this GO


        private void Start() {
            if (transform.childCount > 0)
                label = transform.GetChild(0);
        }
        /// <summary>
        /// Every frame - realign the billboard
        /// </summary>
        void Update()
        {
            if (label) label.LookAt(AppState.instance.mainCamera.transform);
        }


        public override void Selected(SelectionType button){
            thisRenderer.material = selectedMat;
            if (button != SelectionType.BROADCAST){
                transform.parent.SendMessageUpwards("Selected", button, SendMessageOptions.DontRequireReceiver);
            }
        }


        public override void UnSelected(SelectionType button){
            thisRenderer.material = mainMat;
            if (button != SelectionType.BROADCAST){
                MoveArgs args = new MoveArgs();
                switch (AppState.instance.editSession.mode){
                    case EditSession.EditMode.None:
                        break;
                    case EditSession.EditMode.SnapAnchor:
                        LayerMask layerMask = UnityLayers.POINT;
                        List<Collider> hitColliders = Physics.OverlapBox(transform.position, transform.TransformVector(Vector3.one / 2 ), Quaternion.identity, layerMask).ToList().FindAll( item => item.transform.position != transform.position);
                        if (hitColliders.Count > 0)
                        {
                            args.oldPos = transform.position;
                            args.pos = hitColliders.First<Collider>().transform.position;
                            args.id = GetId();
                            args.translate = args.pos - args.oldPos;
                            SendMessageUpwards("Translate", args, SendMessageOptions.DontRequireReceiver);
                        }
                        break;
                    case EditSession.EditMode.SnapGrid:
                        args.oldPos = transform.position;
                        args.pos = transform.position.Round(AppState.instance.map.transform.TransformVector(Vector3.one * (AppState.instance.project.ContainsKey("GridScale") && AppState.instance.project.GridScale != 0 ? AppState.instance.project.GridScale :  1f)).magnitude);;
                        args.id = GetId();
                        args.translate = args.pos - transform.position;
                        SendMessageUpwards("Translate", args, SendMessageOptions.DontRequireReceiver);
                        break;
                }
            }
            transform.parent.SendMessageUpwards("UnSelected", button, SendMessageOptions.DontRequireReceiver);
        }

 
        public override void SetMaterial(Material mainMat, Material selectedMat)
        {
            this.mainMat = mainMat;
            this.selectedMat = selectedMat;
            thisRenderer = GetComponent<Renderer>();
            if (thisRenderer)
            {
                thisRenderer.material = mainMat;
            }
        }


        public override void MoveTo(MoveArgs args) {
            if (args.translate != Vector3.zero) {
                args.id = GetId();
                transform.parent.SendMessage("Translate", args, SendMessageOptions.DontRequireReceiver);
            } else if (args.pos != Vector3.zero && args.pos != transform.position) {
                args.id = GetId();
                args.translate = args.pos - transform.position;
                transform.parent.SendMessage("Translate", args, SendMessageOptions.DontRequireReceiver);
            }

        }

        /// <summary>
        ///  Sent by the parent entity to request this market to move as part of an entity move
        /// </summary>
        /// <param name="argsin">MoveArgs</param>
        void TranslateHandle(MoveArgs argsin) {
            if (argsin.id == GetId()) {
                MoveArgs argsout = new MoveArgs();
                argsout.oldPos = transform.position;
                transform.Translate(argsin.translate, Space.World);
                argsout.id = GetId();
                argsout.pos = transform.position;
                SendMessageUpwards("VertexMove", argsout, SendMessageOptions.DontRequireReceiver);
            }
        }


        public override void MoveAxis(MoveArgs args) {
            args.pos = transform.position;
            transform.parent.GetComponent<IVirgisEntity>().MoveAxis(args);
        }



        public override VirgisFeature GetClosest(Vector3 coords, Guid[] excludes) {
            return this;
        }

        public void Delete() {
            transform.parent.SendMessage("RemoveVertex", this, SendMessageOptions.DontRequireReceiver);
        }


        public override Dictionary<string, object> GetMetadata() {
            Dictionary<string, object> meta = feature.GetAll();
            Geometry geom = (gameObject.transform.position.ToGeometry());
            geom.TransformTo(GetLayer().GetCrs());
            double[] coords = new double[3];
            geom.GetPoint(0, coords);
            meta.Add("X Coordinate", coords[0].ToString());
            meta.Add("Y Coordinate", coords[1].ToString());
            meta.Add("Z Coordinate", coords[2].ToString());
            geom.Dispose();
            return meta;
        }

        public override void SetMetadata(Dictionary<string, object> meta) {
            throw new NotImplementedException();
        }
    }
}
