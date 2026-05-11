using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace MonsterTrainAccessibility.Core
{
    internal static class ReflectionUtil
    {
        private static readonly Dictionary<string, FieldInfo> FieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private static readonly Dictionary<string, PropertyInfo> PropertyCache = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);
        private static readonly HashSet<string> MissingMemberWarnings = new HashSet<string>(StringComparer.Ordinal);
        private static readonly object Sync = new object();

        public static T GetFieldValue<T>(object instance, string fieldName, string ownerName = null)
        {
            if (instance == null)
            {
                return default;
            }

            FieldInfo field = GetFieldInternal(instance.GetType(), fieldName, ownerName);
            object value = field?.GetValue(instance);
            return value is T typedValue ? typedValue : default;
        }

        public static T Get<T>(object instance, FieldInfo field)
        {
            if (instance == null || field == null)
            {
                return default;
            }

            object value = field.GetValue(instance);
            return value is T typedValue ? typedValue : default;
        }

        public static FieldInfo GetField(Type type, string fieldName, string ownerName = null)
        {
            if (type == null)
            {
                return null;
            }

            return GetFieldInternal(type, fieldName, ownerName);
        }

        public static PropertyInfo GetProperty(Type type, string propertyName, string ownerName = null)
        {
            if (type == null)
            {
                return null;
            }

            return GetPropertyInternal(type, propertyName, ownerName);
        }

        public static int? GetIntField(object instance, string fieldName, string ownerName = null)
        {
            if (instance == null)
            {
                return null;
            }

            FieldInfo field = GetFieldInternal(instance.GetType(), fieldName, ownerName);
            if (field == null)
            {
                return null;
            }

            object value = field.GetValue(instance);
            return value is int intValue ? intValue : (int?)null;
        }

        public static T GetPropertyValue<T>(object instance, string propertyName, string ownerName = null)
        {
            if (instance == null)
            {
                return default;
            }

            PropertyInfo property = GetPropertyInternal(instance.GetType(), propertyName, ownerName);
            object value = property?.GetValue(instance, null);
            return value is T typedValue ? typedValue : default;
        }

        private static FieldInfo GetFieldInternal(Type type, string fieldName, string ownerName)
        {
            string cacheKey = type.FullName + "::field::" + fieldName;
            lock (Sync)
            {
                if (!FieldCache.TryGetValue(cacheKey, out FieldInfo field))
                {
                    field = AccessTools.Field(type, fieldName);
                    FieldCache[cacheKey] = field;
                    if (field == null)
                    {
                        WarnMissingOnce(cacheKey, ownerName ?? type.FullName, "field", fieldName);
                    }
                }

                return field;
            }
        }

        private static PropertyInfo GetPropertyInternal(Type type, string propertyName, string ownerName)
        {
            string cacheKey = type.FullName + "::property::" + propertyName;
            lock (Sync)
            {
                if (!PropertyCache.TryGetValue(cacheKey, out PropertyInfo property))
                {
                    property = AccessTools.Property(type, propertyName);
                    PropertyCache[cacheKey] = property;
                    if (property == null)
                    {
                        WarnMissingOnce(cacheKey, ownerName ?? type.FullName, "property", propertyName);
                    }
                }

                return property;
            }
        }

        private static void WarnMissingOnce(string cacheKey, string ownerName, string memberType, string memberName)
        {
            if (!MissingMemberWarnings.Add(cacheKey))
            {
                return;
            }

            Log.Warn(ownerName + " missing " + memberType + " '" + memberName + "'. A game update may have renamed it.");
        }
    }
}
