using System;
using System.Collections.Generic;

namespace ClanSystem.Core
{
    public static class Names
    {
        public static string Primary(List<string> values, string fallback)
        {
            if (values == null || values.Count == 0)
                return fallback;

            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }

            return fallback;
        }

        public static string[] All(List<string> values, string fallback)
        {
            List<string> result = new List<string>();

            if (values != null)
            {
                foreach (string value in values)
                {
                    if (!string.IsNullOrWhiteSpace(value))
                        result.Add(value.Trim());
                }
            }

            if (result.Count == 0)
                result.Add(fallback);

            return result.ToArray();
        }

        public static bool Matches(List<string> values, string token)
        {
            if (values == null || string.IsNullOrWhiteSpace(token))
                return false;

            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value) && string.Equals(value.Trim(), token.Trim(), StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
