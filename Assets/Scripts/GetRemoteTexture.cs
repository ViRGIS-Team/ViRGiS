// from https://stackoverflow.com/questions/31765518/how-to-load-an-image-from-url-with-unity
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class Network { 

    public static async Task<Texture2D> GetRemoteTexture(string url)
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
            if (www.isNetworkError || www.isHttpError)
            {
                //log error:
                #if DEBUG
                Debug.Log($"{ www.error }, URL:{ www.url }");
                #endif

                //nothing to return on error:
                return null;
            }
            else
            {
                //return valid results:
                return DownloadHandlerTexture.GetContent(www);
            }
        }
    }

}