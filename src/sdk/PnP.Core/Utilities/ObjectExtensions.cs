﻿using PnP.Core.Model;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace PnP.Core.Utilities
{
    internal static class ObjectExtensions
    {
        /// <summary>
        /// Retrieves the value of a public, instance property 
        /// </summary>
        /// <param name="source">The source object</param>
        /// <param name="propertyName">The property name, case insensitive</param>
        /// <returns>The property value, if any</returns>
        internal static Object GetPublicInstancePropertyValue(this Object source, String propertyName)
        {
            return (source?.GetType()?.GetProperty(propertyName,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.IgnoreCase)?
                .GetValue(source));
        }

        /// <summary>
        /// Retrieves a public, instance property 
        /// </summary>
        /// <param name="source">The source object</param>
        /// <param name="propertyName">The property name, case insensitive</param>
        /// <returns>The property, if any</returns>
        internal static PropertyInfo GetPublicInstanceProperty(this Object source, String propertyName)
        {
            return (source?.GetType()?.GetProperty(propertyName,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.IgnoreCase));
        }

        /// <summary>
        /// Sets the value of a public, instance property 
        /// </summary>
        /// <param name="source">The source object</param>
        /// <param name="propertyName">The property name, case insensitive</param>
        /// <param name="value">The value to set</param>
        internal static void SetPublicInstancePropertyValue(this Object source, String propertyName, object value)
        {
            source?.GetType()?.GetProperty(propertyName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.IgnoreCase)?
                .SetValue(source, value);
        }

        /// <summary>
        /// Indicates whether 2 types are compatible for mapping
        /// </summary>
        /// <param name="sourceType">The type of the property to map the value from</param>
        /// <param name="targetType">The type of the property to map the value to</param>
        /// <returns><c>true</c> if the types are compatible, <c>false</c> otherwise</returns>
        private static bool AreMappingCompatible(Type sourceType, Type targetType)
        {
            if (!targetType.IsAssignableFrom(sourceType))
            {
                // Enum <=> int is supported
                if ((targetType.IsEnum && sourceType == typeof(int)) || (sourceType.IsEnum && targetType == typeof(int)))
                    return true;

                // Nullable<T> <=> T is supported (null => default)
                if (Nullable.GetUnderlyingType(targetType) == sourceType || Nullable.GetUnderlyingType(sourceType) == targetType)
                    return true;

                return false;
            }

            return true;
        }

        internal static Dictionary<string, object> AsKeyValues(this object obj,
                    BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase,
                    string[] ignoreProperties = null,
                    bool ignoreNullValues = false)
        {
            if (obj == null)
                return new Dictionary<string, object>();

            PropertyInfo[] propsInfo = obj.GetType().GetProperties(bindingFlags);

            var qActualPropsQuery = from pi in propsInfo
                                    select pi;

            if (ignoreProperties != null)
            {
                qActualPropsQuery = from pi in qActualPropsQuery
                                    where !ignoreProperties.Contains(pi.Name)
                                    select pi;
            }

            var qActualPropsKVPQuery = ignoreNullValues
                ? from pi in qActualPropsQuery
                  let key = pi.Name
                  let value = pi.GetValue(obj)
                  where value != null
                  select new { key, value }
                : from pi in qActualPropsQuery
                  let key = pi.Name
                  let value = pi.GetValue(obj)
                  select new { key, value };

            return qActualPropsKVPQuery.ToDictionary(k => k.key, v => v.value);
        }

        internal static ExpandoObject AsExpando(this object obj,
                    BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase,
                    string[] ignoreProperties = null,
                    bool ignoreNullValues = false)
        {
            var dict = AsKeyValues(obj, bindingFlags, ignoreProperties, ignoreNullValues);
            var expando = new ExpandoObject();
            foreach (var kvp in dict)
            {
                expando.SetProperty(kvp.Key, kvp.Value);
            }
            return expando;
        }
    }
}
