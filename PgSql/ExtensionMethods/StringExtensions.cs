﻿using System;
using System.Text.RegularExpressions;

namespace doob.PgSql.ExtensionMethods
{
    public static class StringExtensions
    {
        public static string TrimToNull(this string input, params string[] trimCharacters)
        {
            if (String.IsNullOrEmpty(input))
                return null;

            return input.Trim(String.Join("", trimCharacters).ToCharArray());
        }

      
        public static string ClearString(this string input)
        {
            return input.TrimToNull(" ", "\"", ".");
        }

        internal static string ReplaceUnescaped(this string value, string search, string replacewith)
        {
            var escapedSearch = Regex.Escape(search);
            var regEx = new Regex($"(?<!\\\\){escapedSearch}");
            return regEx.Replace(value, replacewith);
        }

        public static string EnsureEndsWith(this string source, string end)
        {
            if (source.EndsWith(end))
                return source;

            return $"{source}{end}";
        }

    }
}
