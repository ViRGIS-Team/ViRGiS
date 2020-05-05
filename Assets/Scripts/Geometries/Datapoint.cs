
// copyright Runette Software Ltd, 2020. All rights reserved﻿using System.Collections;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace Virgis
{


    /// <summary>
    /// Controls an instance of a data pointor handle
    /// </summary>
    public class Datapoint : VirgisComponent
    {
        private Renderer thisRenderer; // convenience link to the rendere for this marker
        private bool newSelect = false;
        private Vector3 moveOffset = Vector3.zero;

        void Start()
        {
            thisRenderer = GetComponent<Renderer>();
            if (color != null)
            {
                thisRenderer.material.SetColor("_BaseColor", color);
            }
            if (transform.childCount > 0) label = transform.GetChild(0);
        }
        /// <summary>
        /// Every frame - realign the billboard
        /// </summary>
        void Update()
        {
            if (label) label.LookAt(Global.mainCamera.transform);

        }


        public override void Selected(SelectionTypes button)
        {
            newSelect = true;
            thisRenderer.material.SetColor("_BaseColor", anticolor);
            if (button != SelectionTypes.BROADCAST)
            {
                gameObject.transform.parent.gameObject.SendMessageUpwards("Selected", button, SendMessageOptions.DontRequireReceiver);
            }
        }


        public override void UnSelected(SelectionTypes button)
        {
            thisRenderer.material.SetColor("_BaseColor", color);
            if (button != SelectionTypes.BROADCAST)
            {
                gameObject.transform.parent.gameObject.SendMessageUpwards("UnSelected", button, SendMessageOptions.DontRequireReceiver);
                MoveArgs args = new MoveArgs();
                switch (AppState.instance.editSession.mode)
                {
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
                        args.pos = transform.position.Round(Global.Map.transform.TransformVector(Vector3.one * (Global.project.ContainsKey("GridScale") && Global.project.GridScale != 0 ? Global.project.GridScale :  1f)).magnitude);;
                        args.id = id;
                        args.translate = args.pos - transform.position;
                        SendMessageUpwards("Translate", args, SendMessageOptions.DontRequireReceiver);
                        break;
                }
            }
        }

 
        public override void SetColor(Color newColor)
        {
            color = newColor;
            anticolor = Color.white - newColor;
            anticolor.a = color.a;
            Renderer thisRenderer = GetComponent<Renderer>();
            if (thisRenderer)
            {
                thisRenderer.material.SetColor("_BaseColor", color);
            }
        }


        public override void MoveTo(Vector3 newPos)
        {
            if (newSelect)
            {
                newSelect = false;
                moveOffset = newPos - transform.position;
            }
            else
            {
                MoveArgs args = new MoveArgs();
                args.translate = newPos - transform.position - moveOffset;
                args.oldPos = transform.position;
                args.id = id;
                args.pos = newPos;
                SendMessageUpwards("Translate", args, SendMessageOptions.DontRequireReceiver);
            }
        }

        /// <summary>
        ///  Sent by the parent entity to request this market to move as part of an entity move
        /// </summary>
        /// <param name="argsin">MoveArgs</param>
        void TranslateHandle(MoveArgs argsin)
        {
            if (argsin.id == id && argsin.pos != transform.position)
            {
                MoveArgs argsout = new MoveArgs();
                argsout.oldPos = transform.position;
                transform.position = argsin.pos;
                argsout.id = id;
                argsout.pos = transform.position;
                SendMessageUpwards("VertexMove", argsout, SendMessageOptions.DontRequireReceiver);
            }
        }

        public override void EditEnd()
        {

        }

        public override void MoveAxis(MoveArgs args)
        {
            if (args.pos == null)
            {
                args.translate = args.pos - transform.position;
            }
            else
            {
                args.pos = transform.position;
            }
            transform.parent.SendMessageUpwards("MoveAxis", args, SendMessageOptions.DontRequireReceiver);
        }

        public override void VertexMove(MoveArgs args)
        {
            
        }

        public override void Translate(MoveArgs args)
        {
            
        }
    }
}
