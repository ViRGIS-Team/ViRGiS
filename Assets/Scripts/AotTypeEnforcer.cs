using Newtonsoft.Json.Utilities;
using UnityEngine;
using Newtonsoft.Json.Converters;
 
public class AotTypeEnforcer : MonoBehaviour
{
    public void Awake()
    {
        AotHelper.EnsureType<StringEnumConverter>();
    }
}