using UnityEngine;
using UnityEngine.UI;

public class ShowSliderValue : MonoBehaviour {

	private static string str_Fmt1 = "{0:F1}";
	private static string str_Fmt2 = "{0:F2}";
	private static string str_Fmt3 = "{0:F3}";
	private static string str_Fmt4 = "{0:F4}";
	private static string str_Fmt5 = "{0:F5}";
	[Range(0,5)] public int decimals = 1;
	
	
	public void SetFloatValue (float aFloat) {
		string s;
		switch (decimals)
		{
			case 0:
				s = "" + Mathf.RoundToInt(aFloat);
				break;
			case 1:
				s = string.Format(str_Fmt1, aFloat);
				break;
			case 2:
				s = string.Format(str_Fmt2, aFloat);
				break;
			case 3:
				s = string.Format(str_Fmt3, aFloat);
				break;
			case 4:
				s = string.Format(str_Fmt4, aFloat);
				break;
			default:
				s = string.Format(str_Fmt5, aFloat);
				break;
		}

		Text t = gameObject.GetComponent<Text>();
		if (t != null) t.text = s;
	}
}
