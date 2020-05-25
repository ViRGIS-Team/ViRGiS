
// copyright Runette Software Ltd, 2020. All rights reserved﻿using System.Collections;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;


namespace Virgis
{
    /// <summary>
    /// Controls an instance of a data pointor handle
    /// </summary>
    public class Datapoint : VirgisComponent
    {
        private Renderer thisRenderer; // convenience link to the rendere for this marker


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


        public override void Selected(SelectionTypes button){
            thisRenderer.material = selectedMat;
            if (button != SelectionTypes.BROADCAST){
                transform.parent.SendMessageUpwards("Selected", button, SendMessageOptions.DontRequireReceiver);
            }
        }


        public override void UnSelected(SelectionTypes button){
            thisRenderer.material = mainMat;
            if (button != SelectionTypes.BROADCAST){
                transform.parent.SendMessageUpwards("UnSelected", button, SendMessageOptions.DontRequireReceiver);
                MoveArgs args = new MoveArgs();
                switch (AppState.instance.editSession.mode){
                    case EditSession.EditMode.None:
                        break;
                    case EditSession.EditMode.SnapAnchor:
                        List<Collider> hitColliders = Physics.OverlapBox(transform.position, transform.TransformVector(Vector3.one / 2 ), Quaternion.identity).ToList().FindAll( item => item.transform.position != transform.position);
                        if (hitColliders.Count > 0)
                        {
                            args.oldPos = transform.position;
                            args.pos = hitColliders.First<Collider>().transform.position;
                            args.id = id;
                            args.translate = args.pos - args.oldPos;
                            SendMessageUpwards("Translate", args, SendMessageOptions.DontRequireReceiver);
                        }
                        break;
                    case EditSession.EditMode.SnapGrid:
                        args.oldPos = transform.position;
                        args.pos = transform.position.Round(AppState.instance.map.transform.TransformVector(Vector3.one * (AppState.instance.project.ContainsKey("GridScale") && AppState.instance.project.GridScale != 0 ? AppState.instance.project.GridScale :  1f)).magnitude);;
                        args.id = id;
                        args.translate = args.pos - transform.position;
                        SendMessageUpwards("Translate", args, SendMessageOptions.DontRequireReceiver);
                        break;
                }
            }
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
                args.id = id;
                SendMessageUpwards("Translate", args, SendMessageOptions.DontRequireReceiver);
            }
        }

        /// <summary>
        ///  Sent by the parent entity to request this market to move as part of an entity move
        /// </summary>
        /// <param name="argsin">MoveArgs</param>
        void TranslateHandle(MoveArgs argsin) {
            if (argsin.id == id) {
                MoveArgs argsout = new MoveArgs();
                argsout.oldPos = transform.position;
                transform.Translate(argsin.translate, Space.World);
                argsout.id = id;
                argsout.pos = transform.position;
                SendMessageUpwards("VertexMove", argsout, SendMessageOptions.DontRequireReceiver);
            }
        }


        public override void MoveAxis(MoveArgs args) {
            args.pos = transform.position;
            transform.parent.SendMessageUpwards("MoveAxis", args, SendMessageOptions.DontRequireReceiver);
        }

        public override void VertexMove(MoveArgs args) {
            
        }

        public override void Translate(MoveArgs args) {
            
        }

        public override Vector3 GetClosest(Vector3 coords) {
            return transform.position;
        }

        public void Delete() {
            transform.parent.SendMessage("RemoveVertex", this, SendMessageOptions.DontRequireReceiver);
        }

        public override T GetGeometry<T>() {
            switch (typeof(T).Name)
            {
                case "Vector3":
                    return (T)Convert.ChangeType("Hello there", typeof(T));
                case "Vector3d":
                    return (T)Convert.ChangeType("Hello there", typeof(T));
                case "Point":
                    return (T)Convert.ChangeType(transform.position.ToPoint(), typeof(T));
                default:
                    throw new NotSupportedException(String.Format("TYpe {} is not support by the Datapoint class", typeof(T).Name));
            }
        }   
    }
}
