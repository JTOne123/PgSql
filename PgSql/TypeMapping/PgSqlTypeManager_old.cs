﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using doob.PgSql.CustomTypes;
using doob.PgSql.ExtensionMethods;
using Newtonsoft.Json.Linq;
using NpgsqlTypes;

namespace doob.PgSql.TypeMapping
{
    public static class PgSqlTypeManagerOLD
    {
        static PgSqlTypeManagerOLD()
        {
            PostgresNameDictionary["jsonb"].ClrTypes = new[] { typeof(Dictionary<string, object>) };

            AddPostgresNameDictionary("ltree", "text", NpgsqlDbType.Unknown, typeof(PgSqlLTree));
            //AddPostgresNameDictionary("secstring", null, NpgsqlDbType.Enum, typeof(PgSqlSecureString));


            //var tma = new TypeMappingAttribute();
            //tma.NpgsqlDbType = NpgsqlDbType.Enum;
            //tma.ClrTypes = new[] { typeof(Enum) };
            //tma.PgName = "jsonb";
            //HandlerTypesByNpsgqlDbType.Add(NpgsqlDbType.Enum, tma);


        }


        private static void AddPostgresNameDictionary(string name, string pgTypeNeeded, NpgsqlDbType npgsqlDbType, params Type[] dotneTypes)
        {
            var attr = new TypeMappingAttribute();
            attr.PgName = name;
            attr.NpgsqlDbType = npgsqlDbType;
            attr.ClrTypes = dotneTypes;
            attr.PgTypeNeeded = pgTypeNeeded;

            PostgresNameDictionary.Add(name, attr);
        }

        private static readonly Lazy<Type> _typeHandlerRegistry = new Lazy<Type>(() => Type.GetType("Npgsql.TypeHandlerRegistry, Npgsql"));
        private static Type TypeHandlerRegistry => _typeHandlerRegistry.Value;

        private static IDictionary GetInternalTypeHandlerRegistryDictionary(string fieldName, BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Static)
        {
            var field = (IDictionary)TypeHandlerRegistry.GetField(fieldName, bindingFlags)?.GetValue(TypeHandlerRegistry);
            return field;
        }

        private static object GetFieldValue(object @object, string fieldName, BindingFlags bindingFlags)
        {
            return GetFieldValue<object>(@object, fieldName, bindingFlags);
        }
        private static T GetFieldValue<T>(object @object, string fieldName, BindingFlags bindingFlags)
        {
            return (T)@object.GetType().GetField(fieldName, bindingFlags)?.GetValue(@object);
        }
        private static T GetPropertyValue<T>(object @object, string fieldName, BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance)
        {
            return (T)@object.GetType().GetProperty(fieldName, bindingFlags)?.GetValue(@object);
        }


        private static readonly Lazy<Dictionary<string, TypeMappingAttribute>> _lazyHandlerTypes = new Lazy<Dictionary<string, TypeMappingAttribute>>(() =>
        {
            var ret = GetInternalTypeHandlerRegistryDictionary("HandlerTypes").CastDict().ToDictionary(
                e => (string)e.Key,
                e =>
                {
                    var va = GetFieldValue(e.Value, "Mapping", BindingFlags.NonPublic | BindingFlags.Instance);
                    var typem = new TypeMappingAttribute
                    {
                        PgName = GetPropertyValue<string>(va, "PgName"),
                        ClrTypes = GetPropertyValue<Type[]>(va, "ClrTypes"),
                        NpgsqlDbType = GetPropertyValue<NpgsqlDbType?>(va, "NpgsqlDbType")
                    };
                    return typem;
                }, StringComparer.OrdinalIgnoreCase);
            return ret;
        });
        private static Dictionary<string, TypeMappingAttribute> PostgresNameDictionary => _lazyHandlerTypes.Value;

        private static readonly Lazy<Dictionary<Type, NpgsqlDbType>> _lazyTypeToNpgsqlDbType = new Lazy<Dictionary<Type, NpgsqlDbType>>(() =>
        {
            var ret = GetInternalTypeHandlerRegistryDictionary("TypeToNpgsqlDbType").CastDict().ToDictionary(e => (Type)e.Key, e => (NpgsqlDbType)e.Value);
            return ret;
        });
        private static Dictionary<Type, NpgsqlDbType> TypeToNpgsqlDbType => _lazyTypeToNpgsqlDbType.Value;

        private static readonly Lazy<Dictionary<NpgsqlDbType, TypeMappingAttribute>> _lazyHandlerTypesByNpsgqlDbType = new Lazy<Dictionary<NpgsqlDbType, TypeMappingAttribute>>(() =>
        {
            var ret = GetInternalTypeHandlerRegistryDictionary("HandlerTypesByNpsgqlDbType").CastDict().ToDictionary(
                e => (NpgsqlDbType)e.Key,
                e =>
                {
                    var va = GetFieldValue(e.Value, "Mapping", BindingFlags.NonPublic | BindingFlags.Instance);
                    var typem = new TypeMappingAttribute
                    {
                        PgName = GetPropertyValue<string>(va, "PgName"),
                        ClrTypes = GetPropertyValue<Type[]>(va, "ClrTypes"),
                        NpgsqlDbType = GetPropertyValue<NpgsqlDbType?>(va, "NpgsqlDbType")
                    };
                    return typem;
                });


            return ret;
        });
        private static Dictionary<NpgsqlDbType, TypeMappingAttribute> HandlerTypesByNpsgqlDbType => _lazyHandlerTypesByNpsgqlDbType.Value;

        private static readonly Lazy<Dictionary<string, Type>> _lazyDotNetTypeAlias = new Lazy<Dictionary<string, Type>>(() =>
        {
            return new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                {"bool", typeof(Boolean)},
                {"byte", typeof(Byte)},
                {"sbyte", typeof(SByte)},
                {"char", typeof(Char)},
                {"decimal", typeof(Decimal)},
                {"double", typeof(Double)},
                {"float", typeof(Single)},
                {"int", typeof(Int32)},
                {"uint", typeof(UInt32)},
                {"long", typeof(Int64)},
                {"ulong", typeof(UInt64)},
                {"object", typeof(Object)},
                {"short", typeof(Int16)},
                {"ushort", typeof(UInt16)},
                {"string", typeof(String)},
                {"integer", typeof(Int32)},
                {"number", typeof(Int64) }
            };
        });
        private static Dictionary<string, Type> DotNetTypeAlias => _lazyDotNetTypeAlias.Value;



        private static TypeMappingAttribute _geTypeMappingAttributeFromPostgresName(string postgresName)
        {
            TypeMappingAttribute tma;
            if (PostgresNameDictionary.TryGetValue(postgresName, out tma))
            {
                return tma;
            }
            return null;
        }
        private static TypeMappingAttribute _geTypeMappingAttributeFromNpgsqlDbType(NpgsqlDbType npgsqlDbType)
        {
            TypeMappingAttribute tma;
            if (HandlerTypesByNpsgqlDbType.TryGetValue(npgsqlDbType, out tma))
            {
                return tma;
            }
            return null;
        }

        public static NpgsqlDbType GetNpgsqlDbType(string postgresName)
        {
            var tma = _geTypeMappingAttributeFromPostgresName(postgresName);
            if (tma != null)
                return tma.NpgsqlDbType ?? NpgsqlDbType.Unknown;

            var isArray = postgresName.EndsWith("[]");
            if (isArray)
            {
                postgresName = postgresName.TrimEnd("[]".ToCharArray()).TrimStart("_".ToCharArray());
                tma = _geTypeMappingAttributeFromPostgresName(postgresName);
                if (tma != null)
                {
                    var npgsqlDbType = tma.NpgsqlDbType ?? NpgsqlDbType.Unknown;
                    return npgsqlDbType | NpgsqlDbType.Array;
                }
            }
            throw new NotSupportedException($"Can't infer Type for PostgresName '{postgresName}'");
        }
        public static Type GetDotNetType(string postgresName)
        {
            var tma = _geTypeMappingAttributeFromPostgresName(postgresName);
            if (tma != null)
            {
                var ret = tma.ClrTypes.FirstOrDefault(t => t.IsBasicDotNetType());
                if (ret != null)
                    return ret;

                return tma.ClrTypes.FirstOrDefault();
            }


            var isArray = postgresName.EndsWith("[]");

            if (isArray)
            {
                postgresName = postgresName.TrimEnd("[]".ToCharArray()).TrimStart("_".ToCharArray());
                tma = _geTypeMappingAttributeFromPostgresName(postgresName);
                if (tma != null)
                    return typeof(List<>).MakeGenericType(tma.ClrTypes.FirstOrDefault());
            }


            throw new NotSupportedException($"Can't infer Type for PostgresName '{postgresName}'");
        }


        public static NpgsqlDbType GetNpgsqlDbType(Type dotnetType)
        {
            if (dotnetType == null)
                return NpgsqlDbType.Unknown;

            if (dotnetType.IsNullable())
                return GetNpgsqlDbType(dotnetType.GetInnerTypeFromNullable());



            if (dotnetType == typeof(PgSqlLTree))
                return NpgsqlDbType.Unknown;

            //if (dotnetType == typeof(PgSqlLTree))
            //    return NpgsqlDbType.Text;

            if (dotnetType.GetTypeInfo().IsEnum)
                return NpgsqlDbType.Text;

            if (dotnetType == typeof(JValue))
                return NpgsqlDbType.Jsonb;

            if (dotnetType == typeof(JToken))
                return NpgsqlDbType.Jsonb;

            if (dotnetType == typeof(JObject))
                return NpgsqlDbType.Jsonb;

            if (dotnetType.IsDictionaryType())
                return NpgsqlDbType.Jsonb;


            NpgsqlDbType npgsqlDbType;
            if (TypeToNpgsqlDbType.TryGetValue(dotnetType, out npgsqlDbType))
                return npgsqlDbType;


            if (dotnetType.IsArray)
            {
                if (dotnetType == typeof(byte[]))
                    return NpgsqlDbType.Bytea;

                return NpgsqlDbType.Array | GetNpgsqlDbType(dotnetType.GetElementType());
            }

            if (dotnetType.IsListType())
            {
                if (dotnetType == typeof(byte[]))
                    return NpgsqlDbType.Bytea;

                Type t = dotnetType;
                if (dotnetType.GetTypeInfo().BaseType != null && dotnetType.GetTypeInfo().BaseType.IsListType())
                    t = dotnetType.GetTypeInfo().BaseType;

                var genericType = t.GetGenericArguments().FirstOrDefault();

                return NpgsqlDbType.Array | GetNpgsqlDbType(genericType);
            }

            if (dotnetType.GetTypeInfo().IsGenericType && dotnetType.GetGenericTypeDefinition() == typeof(NpgsqlRange<>))
                return NpgsqlDbType.Range | GetNpgsqlDbType(dotnetType.GetGenericArguments()[0]);

            if (dotnetType == typeof(DBNull))
                return NpgsqlDbType.Unknown;



            return NpgsqlDbType.Jsonb;
        }
        public static string GetPostgresName(Type dotnetType)
        {
            if (dotnetType == typeof(PgSqlLTree))
                return "ltree";

            if (dotnetType == typeof(PgSqlSecureString))
                return "secstring";

            var npgsqlDbType = GetNpgsqlDbType(dotnetType);
            return GetPostgresName(npgsqlDbType);
        }

        public static string GetPostgresName(NpgsqlDbType npgsqlDbType)
        {
            TypeMappingAttribute tma;
            bool isArray = npgsqlDbType.HasFlag(NpgsqlDbType.Array);

            if (isArray)
            {
                npgsqlDbType = npgsqlDbType & ~NpgsqlDbType.Array;
                tma = _geTypeMappingAttributeFromNpgsqlDbType(npgsqlDbType);
                if (tma != null)
                    return $"{tma.PgName}[]";
            }

            tma = _geTypeMappingAttributeFromNpgsqlDbType(npgsqlDbType);
            if (tma != null)
                return tma.PgName;

            throw new NotSupportedException("Can't infer PostgresName for type " + (object)npgsqlDbType);
        }
        public static Type GetDotNetType(NpgsqlDbType npgsqlDbType)
        {
            var postgresName = GetPostgresName(npgsqlDbType);
            return GetDotNetType(postgresName);
        }


        //public static string GetDotNetAlias(string alias)
        //{
        //    if (PgSqlTypeManager.DotNetTypeAlias.ContainsKey(alias))
        //        alias = PgSqlTypeManager.DotNetTypeAlias[alias];

        //    return alias;
        //}

        public static string PgNameNeeded(string pgName)
        {
            if (String.IsNullOrWhiteSpace(pgName)) return null;
            return PostgresNameDictionary.ContainsKey(pgName) ? PostgresNameDictionary[pgName].PgTypeNeeded : null;
        }

        public static object ConvertTo(object @object, NpgsqlDbType type)
        {
            if (@object == null)
                return null;

            JToken jToken = null;

            if (@object is JToken)
                jToken = (JToken)@object;

            if (@object is string)
            {
                if (jToken == null)
                {
                    try
                    {
                        jToken = JToken.FromObject(@object.ToString());
                    }
                    catch { }
                }

            }

            if (jToken == null)
            {
                jToken = JSON.ToJToken(@object); // JToken.FromObject(@object, JsonSerializer.Create(Json.JsonSerializerSettings));
            }

            switch (type)
            {
                case NpgsqlDbType.Uuid:
                {
                    return jToken.ToObject<Guid>();
                }
                case NpgsqlDbType.Bytea:
                {
                    return @object;
                }
                default:
                {
                    return @object;
                }
            }
        }

    }
}
