using System;
using System.IO;
using System.Linq;
using BepInEx;
using UnityEngine;

namespace Veilheim.Util
{
    public static class Utils
    {
        public static void PrintObject(object o)
        {
            if (o == null)
            {
                Debug.Log("null");
            }
            else
            {
                Debug.Log(o + ":\n" + GetObjectString(o, "  "));
            }
        }

        public static string GetObjectString(object obj, string indent)
        {
            var output = "";
            Type type = obj.GetType();
            var publicFields = type.GetFields().Where(f => f.IsPublic);
            foreach (var f in publicFields)
            {
                var value = f.GetValue(obj);
                var valueString = value == null ? "null" : value.ToString();
                output += $"\n{indent}{f.Name}: {valueString}";
            }

            return output;
        }
    }
}
