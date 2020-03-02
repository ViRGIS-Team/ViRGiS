/* OrbCreationExtensions 1.0            */
/* By Orbcreation BV                    */
/* Richard Knol                         */
/* info@orbcreation.com                 */
/* March 31, 2015                       */
/* games, components and freelance work */

/* Note: if you also use other packages by Orbcreation,  */
/* you may end up with multiple copies of this file.     */
/* In that case, better delete/merge those files into 1. */

using UnityEngine;
using System.Collections;
using System;

namespace OrbCreationExtensions
{
	public static class Texture2DExtensions
    {
	    public static bool HasTransparency(this Texture2D tex) {
	    	Color[] pixels;
	    	try {
		        pixels = tex.GetPixels(0);
	        } catch(Exception e) {  // non readable texture
	        	Debug.Log(e);
	        	return false;
	        }
	        for (int i=0; i<pixels.Length; i++) {
	        	if(pixels[i].a < 1f) return true;
	        }
	        return false;
	    }
	}
}


