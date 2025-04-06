using System;
using System.Collections.Generic;
using System.Reflection;

namespace Oxide.Ext.AdminPanel
{

    /// <summary>
    /// Helper class to getting values from attribute for ws
    /// </summary>
    public static class WebSocketExposeHelper
    {
        public static Dictionary<string, object> GetExposedValues(object obj)
        {
            var exposedValues = new Dictionary<string, object>();


            var fields = obj.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (field.GetCustomAttribute<WebSocketExposeAttribute>() != null)
                {
                    exposedValues[field.Name] = field.GetValue(obj);
                }
            }

            return exposedValues;
        }
    }

}
