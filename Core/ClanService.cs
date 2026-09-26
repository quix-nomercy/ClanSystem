using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Exiled.API.Features;

namespace ClanSystem.Core
{
    public enum ClanAction
    {
        Create,
        Delete,
        Invite,
        Info,
        Accept,
        Decline,
        Leave,
        Kick,
        List,
    }

    public struct ClanOutcome
    {
        public bool Success;

        public string Message;

        public static ClanOutcome Ok(string message)
        {
            return new ClanOutcome { Success = true, Message = message ?? string.Empty };
        }

        public static ClanOutcome Fail(string message)
        {
            return new ClanOutcome { Success = false, Message = message ?? string.Empty };
        }
    }

    public sealed class ClanService
    {
        private readonly Plugin plugin;

        public ClanService(Plugin plugin)
        {
            this.plugin = plugin;
        }

        private MessageSection Messages
        {
            get { return plugin.Config.Messages; }
        }

        private RuleSection Rules
        {
            get { return plugin.Config.Rules; }
        }

        public ClanOutcome Create(Player actor, Player owner, string name)
        {
            Player leader = owner ?? actor;

            if (leader == null)
                return ClanOutcome.Fail(plugin.Parts().Set("usage", plugin.Usage(ClanAction.Create)).Apply(Messages.PlayerRequired));

            if (string.IsNullOrWhiteSpace(name))
                return ClanOutcome.Fail(plugin.Parts().Set("usage", plugin.Usage(ClanAction.Create)).Apply(Messages.NameRequired));

            name = name.Trim();

            if (name.Length < Rules.MinNameLength)
                return ClanOutcome.Fail(plugin.Parts().Set("min", Rules.MinNameLength).Apply(Messages.NameTooShort));

            if (name.Length > Rules.MaxNameLength)
                return ClanOutcome.Fail(plugin.Parts().Set("max", Rules.MaxNameLength).Apply(Messages.NameTooLong));

            if (!MatchesNamePattern(name))
                return ClanOutcome.Fail(Messages.NameInvalid);

            if (IsBlockedName(name))
                return ClanOutcome.Fail(plugin.Parts().Set("clan", name).Apply(Messages.NameBlocked));

            Clan existing;

            if (plugin.Store.TryGetByName(name, out existing))
                return ClanOutcome.Fail(plugin.Parts().Set("clan", existing.Name).Apply(Messages.ClanExists));

            List<Clan> memberships = plugin.Store.GetMemberships(leader.UserId);

            if (memberships.Count >= Rules.MaxClansPerPlayer && memberships.Count > 0)
            {
                string message = memberships.Count == 1 && Rules.MaxClansPerPlayer == 1
                    ? (Same(leader, actor) ? Messages.AlreadyInClan : Messages.OwnerAlreadyInClan)
                    : Messages.ClanLimitReached;

                return ClanOutcome.Fail(plugin.Parts().Set("clan", memberships[0].Name).Set("player", leader.Nickname).Set("limit", Rules.MaxClansPerPlayer).Apply(message));
            }

            Clan clan = new Clan
            {
                Name = name,
                LeaderId = leader.UserId,
                CreatedUtc = DateTime.UtcNow,
            };

            clan.Members.Add(leader.UserId);
            clan.RememberName(leader.UserId, leader.Nickname);

            plugin.Store.Add(clan);
            plugin.Store.Save();
            plugin.Tag.Apply(leader);
            plugin.Menu.Refresh(leader);

            bool selfCreated = Same(leader, actor);

            string response = selfCreated
                ? plugin.Parts().Set("clan", clan.Name).Apply(Messages.ClanCreated)
                : plugin.Parts().Set("clan", clan.Name).Set("player", leader.Nickname).Apply(Messages.ClanCreatedFor);

            if (plugin.Config.Menu.Announcements)
                plugin.Announce(plugin.Parts().Set("clan", clan.Name).Set("player", leader.Nickname).Apply(Messages.ClanCreatedAnnouncement));

            if (!selfCreated)
                Notify(leader, response);

            return ClanOutcome.Ok(response);
        }

        public ClanOutcome Delete(Player actor, string name)
        {
            Clan clan;

            if (string.IsNullOrWhiteSpace(name))
            {
                if (actor == null)
                    return ClanOutcome.Fail(plugin.Parts().Set("usage", plugin.Usage(ClanAction.Delete)).Apply(Messages.NameRequired));

                clan = plugin.Store.GetByMember(actor.UserId);

                if (clan == null)
                    return ClanOutcome.Fail(Messages.SenderNotInClan);
            }
            else if (!plugin.Store.TryGetByName(name, out clan))
            {
                return ClanOutcome.Fail(plugin.Parts().Set("clan", name).Apply(Messages.ClanNotFound));
            }

            if (Rules.OnlyLeaderCanDelete && actor != null && !clan.IsLeader(actor.UserId))
                return ClanOutcome.Fail(plugin.Parts().Set("clan", clan.Name).Apply(Messages.NotLeader));

            List<string> members = clan.Members.ToList();

            plugin.Store.Remove(clan);
            plugin.Store.Save();

            foreach (string memberId in members)
            {
                Player member = PlayerLookup.ByUserId(memberId);

                if (member == null)
                    continue;

                plugin.Tag.Clear(member);
                plugin.Invites.RemoveAll(member.UserId);
                plugin.Menu.Refresh(member);

                if (!Same(member, actor))
                    Notify(member, plugin.Parts().Set("clan", clan.Name).Apply(Messages.ClanDeleted));
            }

            if (plugin.Config.Menu.Announcements && actor != null)
                plugin.Announce(plugin.Parts().Set("clan", clan.Name).Set("player", actor.Nickname).Apply(Messages.ClanDeletedAnnouncement));

            return ClanOutcome.Ok(plugin.Parts().Set("clan", clan.Name).Apply(Messages.ClanDeleted));
        }

        public ClanOutcome Invite(Player actor, Player target, string name)
        {
            if (target == null)
                return ClanOutcome.Fail(plugin.Parts().Set("usage", plugin.Usage(ClanAction.Invite)).Apply(Messages.PlayerRequired));

            if (Same(actor, target))
                return ClanOutcome.Fail(Messages.InviteSelf);

            Clan clan;

            if (string.IsNullOrWhiteSpace(name))
            {
                if (actor == null)
                    return ClanOutcome.Fail(plugin.Parts().Set("usage", plugin.Usage(ClanAction.Invite)).Apply(Messages.NameRequired));

                clan = plugin.Store.GetByMember(actor.UserId);

                if (clan == null)
                    return ClanOutcome.Fail(Messages.SenderNotInClan);
            }
            else if (!plugin.Store.TryGetByName(name, out clan))
            {
                return ClanOutcome.Fail(plugin.Parts().Set("clan", name).Apply(Messages.ClanNotFound));
            }

            if (Rules.OnlyLeaderCanInvite && actor != null && !clan.IsLeader(actor.UserId))
                return ClanOutcome.Fail(plugin.Parts().Set("clan", clan.Name).Apply(Messages.NotLeader));

            if (plugin.Store.GetByMember(target.UserId) != null)
                return ClanOutcome.Fail(plugin.Parts().Set("player", target.Nickname).Apply(Messages.InviteTargetInClan));

            if (clan.Members.Count >= Rules.MaxMembers)
                return ClanOutcome.Fail(plugin.Parts().Set("clan", clan.Name).Set("max", Rules.MaxMembers).Apply(Messages.InviteClanFull));

            if (plugin.Invites.Has(target.UserId, clan.Name))
                return ClanOutcome.Fail(plugin.Parts().Set("player", target.Nickname).Set("clan", clan.Name).Apply(Messages.InvitePending));

            if (plugin.Invites.Count(target.UserId) >= plugin.Config.Invites.MaxPending)
                return ClanOutcome.Fail(plugin.Parts().Set("player", target.Nickname).Set("max", plugin.Config.Invites.MaxPending).Apply(Messages.InviteLimitReached));

            string inviterName = actor != null ? actor.Nickname : "server";

            plugin.Invites.Add(target.UserId, new PendingInvite
            {
                Clan = clan.Name,
                InviterId = actor != null ? actor.UserId : string.Empty,
                InviterName = inviterName,
                ExpiresUtc = DateTime.UtcNow.AddSeconds(plugin.Config.Invites.ExpireSeconds),
            });

            Notify(target, plugin.Parts()
                .Set("player", inviterName)
                .Set("clan", clan.Name)
                .Set("seconds", plugin.Config.Invites.ExpireSeconds)
                .Apply(Messages.InviteReceived));

            plugin.Menu.Refresh(target);

            return ClanOutcome.Ok(plugin.Parts().Set("player", target.Nickname).Set("clan", clan.Name).Apply(Messages.InviteSent));
        }

        public ClanOutcome Accept(Player player, string name)
        {
            PendingInvite invite = FindInvite(player, name, out ClanOutcome failure);

            if (invite == null)
                return failure;

            Clan clan;

            if (!plugin.Store.TryGetByName(invite.Clan, out clan))
            {
                plugin.Invites.Remove(player.UserId, invite.Clan);
                plugin.Menu.Refresh(player);
                return ClanOutcome.Fail(plugin.Parts().Set("clan", invite.Clan).Apply(Messages.ClanNotFound));
            }

            if (Rules.MaxMembers > 0 && clan.Members.Count >= Rules.MaxMembers)
                return ClanOutcome.Fail(plugin.Parts().Set("clan", clan.Name).Set("max", Rules.MaxMembers).Apply(Messages.InviteClanFull));

            if (clan.HasMember(player.UserId))
                return ClanOutcome.Fail(plugin.Parts().Set("clan", clan.Name).Apply(Messages.AlreadyInClan));

            plugin.Invites.Remove(player.UserId, clan.Name);

            clan.Members.Add(player.UserId);
            clan.RememberName(player.UserId, player.Nickname);
            plugin.Store.Save();

            plugin.Tag.Apply(player);

            if (Rules.MaxClansPerPlayer > 0 && plugin.Store.GetMemberships(player.UserId).Count >= Rules.MaxClansPerPlayer)
                plugin.Invites.RemoveAll(player.UserId);

            plugin.Menu.Refresh(player);

            string joined = plugin.Parts().Set("player", player.Nickname).Set("clan", clan.Name).Apply(Messages.MemberJoined);
            NotifyClan(clan, joined, player.UserId);

            Player inviter = PlayerLookup.ByUserId(invite.InviterId);

            if (inviter != null && !Same(inviter, player))
                Notify(inviter, plugin.Parts().Set("player", player.Nickname).Set("clan", clan.Name).Apply(Messages.InviteAcceptedNotice));

            return ClanOutcome.Ok(plugin.Parts().Set("clan", clan.Name).Apply(Messages.InviteAccepted));
        }

        public ClanOutcome Decline(Player player, string name)
        {
            PendingInvite invite = FindInvite(player, name, out ClanOutcome failure);

            if (invite == null)
                return failure;

            plugin.Invites.Remove(player.UserId, invite.Clan);
            plugin.Menu.Refresh(player);

            Player inviter = PlayerLookup.ByUserId(invite.InviterId);

            if (inviter != null && !Same(inviter, player))
                Notify(inviter, plugin.Parts().Set("player", player.Nickname).Set("clan", invite.Clan).Apply(Messages.InviteDeclinedNotice));

            return ClanOutcome.Ok(plugin.Parts().Set("clan", invite.Clan).Apply(Messages.InviteDeclined));
        }

        public ClanOutcome Leave(Player player)
        {
            if (player == null)
                return ClanOutcome.Fail(Messages.PlayerOnly);

            Clan clan = plugin.Store.GetByMember(player.UserId);

            if (clan == null)
                return ClanOutcome.Fail(Messages.SenderNotInClan);

            if (clan.Members.Count <= 1)
            {
                plugin.Store.Remove(clan);
                plugin.Store.Save();
                plugin.Tag.Clear(player);
                plugin.Invites.RemoveAll(player.UserId);
                plugin.Menu.Refresh(player);

                return ClanOutcome.Ok(plugin.Parts().Set("clan", clan.Name).Apply(Messages.ClanDisbanded));
            }

            if (clan.IsLeader(player.UserId))
                return ClanOutcome.Fail(plugin.Parts().Set("clan", clan.Name).Apply(Messages.LeaderCannotLeave));

            clan.Members.Remove(player.UserId);
            clan.Names.Remove(player.UserId);
            plugin.Store.Save();

            plugin.Tag.Clear(player);
            plugin.Menu.Refresh(player);

            NotifyClan(clan, plugin.Parts().Set("player", player.Nickname).Set("clan", clan.Name).Apply(Messages.MemberLeft), player.UserId);

            return ClanOutcome.Ok(plugin.Parts().Set("clan", clan.Name).Apply(Messages.ClanLeft));
        }

        public ClanOutcome Kick(Player actor, Player target, string name)
        {
            if (target == null)
                return ClanOutcome.Fail(plugin.Parts().Set("usage", plugin.Usage(ClanAction.Kick)).Apply(Messages.PlayerRequired));

            Clan clan;

            if (string.IsNullOrWhiteSpace(name))
            {
                if (actor == null)
                    return ClanOutcome.Fail(plugin.Parts().Set("usage", plugin.Usage(ClanAction.Kick)).Apply(Messages.NameRequired));

                clan = plugin.Store.GetByMember(actor.UserId);

                if (clan == null)
                    return ClanOutcome.Fail(Messages.SenderNotInClan);
            }
            else if (!plugin.Store.TryGetByName(name, out clan))
            {
                return ClanOutcome.Fail(plugin.Parts().Set("clan", name).Apply(Messages.ClanNotFound));
            }

            if (Rules.OnlyLeaderCanKick && actor != null && !clan.IsLeader(actor.UserId))
                return ClanOutcome.Fail(plugin.Parts().Set("clan", clan.Name).Apply(Messages.NotLeader));

            if (!clan.HasMember(target.UserId))
                return ClanOutcome.Fail(plugin.Parts().Set("player", target.Nickname).Set("clan", clan.Name).Apply(Messages.KickNotMember));

            if (clan.IsLeader(target.UserId))
                return ClanOutcome.Fail(plugin.Parts().Set("clan", clan.Name).Apply(Messages.KickLeader));

            clan.Members.Remove(target.UserId);
            clan.Names.Remove(target.UserId);
            plugin.Store.Save();

            plugin.Tag.Clear(target);
            plugin.Menu.Refresh(target);
            Notify(target, plugin.Parts().Set("clan", clan.Name).Apply(Messages.KickedFromClan));

            string kicked = plugin.Parts().Set("player", target.Nickname).Set("clan", clan.Name).Apply(Messages.MemberKicked);
            NotifyClan(clan, kicked, target.UserId, actor == null ? null : actor.UserId);

            return ClanOutcome.Ok(kicked);
        }

        public ClanOutcome Info(Player viewer, string name)
        {
            Clan clan;

            if (string.IsNullOrWhiteSpace(name))
            {
                if (viewer == null)
                    return ClanOutcome.Fail(plugin.Parts().Set("usage", plugin.Usage(ClanAction.Info)).Apply(Messages.NameRequired));

                clan = plugin.Store.GetByMember(viewer.UserId);

                if (clan == null)
                    return ClanOutcome.Fail(Messages.SenderNotInClan);
            }
            else if (!plugin.Store.TryGetByName(name, out clan))
            {
                return ClanOutcome.Fail(plugin.Parts().Set("clan", name).Apply(Messages.ClanNotFound));
            }

            List<string> lines = new List<string>();

            foreach (string memberId in clan.Members)
                lines.Add(plugin.Parts().Set("player", Describe(clan, memberId)).Set("role", Role(clan, memberId)).Apply(Messages.MemberLine));

            string info = plugin.Parts()
                .Set("clan", clan.Name)
                .Set("tag", plugin.Tag.BuildTag(clan))
                .Set("leader", Describe(clan, clan.LeaderId))
                .Set("members", string.Join("\n", lines.ToArray()))
                .Set("count", clan.Members.Count)
                .Set("max", Rules.MaxMembers)
                .Set("created", clan.CreatedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm"))
                .Apply(Messages.ClanInfo);

            return ClanOutcome.Ok(info);
        }

        public ClanOutcome List()
        {
            List<Clan> clans = plugin.Store.All().OrderBy(clan => clan.Name, StringComparer.OrdinalIgnoreCase).ToList();

            if (clans.Count == 0)
                return ClanOutcome.Ok(Messages.ClanListEmpty);

            List<string> lines = new List<string>();

            foreach (Clan clan in clans)
            {
                lines.Add(plugin.Parts()
                    .Set("clan", clan.Name)
                    .Set("leader", Describe(clan, clan.LeaderId))
                    .Set("count", clan.Members.Count)
                    .Set("max", Rules.MaxMembers)
                    .Apply(Messages.ClanListLine));
            }

            return ClanOutcome.Ok(plugin.Parts().Set("count", clans.Count).Set("clans", string.Join("\n", lines.ToArray())).Apply(Messages.ClanList));
        }

        public void Notify(Player player, string message)
        {
            if (player == null || string.IsNullOrWhiteSpace(message))
                return;

            if (plugin.Config.Menu.ReplyWithConsole)
                player.SendConsoleMessage(message, "white");

            if (plugin.Config.Menu.ReplyWithHint)
                player.ShowHint(message, plugin.Config.Menu.HintDuration);
        }

        public void NotifyClan(Clan clan, string message, params string[] exceptIds)
        {
            if (clan == null || string.IsNullOrWhiteSpace(message))
                return;

            foreach (Player player in Player.List)
            {
                if (!clan.HasMember(player.UserId))
                    continue;

                if (exceptIds != null && exceptIds.Any(id => string.Equals(id, player.UserId, StringComparison.Ordinal)))
                    continue;

                Notify(player, message);
            }
        }

        public string Describe(Clan clan, string userId)
        {
            Player online = PlayerLookup.ByUserId(userId);

            if (online != null)
                return online.Nickname;

            string stored = clan.StoredName(userId);

            return string.IsNullOrWhiteSpace(stored) ? Messages.UnknownMember : stored;
        }

        public string Role(Clan clan, string userId)
        {
            return clan.IsLeader(userId) ? Messages.RoleLeader : Messages.RoleMember;
        }

        public List<PendingInvite> InvitesOf(Player player)
        {
            return player == null ? new List<PendingInvite>() : plugin.Invites.List(player.UserId);
        }

        private PendingInvite FindInvite(Player player, string name, out ClanOutcome failure)
        {
            failure = ClanOutcome.Fail(Messages.PlayerOnly);

            if (player == null)
                return null;

            List<PendingInvite> invites = plugin.Invites.List(player.UserId);
            PendingInvite match = null;

            foreach (PendingInvite invite in invites)
            {
                if (!string.IsNullOrWhiteSpace(name) && !string.Equals(invite.Clan, name.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;

                match = invite;
                break;
            }

            string wanted = string.IsNullOrWhiteSpace(name)
                ? (invites.Count > 0 ? invites[0].Clan : string.Empty)
                : name.Trim();

            if (match == null)
            {
                failure = ClanOutcome.Fail(plugin.Parts().Set("clan", wanted).Apply(Messages.InviteNotFound));
                return null;
            }

            if (match.IsExpired)
            {
                plugin.Invites.Remove(player.UserId, match.Clan);
                plugin.Menu.Refresh(player);
                failure = ClanOutcome.Fail(plugin.Parts().Set("clan", match.Clan).Apply(Messages.InviteExpired));
                return null;
            }

            return match;
        }

        private bool MatchesNamePattern(string name)
        {
            string pattern = Rules.NamePattern;

            if (string.IsNullOrWhiteSpace(pattern))
                return true;

            try
            {
                return Regex.IsMatch(name, pattern);
            }
            catch (Exception exception)
            {
                if (plugin.Config.Debug)
                    Log.Warn("[ClanSystem] Clan name pattern is invalid: " + exception.Message);

                return true;
            }
        }

        private bool IsBlockedName(string name)
        {
            if (Rules.BlockedNames == null)
                return false;

            return Rules.BlockedNames.Any(blocked => !string.IsNullOrWhiteSpace(blocked) && string.Equals(blocked.Trim(), name, StringComparison.OrdinalIgnoreCase));
        }

        public static bool Same(Player first, Player second)
        {
            if (first == null || second == null)
                return false;

            return string.Equals(first.UserId, second.UserId, StringComparison.Ordinal);
        }
    }
}
