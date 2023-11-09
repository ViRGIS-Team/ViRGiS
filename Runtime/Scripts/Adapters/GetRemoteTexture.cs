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

// from https://stackoverflow.com/questions/31765518/how-to-load-an-image-from-url-with-unity
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System;


namespace Virgis
{
    public class TextureImage
    {

        public static async Task<Texture2D> Get(Uri url)
        {
            using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
            {
                //begin requenst:
                var asyncOp = www.SendWebRequest();

                //await until it's done: 
                while (asyncOp.isDone == false)
                {
                    await Task.Delay(1000 / 30);//30 hertz
                }

                //read results:
                if (www.result == UnityWebRequest.Result.ConnectionError)
                {
                    //log error:

                    Debug.Log($"{ www.error }, URL:{ www.url }");


                    //nothing to return on error:
                    return null;
                }
                else
                {
                    //return valid results:
                    Texture2D tex =  DownloadHandlerTexture.GetContent(www);
                    return tex;
                }
            }
        }

    }
}
