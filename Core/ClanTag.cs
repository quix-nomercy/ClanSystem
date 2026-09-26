using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Exiled.API.Features;

namespace ClanSystem.Core
{
    public sealed class ClanTag
    {
        private static readonly Regex Marker = new Regex("<color=#855439>\\*</color>$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly Plugin plugin;
        private readonly Dictionary<string, string> applied = new Dictionary<string, string>(StringComparer.Ordinal);

        public ClanTag(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public string BuildTag(Clan clan)
        {
            string tag = clan == null || clan.Name == null ? string.Empty : clan.Name;

            return plugin.Config.Tag.Uppercase ? tag.ToUpperInvariant() : tag;
        }

        public string BuildDisplayName(Clan clan, string baseName)
        {
            if (clan == null)
                return baseName;

            string format = string.IsNullOrEmpty(plugin.Config.Tag.Format) ? "[{clan}] {name}" : plugin.Config.Tag.Format;
            string tag = BuildTag(clan);
            string color = plugin.Config.Tag.Color;

            if (!string.IsNullOrWhiteSpace(color))
                tag = "<color=" + color.Trim() + ">" + tag + "</color>";

            string display = format.Replace("{clan}", tag).Replace("{name}", baseName ?? string.Empty);
            int limit = plugin.Config.Tag.DisplayNameLimit > 0 ? plugin.Config.Tag.DisplayNameLimit : 48;

            if (display.Length > limit)
            {
                display = display.Substring(0, limit);
                Log.Warn("[ClanSystem] " + plugin.Parts().Set("clan", clan.Name).Set("limit", limit).Apply(plugin.Config.Messages.TagTooLong));
            }

            return display;
        }

        public void Apply(Player player)
        {
            if (player == null || player.IsNPC)
                return;

            Clan clan = plugin.Store.GetByMember(player.UserId);

            if (clan == null || !plugin.Config.Tag.Show)
            {
                Clear(player);
                return;
            }

            clan.RememberName(player.UserId, player.Nickname);
            string display = BuildDisplayName(clan, GetBaseName(player, clan));

            if (!string.IsNullOrEmpty(player.UserId))
                applied[player.UserId] = display;

            if (!string.Equals(RawName(player), display, StringComparison.Ordinal))
                player.DisplayNickname = display;
        }

        public void Forget(string userId)
        {
            if (!string.IsNullOrEmpty(userId))
                applied.Remove(userId);
        }

        public void Clear(Player player)
        {
            if (player == null || player.IsNPC)
                return;

            string userId = player.UserId;

            if (string.IsNullOrEmpty(userId) || !applied.TryGetValue(userId, out string display))
                return;

            applied.Remove(userId);

            if (player.HasCustomName && string.Equals(RawName(player), display, StringComparison.Ordinal))
                player.DisplayNickname = null;
        }

        public string GetBaseName(Player player, Clan clan)
        {
            string plain = StripTag(clan, player.DisplayNickname);

            return string.IsNullOrWhiteSpace(plain) ? player.Nickname : plain;
        }

        private static string RawName(Player player)
        {
            string current = player.DisplayNickname;

            return string.IsNullOrEmpty(current) ? string.Empty : Marker.Replace(current, string.Empty).Trim();
        }

        public string StripTag(Clan clan, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            string cleaned = Marker.Replace(text, string.Empty).Trim();
            string plain;

            if (TryStripTag(clan, cleaned, out plain) && !string.IsNullOrWhiteSpace(plain))
                return plain;

            return cleaned;
        }

        private bool TryStripTag(Clan clan, string text, out string plain)
        {
            plain = text;
            string pattern = BuildStripPattern(clan);

            if (string.IsNullOrEmpty(pattern))
                return false;

            try
            {
                Match match = Regex.Match(text, pattern);

                if (match.Success)
                {
                    plain = match.Groups["name"].Value.Trim();
                    return true;
                }
            }
            catch (Exception exception)
            {
                if (plugin.Config.Debug)
                    Log.Warn("[ClanSystem] Tag strip failed: " + exception.Message);
            }

            return false;
        }

        private string BuildStripPattern(Clan clan)
        {
            string format = plugin.Config.Tag.Format;

            if (string.IsNullOrEmpty(format) || !format.Contains("{name}"))
                return null;

            string pattern = Regex.Escape(format);
            string clanPart = "(?:<color=[^>]*>)?" + Regex.Escape(BuildTag(clan)) + "(?:</color>)?";

            pattern = pattern.Replace(Regex.Escape("{clan}"), clanPart);
            pattern = pattern.Replace(Regex.Escape("{name}"), "(?<name>.+)");

            return "^" + pattern + "$";
        }
    }
}
