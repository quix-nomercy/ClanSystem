using System;
using System.Collections.Generic;
using System.Text;

namespace ClanSystem.Core
{
    public sealed class MessageParts
    {
        private readonly Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public MessageParts Set(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                return this;

            values[key] = value ?? string.Empty;
            return this;
        }

        public MessageParts Set(string key, int value)
        {
            return Set(key, value.ToString());
        }

        public string Apply(string template)
        {
            if (string.IsNullOrEmpty(template) || values.Count == 0)
                return template;

            StringBuilder builder = new StringBuilder(template);

            foreach (KeyValuePair<string, string> pair in values)
                builder.Replace("{" + pair.Key + "}", pair.Value);

            return builder.ToString();
        }
    }
}
