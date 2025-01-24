using Newtonsoft.Json.Linq;
using System;
using System.Windows;

namespace IPTVChannelManager
{
    public static class ParseHelper
    {
        public static bool ToBool(this string value) => boolParse(value);

        public static bool boolParse(string value) => bool.TryParse(value, out bool result) == false ? false : result;

        public static bool? ToNullableBool(this string value) => boolNullParse(value);

        public static bool? boolNullParse(string value) => !string.IsNullOrWhiteSpace(value) && bool.TryParse(value, out bool result) ? result : (bool?)null;

        public static int ToInt(this string value) => intParse(value);

        public static int intParse(string value) => !int.TryParse(value, out int result) ? (int)(value.ToNullableDouble() ?? 0) : result;

        public static uint ToUInt(this string value) => uintParse(value);

        public static uint uintParse(string value) => !uint.TryParse(value, out uint result) ? (uint)(value.ToNullableDouble() ?? 0u) : result;

        public static int? ToNullableInt(this string value) => intNullParse(value);

        public static int? intNullParse(string value) => (string.IsNullOrWhiteSpace(value) || !int.TryParse(value, out int result)) ? null : (int?)result;

        public static long ToLong(this string value) => longParse(value);

        public static long longParse(string value) => !long.TryParse(value, out long result) ? 0L : result;

        public static ulong ToULong(this string value) => ulongParse(value);

        public static ulong ulongParse(string value) => !ulong.TryParse(value, out ulong result) ? 0L : result;

        public static double ToDouble(this string value) => doubleParse(value);

        public static double doubleParse(string value) => !double.TryParse(value, out double result) ? 0.0 : result;

        public static double? ToNullableDouble(this string value) => doubleNullParse(value);

        public static double? doubleNullParse(string value) => (string.IsNullOrWhiteSpace(value) || !double.TryParse(value, out double result)) ? null : (double?)result;

        public static DateTime ToDateTime(this string value) => DateTimeParse(value);

        public static DateTime DateTimeParse(string value) => DateTimeNullParse(value, DateTime.Now).Value;

        public static DateTime? ToNullableDateTime(this string value) => DateTimeNullParse(value);

        public static DateTime? DateTimeNullParse(string value, DateTime? @default = null)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                if (value.Length != Constants.DateTimeFormat.Length && value.Length > Constants.DateTimeFormat.Length - 4)
                {
                    int dot = value.LastIndexOf('.');
                    string dotMillisecond = dot > -1 ? value.Substring(dot) : string.Empty;
                    string ms = dotMillisecond.TrimStart('.');
                    string formattedMillisecond = ms.Length == 2 ? $"0{ms}" : (ms.Length == 1 ? $"00{ms}" : ms);
                    if (!string.IsNullOrEmpty(dotMillisecond))
                    {
                        value = value.Replace(dotMillisecond, $".{formattedMillisecond}");
                    }
                }
                return !DateTime.TryParseExact(value, new[] { Constants.DateTimeFormat, Constants.DateFormat, Constants.DateFormatInQuery }, null, System.Globalization.DateTimeStyles.None, out DateTime dateTime) ? @default : (DateTime?)dateTime;
            }
            return @default;
        }

        public static dynamic ConvertTo<T>(this object value)
        {
            dynamic result = ConvertTo(value, typeof(T));
            if (result.GetType() != typeof(T))
            {
                try
                {
                    return value.To<T>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Cannot cast type {typeof(T)}, {ex}");
                    return default;
                }
            }
            return result;
        }

        public static dynamic ConvertTo(this object value, Type type)
        {
            try
            {
                if (value is DBNull)
                {
                    value = string.Empty;
                }
                if (type == typeof(int))
                {
                    return value?.ToString()?.ToInt();
                }
                else if (type == typeof(uint))
                {
                    return value?.ToString()?.ToUInt();
                }
                else if (type == typeof(double))
                {
                    return value?.ToString()?.ToDouble();
                }
                if (type == typeof(ulong))
                {
                    return value?.ToString()?.ToULong();
                }
                if (type == typeof(long))
                {
                    return value?.ToString()?.ToLong();
                }
                else if (type == typeof(double?))
                {
                    return value?.ToString()?.ToNullableDouble();
                }
                else if (type == typeof(int?))
                {
                    return value?.ToString()?.ToNullableInt();
                }
                else if (type == typeof(bool))
                {
                    return value?.ToString()?.ToBool();
                }
                else if (type == typeof(bool?))
                {
                    return value?.ToString()?.ToNullableBool();
                }
                else if (type == typeof(DateTime))
                {
                    return value?.ToString()?.ToDateTime();
                }
                else if (type == typeof(DateTime?))
                {
                    return value?.ToString()?.ToNullableDateTime();
                }
                else if (type == typeof(JArray))
                {
                    return JArray.Parse(!string.IsNullOrWhiteSpace(value?.ToString()) ? value.ToString() : "[]");
                }
                else if (type == typeof(Point))
                {
                    return !string.IsNullOrWhiteSpace(value?.ToString()) ? Point.Parse(value.ToString()) : default;
                }
                else if (type.IsEnum)
                {
                    return !string.IsNullOrWhiteSpace(value?.ToString()) ? Enum.Parse(type, value.ToString()) : default;
                }
                else if (value is JObject jobject)
                {
                    return jobject.ToObject(type);
                }
                else
                {
                    return value;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}, {ex}");
                return value;
            }
        }
    }
}
