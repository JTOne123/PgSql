﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;

namespace doob.PgSql.ExtensionMethods
{
    public static class TypeExtensions
    {
        public static bool IsBasicDotNetType(this Type type)
        {

            if (type == null)
                return false;

            if (type.IsPrimitive)
                return true;

            if (type == typeof(String))
                return true;

            if (type == typeof(DateTime))
                return true;

            if (type == typeof(Guid))
                return true;

            if (type == typeof(Uri))
                return true;

            if (type == typeof(TimeSpan))
                return true;

            if (type.Assembly == typeof(object).Assembly)
                return true;

            return false;
        }

        public static bool IsNullable(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static Type GetInnerTypeFromNullable(this Type nullableType)
        {
            return nullableType.GetGenericArguments()[0];
        }

        public static bool IsDictionaryType(this Type type)
        {
            var implementedInterfaces = type.GetInterfaces();
            var ret = implementedInterfaces.Contains(typeof(IDictionary));
            return ret;
        }

        public static bool IsListType(this Type type)
        {
            if (type.IsDictionaryType())
                return false;

            var targetType = typeof(IList<>);
            var implementedInterfaces = type.GetInterfaces();
            var ret = implementedInterfaces.Any(i => i.GetTypeInfo().IsGenericType
                                                     && i.GetGenericTypeDefinition() == targetType);
            if (!ret)
                ret = implementedInterfaces.Contains(typeof(IEnumerable));

            return ret;
        }

        private static readonly ConcurrentDictionary<Type, JTokenType> JTokenTypeCache = new ConcurrentDictionary<Type, JTokenType>();
        internal static JTokenType GetJTokenType(this Type type)
        {

            if (JTokenTypeCache.TryGetValue(type, out JTokenType jType))
            {
                return jType;
            };


            var jsonType = JTokenType.Undefined;
            if (type == typeof(string))
            {
                jsonType = JTokenType.String;
            }

            if (jsonType != JTokenType.Undefined)
            {
                return JTokenTypeCache.AddOrUpdate(type, jsonType, (type1, tokenType) => jsonType);
            }

            try
            {
                var inst = Activator.CreateInstance(type);
                var jtype = JSON.ToJToken(inst).Type;
                return JTokenTypeCache.AddOrUpdate(type, jtype, (type1, tokenType) => jtype);
            }
            catch
            {
                // ignored
            }

            return JTokenType.Object;
        }


        /// Checks whether the specified type is either a primitive, an enum, a string or a decimal.
        public static bool IsSimple(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsPrimitive
                   || typeInfo.IsEnum
                   || typeInfo.Equals(typeof(string))
                   || typeInfo.Equals(typeof(decimal));
        }

        /// Checks whether the specified type is anonymous.
        public static bool IsAnonymous(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.GetCustomAttribute<CompilerGeneratedAttribute>() != null
                   && typeInfo.IsGenericType
                   && typeInfo.Name.Contains("AnonymousType")
                   && (typeInfo.Name.StartsWith("<>") || typeInfo.Name.StartsWith("VB$"))
                   && typeInfo.Attributes.HasFlag(TypeAttributes.NotPublic);
        }

        /// Returns specified type's public and settable properties.
        public static PropertyInfo[] GetPublicSettableProperties(this Type type)
        {
            return type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.GetSetMethod() != null)
                .ToArray();
        }
    }
}
