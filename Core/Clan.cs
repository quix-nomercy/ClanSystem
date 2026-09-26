using System;
using System.Collections.Generic;

namespace ClanSystem.Core
{
    public sealed class Clan
    {
        public string Name { get; set; }

        public string LeaderId { get; set; }

        public List<string> Members { get; set; } = new List<string>();

        public Dictionary<string, string> Names { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public bool HasMember(string userId)
        {
            return !string.IsNullOrEmpty(userId) && Members != null && Members.Contains(userId);
        }

        public bool IsLeader(string userId)
        {
            return !string.IsNullOrEmpty(userId) && string.Equals(LeaderId, userId, StringComparison.Ordinal);
        }

        public string StoredName(string userId)
        {
            if (Names != null && userId != null && Names.TryGetValue(userId, out string name) && !string.IsNullOrWhiteSpace(name))
                return name;

            return null;
        }

        public void RememberName(string userId, string nickname)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrWhiteSpace(nickname))
                return;

            if (Names == null)
                Names = new Dictionary<string, string>(StringComparer.Ordinal);

            Names[userId] = nickname;
        }
    }
}
