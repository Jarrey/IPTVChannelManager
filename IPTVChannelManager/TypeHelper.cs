using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;

namespace IPTVChannelManager
{
    public static class TypeHelper
    {
        [Pure]
        public static T PickObj<T>(this object[] objs, int i) where T : class => objs?.Length > i ? objs[i] as T : default;
        [Pure]
        public static T Pick<T>(this object[] objs, int i) where T : struct => Pick(objs, i, typeof(T));
        [Pure]
        public static dynamic Pick(this object[] objs, int i, Type type = null) => objs?.Length > i ? objs[i]?.ConvertTo(type ?? typeof(string)) ?? string.Empty : string.Empty;

        [Pure]
        public static T To<T>(this object self) => (T)self;
        [Pure]
        public static T As<T>(this object self) where T : class => self as T;

        public static bool IsNotNullAndNan(this double? value) => value.HasValue && !double.IsNaN(value.Value);

        public static bool IsNullOrNan(this double? value) => !value.HasValue || double.IsNaN(value.Value);

        public static bool IsNaN(this double value) => double.IsNaN(value);

        public static bool IsZero(this double? i)
        {
            return i.HasValue && IsZero(i.Value);
        }

        public static bool IsZero(this double i)
        {
            return Math.Abs(i) < Constants.Epsilon;
        }

        public static bool IsNotZero(this double? i)
        {
            return i.HasValue && !IsZero(i.Value);
        }

        public static bool IsValid(this double? value)
        {
            return value != null && Math.Abs(value.Value) > Constants.Epsilon;
        }

        public static bool IsValidNonZeroValue(this double? dVal)
        {
            return dVal != null && !double.IsNaN(dVal.Value) && IsValid(dVal.Value);
        }

        public static double? RemoveNaN(this double? value) => value.IsNullOrNan() ? null : value;

        public static double? RemoveNaN(this double value) => double.IsNaN(value) ? null : value;

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> items)
        {
            return items == null || !items.Any();
        }

        #region Check type contains the specific property and value
        public static bool IsContains<T>(this T obj, string key, StringComparison comparisonType = StringComparison.CurrentCulture)
        {
            return IsContains(key, obj, comparisonType);
        }

        public static bool IsContains<T>(string key, T obj, StringComparison comparisonType = StringComparison.CurrentCulture)
        {
            return IsContains(typeof(T), key, obj, comparisonType);
        }

        public static bool IsContains(this object obj, Type type, string key, StringComparison comparisonType = StringComparison.CurrentCulture)
        {
            return IsContains(type, key, obj, comparisonType);
        }

        public static bool IsContains(Type type, string key, object obj, StringComparison comparisonType = StringComparison.CurrentCulture)
        {
            if (string.IsNullOrWhiteSpace(key)) return true;
            PropertyInfo[] props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            return props?.Any(p => !p.Name.Equals("Key") && p.GetValue(obj, null)?.ToString().Contains(key, comparisonType) == true) == true;
        }

        public static bool IsContains<T>(this T obj, string property, string key, StringComparison comparisonType = StringComparison.CurrentCulture)
        {
            return IsContains(property, key, obj, comparisonType);
        }

        public static bool IsContains<T>(string property, string key, T obj, StringComparison comparisonType = StringComparison.CurrentCulture)
        {
            if (string.IsNullOrWhiteSpace(property) || string.IsNullOrWhiteSpace(key)) return true;
            PropertyInfo[] props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            return props?.Any(p => p.Name.Equals(property) && p.GetValue(obj, null)?.ToString().Contains(key, comparisonType) == true) == true;
        }

        #endregion

        #region Double compare
        public static bool IsEquals(this double? d1, double? d2) => d1 != null && d2 != null && d1.Value.IsEquals(d2.Value);
        public static bool IsEquals(this double d1, double d2) => Math.Abs(d1 - d2) < Constants.Epsilon;
        public static bool MoreThan(this double? d1, double? d2) => !d1.IsEquals(d2) && (d1 > d2);
        public static bool MoreThan(this double d1, double d2) => !d1.IsEquals(d2) && (d1 > d2);
        public static bool LessThan(this double? d1, double? d2) => !d1.IsEquals(d2) && (d1 < d2);
        public static bool LessThan(this double d1, double d2) => !d1.IsEquals(d2) && (d1 < d2);
        #endregion Double compare

        #region Pickup one field value from object
        public static object PickField<T>(this object obj, string field, object @default = null)
            => obj.PickField(typeof(T), field, @default);

        public static object PickField(this object obj, Type type, string field, object @default = null)
        {
            if (string.IsNullOrWhiteSpace(field)) return true;
            PropertyInfo[] props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            return props?.FirstOrDefault(p => p.Name == field)?.GetValue(obj) ?? @default;
        }
        #endregion

        #region Reflactor to get property value by binding path
        public static object GetPropertyValue(this object obj, string propertyName)
        {
            Func<object, Type, string, object> getValueFunc = (o, t, p) =>
            {
                PropertyInfo propInfo = t.GetProperty(p);
                return propInfo != null && o != null ? propInfo.GetValue(o) : default;
            };

            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                string[] propPath = propertyName.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (propPath?.Length > 0)
                {
                    object temp = obj;
                    foreach (string prop in propPath)
                    {
                        if (temp == null)
                        {
                            break;
                        }
                        temp = getValueFunc(temp, temp.GetType(), prop);
                    }
                    return temp;
                }
            }
            return default;
        }
        #endregion

        #region Type extesions
        public static Type[] GetDirectInterfaces(this Type type)
        {
            Type[] allInterfaces = type.GetInterfaces();
            return allInterfaces.Except(allInterfaces.SelectMany(t => t.GetInterfaces())).ToArray();
        }
        #endregion Type extesions
    }
}
