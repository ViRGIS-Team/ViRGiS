/* MIT License

Copyright (c) 2020 - 21 Runette Software

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice (and subsidiary notices) shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. */

using UnityEngine;

namespace Virgis
{

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
}
