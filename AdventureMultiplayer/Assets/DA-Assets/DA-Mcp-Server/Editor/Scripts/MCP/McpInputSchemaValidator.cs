using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

#if JSONNET_EXISTS
using Newtonsoft.Json.Linq;
#endif

namespace DA_Assets.Shared.MCP
{
    public static class McpInputSchemaValidator
    {
        public static bool TryValidate(InputSchema schema, Dictionary<string, object> args, out string error)
        {
            args ??= new Dictionary<string, object>();
            error = null;

            if (!string.IsNullOrEmpty(schema.Type) && schema.Type != "object")
            {
                error = $"Root schema type must be object, got '{schema.Type}'.";
                return false;
            }

            if (schema.Required != null)
            {
                foreach (string key in schema.Required.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    if (!args.ContainsKey(key) || args[key] == null)
                    {
                        error = $"Missing required argument '{key}'.";
                        return false;
                    }
                }
            }

            if (schema.Properties == null)
            {
                return true;
            }

            foreach (string key in args.Keys)
            {
                if (!schema.Properties.ContainsKey(key))
                {
                    error = $"Unknown argument '{key}'.";
                    return false;
                }
            }

            foreach (KeyValuePair<string, object> arg in args)
            {
                PropertySchema prop = schema.Properties[arg.Key];
                if (!MatchesType(arg.Value, prop.Type))
                {
                    error = $"Argument '{arg.Key}' must be {prop.Type}.";
                    return false;
                }
            }

            return true;
        }

        private static bool MatchesType(object value, string schemaType)
        {
            if (value == null || string.IsNullOrEmpty(schemaType))
                return true;

            value = Unwrap(value);

            return schemaType switch
            {
                "string" => value is string,
                "boolean" => value is bool,
                "integer" => IsInteger(value),
                "number" => IsNumber(value),
                "array" => IsArray(value),
                "object" => IsObject(value),
                _ => true
            };
        }

        private static bool IsArray(object value)
        {
#if JSONNET_EXISTS
            if (value is JArray)
                return true;
#endif
            return value is IEnumerable && value is not string && value is not IDictionary;
        }

        private static bool IsObject(object value)
        {
#if JSONNET_EXISTS
            if (value is JObject)
                return true;
#endif
            return value is IDictionary;
        }

        private static bool IsInteger(object value)
        {
            if (value is byte or sbyte or short or ushort or int or uint or long or ulong)
                return true;

            if (value is float or double or decimal)
            {
                double numeric = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return Math.Abs(numeric % 1) < double.Epsilon;
            }

            return false;
        }

        private static bool IsNumber(object value)
        {
            return value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;
        }

        private static object Unwrap(object value)
        {
#if JSONNET_EXISTS
            if (value is JValue jValue)
                return jValue.Value;
#endif
            return value;
        }
    }
}
