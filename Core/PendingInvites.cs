using System;
using System.Collections.Generic;
using System.Linq;

namespace ClanSystem.Core
{
    public sealed class PendingInvite
    {
        public string Clan { get; set; }

        public string InviterId { get; set; }

        public string InviterName { get; set; }

        public DateTime ExpiresUtc { get; set; }

        public bool IsExpired
        {
            get { return DateTime.UtcNow >= ExpiresUtc; }
        }

        public int SecondsLeft
        {
            get
            {
                double seconds = (ExpiresUtc - DateTime.UtcNow).TotalSeconds;
                return seconds <= 0 ? 0 : (int)seconds;
            }
        }
    }

    public sealed class PendingInvites
    {
        private readonly object sync = new object();
        private readonly Dictionary<string, List<PendingInvite>> byTarget = new Dictionary<string, List<PendingInvite>>(StringComparer.Ordinal);

        public List<PendingInvite> List(string targetId)
        {
            lock (sync)
            {
                if (string.IsNullOrEmpty(targetId) || !byTarget.TryGetValue(targetId, out List<PendingInvite> invites))
                    return new List<PendingInvite>();

                return invites.ToList();
            }
        }

        public int Count(string targetId)
        {
            lock (sync)
            {
                if (string.IsNullOrEmpty(targetId) || !byTarget.TryGetValue(targetId, out List<PendingInvite> invites))
                    return 0;

                return invites.Count;
            }
        }

        public bool Has(string targetId, string clanName)
        {
            lock (sync)
            {
                if (string.IsNullOrEmpty(targetId) || string.IsNullOrWhiteSpace(clanName) || !byTarget.TryGetValue(targetId, out List<PendingInvite> invites))
                    return false;

                return invites.Any(invite => string.Equals(invite.Clan, clanName.Trim(), StringComparison.OrdinalIgnoreCase));
            }
        }

        public bool Add(string targetId, PendingInvite invite)
        {
            if (string.IsNullOrEmpty(targetId) || invite == null || string.IsNullOrWhiteSpace(invite.Clan))
                return false;

            lock (sync)
            {
                if (!byTarget.TryGetValue(targetId, out List<PendingInvite> invites))
                {
                    invites = new List<PendingInvite>();
                    byTarget[targetId] = invites;
                }

                invites.Add(invite);
                return true;
            }
        }

        public void Remove(string targetId, string clanName)
        {
            if (string.IsNullOrEmpty(targetId))
                return;

            lock (sync)
            {
                if (!byTarget.TryGetValue(targetId, out List<PendingInvite> invites))
                    return;

                invites.RemoveAll(invite => string.Equals(invite.Clan, clanName, StringComparison.OrdinalIgnoreCase));

                if (invites.Count == 0)
                    byTarget.Remove(targetId);
            }
        }

        public void RemoveAll(string targetId)
        {
            if (string.IsNullOrEmpty(targetId))
                return;

            lock (sync)
                byTarget.Remove(targetId);
        }

        public int Prune()
        {
            int removed = 0;

            lock (sync)
            {
                List<string> empty = new List<string>();

                foreach (KeyValuePair<string, List<PendingInvite>> pair in byTarget)
                {
                    removed += pair.Value.RemoveAll(invite => invite.IsExpired);

                    if (pair.Value.Count == 0)
                        empty.Add(pair.Key);
                }

                foreach (string targetId in empty)
                    byTarget.Remove(targetId);
            }

            return removed;
        }

        public void Clear()
        {
            lock (sync)
                byTarget.Clear();
        }
    }
}
