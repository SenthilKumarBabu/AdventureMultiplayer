using System;
using System.Data;
using System.Linq;
using System.Text;
using UnityEngine;
using DA_Assets.Shared.Extensions;

namespace DA_Assets.Extensions
{
    public static class OtherExtensions
    {

        /// <summary>
        /// Formats a <see cref="DataTable"/> as a multi-line string with column headers and row values,
        /// then logs it via <c>Debug.LogError</c> for immediate visibility in the Unity Console.
        /// Useful for inspecting in-memory CSV or database data during development.
        /// </summary>
        /// <param name="table">The DataTable to print. Logs a warning and returns early if <c>null</c>.</param>
        /// <param name="title">Optional label shown in the header line.</param>
        /// <param name="maxRows">Maximum number of rows to include; remaining rows are shown as a count summary.</param>
        public static void DebugLogTable(this DataTable table, string title = "DataTable", int maxRows = int.MaxValue)
        {
            if (table == null)
            {
                Debug.Log(SharedLocKey.log_null_datatable.Localize());
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"[{title}] Rows={table.Rows.Count}, Columns={table.Columns.Count}");

            string[] headers = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();
            sb.AppendLine(string.Join(" | ", headers));
            sb.AppendLine(new string('-', Math.Max(3, headers.Sum(h => h.Length) + (headers.Length - 1) * 3)));

            int rowCount = Math.Min(table.Rows.Count, maxRows);
            for (int r = 0; r < rowCount; r++)
            {
                DataRow row = table.Rows[r];
                var cells = table.Columns.Cast<DataColumn>()
                    .Select(c => FormatCell(row[c]));
                sb.AppendLine(string.Join(" | ", cells));
            }

            if (table.Rows.Count > rowCount)
                sb.AppendLine($"... {table.Rows.Count - rowCount} more rows");

            Debug.LogError(sb.ToString());
        }

        private static string FormatCell(object value)
        {
            if (value == null || value == DBNull.Value) return "";
            return value.ToString().Replace("\r", "\\r").Replace("\n", "\\n");
        }


        /// <summary>
        /// Returns <c>true</c> if the value is <c>null</c> or equals <c>default(T)</c>.
        /// Useful for null-checking structs and classes in a unified way.
        /// </summary>
        /// <typeparam name="T">The type of the object to check.</typeparam>
        /// <param name="obj">The value to test.</param>
        /// <returns><c>true</c> if the value is null or default; otherwise <c>false</c>.</returns>
        public static bool IsDefault<T>(this T obj)
        {
            if (obj == null)
            {
                return true;
            }

            return obj.Equals(default(T));
        }

        /// <summary>
        /// Checks whether a specific bit flag is set in an enum value without requiring the enum to have the
        /// <c>[Flags]</c> attribute. Uses a 64-bit integer cast for safe comparison.
        /// </summary>
        /// <typeparam name="T">The enum type.</typeparam>
        /// <param name="value">The composite enum value to test.</param>
        /// <param name="flag">The flag to check for.</param>
        /// <returns><c>true</c> if the flag bits are set in <paramref name="value"/>; otherwise <c>false</c>.</returns>
        public static bool IsFlagSet<T>(this T value, T flag) where T : struct
        {
            long lValue = Convert.ToInt64(value);
            long lFlag = Convert.ToInt64(flag);
            return (lValue & lFlag) != 0;
        }

        /// <summary>
        /// Creates a shallow copy of any serializable class instance by round-tripping through
        /// <c>JsonUtility.ToJson</c> / <c>JsonUtility.FromJson</c>.
        /// Fields not marked as serializable by Unity will not be copied.
        /// </summary>
        /// <typeparam name="T">The serializable class type.</typeparam>
        /// <param name="source">The object to copy.</param>
        /// <returns>A new instance with the same serialized field values.</returns>
        public static T CopyClass<T>(this T source)
        {
            string json = JsonUtility.ToJson(source);
            T copiedObject = JsonUtility.FromJson<T>(json);
            return copiedObject;
        }

        /// <summary>
        /// Extracts the numeric characters from a string in their original order.
        /// </summary>
        /// <param name="text">The input string. Returns empty string if null or whitespace.</param>
        /// <returns>A string containing only the digit characters from <paramref name="text"/>.</returns>
        /// <see href="https://stackoverflow.com/a/33784596/8265642"/>
        public static string GetNumbers(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            return new string(text.Where(p => char.IsDigit(p)).ToArray());
        }

        /// <summary>
        /// Unwraps a nullable boolean, treating <c>null</c> as <c>true</c>.
        /// Used in Figma JSON models where an absent field implies a default-on behavior.
        /// </summary>
        /// <param name="value">The nullable boolean to evaluate.</param>
        /// <returns><c>true</c> if the value is <c>null</c> or <c>true</c>; <c>false</c> only when explicitly set to <c>false</c>.</returns>
        public static bool ToBoolNullTrue(this bool? value)
        {
            if (value == null)
            {
                return true;
            }

            return value.Value;
        }

        /// <summary>
        /// Unwraps a nullable boolean, treating <c>null</c> as <c>false</c>.
        /// Used in Figma JSON models where an absent field implies a default-off behavior.
        /// </summary>
        /// <param name="value">The nullable boolean to evaluate.</param>
        /// <returns><c>false</c> if the value is <c>null</c> or <c>false</c>; <c>true</c> only when explicitly set to <c>true</c>.</returns>
        public static bool ToBoolNullFalse(this bool? value)
        {
            if (value == null)
            {
                return false;
            }

            return value.Value;
        }
    }
}
