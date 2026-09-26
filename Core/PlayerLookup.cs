using System;
using CommandSystem;
using Exiled.API.Features;
using RemoteAdmin;

namespace ClanSystem.Core
{
    internal static class PlayerLookup
    {
        internal static Player FromSender(ICommandSender sender)
        {
            CommandSender commandSender = sender as CommandSender;

            return commandSender == null ? null : ByUserId(commandSender.SenderId);
        }

        internal static Player ByUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return null;

            foreach (Player player in Player.List)
            {
                if (player != null && string.Equals(player.UserId, userId, StringComparison.OrdinalIgnoreCase))
                    return player;
            }

            return null;
        }

        internal static Player ByHub(ReferenceHub hub)
        {
            if (hub == null)
                return null;

            foreach (Player player in Player.List)
            {
                if (player != null && ReferenceEquals(player.ReferenceHub, hub))
                    return player;
            }

            return null;
        }

        internal static Player ById(int id)
        {
            foreach (Player player in Player.List)
            {
                if (player != null && player.Id == id)
                    return player;
            }

            return null;
        }

        internal static Player ByQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return null;

            string trimmed = query.Trim();
            Player found = ByUserId(trimmed);

            if (found != null)
                return found;

            int id;

            if (int.TryParse(trimmed, out id))
                return ById(id);

            Player best = null;
            int bestDifference = int.MaxValue;

            foreach (Player player in Player.List)
            {
                if (player == null || string.IsNullOrEmpty(player.Nickname))
                    continue;

                string nickname = player.Nickname;

                if (nickname.IndexOf(trimmed, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                int difference = nickname.Length - trimmed.Length;

                if (difference < bestDifference)
                {
                    bestDifference = difference;
                    best = player;
                }
            }

            return best;
        }
    }
}
