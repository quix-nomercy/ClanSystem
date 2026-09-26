using System.Collections.Generic;
using System.ComponentModel;
using Exiled.API.Interfaces;
using RemoteAdmin;

namespace ClanSystem
{
    public sealed class Config : IConfig
    {
        [Description("Whether the plugin is enabled.")]
        public bool IsEnabled { get; set; } = true;

        [Description("Print extra lines in the server console for debugging.")]
        public bool Debug { get; set; } = false;

        [Description("Clan data file, relative to the EXILED configs folder.")]
        public string DataFile { get; set; } = "ClanSystem/clans.json";

        [Description("Command settings.")]
        public CommandSection Command { get; set; } = new CommandSection();

        [Description("Rules applied to clan names and membership.")]
        public RuleSection Rules { get; set; } = new RuleSection();

        [Description("Clan tag shown in front of the player name in the player list.")]
        public TagSection Tag { get; set; } = new TagSection();

        [Description("Invite settings.")]
        public InviteSection Invites { get; set; } = new InviteSection();

        [Description("Server-Specific Settings menu.")]
        public MenuSection Menu { get; set; } = new MenuSection();

        [Description("Every message the plugin can send.")]
        public MessageSection Messages { get; set; } = new MessageSection();
    }

    public sealed class CommandSection
    {
        [Description("Root command name, shared by the server console, Remote Admin and the player console. Players type a dot in front, for example .clan.")]
        public string Name { get; set; } = "clan";

        [Description("Root command aliases.")]
        public List<string> Aliases { get; set; } = new List<string> { "clans" };

        [Description("Names of the create sub-command. The first entry is the main name, the others are aliases.")]
        public List<string> Create { get; set; } = new List<string> { "create", "new" };

        [Description("Names of the delete sub-command.")]
        public List<string> Delete { get; set; } = new List<string> { "delete", "disband" };

        [Description("Names of the invite sub-command.")]
        public List<string> Invite { get; set; } = new List<string> { "invite", "inv" };

        [Description("Names of the info sub-command.")]
        public List<string> Info { get; set; } = new List<string> { "info", "show" };

        [Description("Names of the accept sub-command.")]
        public List<string> Accept { get; set; } = new List<string> { "accept", "join" };

        [Description("Names of the decline sub-command.")]
        public List<string> Decline { get; set; } = new List<string> { "decline", "refuse" };

        [Description("Names of the leave sub-command.")]
        public List<string> Leave { get; set; } = new List<string> { "leave", "quit" };

        [Description("Names of the kick sub-command.")]
        public List<string> Kick { get; set; } = new List<string> { "kick", "remove" };

        [Description("Names of the list sub-command.")]
        public List<string> ListNames { get; set; } = new List<string> { "list", "all" };

        [Description("Require a Remote Admin permission before a player can use the clan commands. The server console is always allowed.")]
        public bool RequirePermission { get; set; } = false;

        [Description("Remote Admin permission flag checked when require_permission is true. Names match the flags used in config_gameplay.txt.")]
        public PlayerPermissions PermissionFlag { get; set; } = PlayerPermissions.PermissionsManagement;
    }

    public sealed class RuleSection
    {
        [Description("Minimum clan name length.")]
        public int MinNameLength { get; set; } = 3;

        [Description("Maximum clan name length. Keep it short, the tag is added in front of the player name, which the game limits to 48 characters.")]
        public int MaxNameLength { get; set; } = 12;

        [Description("Regular expression a clan name has to match.")]
        public string NamePattern { get; set; } = "^[A-Za-z0-9_]+$";

        [Description("Clan names that are not allowed, case insensitive.")]
        public List<string> BlockedNames { get; set; } = new List<string>();

        [Description("Maximum members per clan.")]
        public int MaxMembers { get; set; } = 12;

        [Description("Maximum clans a player can be in at the same time.")]
        public int MaxClansPerPlayer { get; set; } = 1;

        [Description("Only the clan leader can delete the clan.")]
        public bool OnlyLeaderCanDelete { get; set; } = false;

        [Description("Only the clan leader can invite players.")]
        public bool OnlyLeaderCanInvite { get; set; } = false;

        [Description("Only the clan leader can kick members.")]
        public bool OnlyLeaderCanKick { get; set; } = false;
    }

    public sealed class TagSection
    {
        [Description("Show the clan tag in front of the player name in the player list.")]
        public bool Show { get; set; } = true;

        [Description("Tag format. {clan} is replaced by the clan name, {name} by the player name.")]
        public string Format { get; set; } = "[{clan}] {name}";

        [Description("Upper case the clan name inside the tag.")]
        public bool Uppercase { get; set; } = true;

        [Description("Optional color of the clan part, for example #00BFFF or white. Leave empty for no color.")]
        public string Color { get; set; } = "";

        [Description("Limit the game applies to the displayed name. Longer names are cut to fit.")]
        public int DisplayNameLimit { get; set; } = 48;
    }

    public sealed class InviteSection
    {
        [Description("Seconds an invite stays valid.")]
        public int ExpireSeconds { get; set; } = 90;

        [Description("How many invites a player can hold at the same time.")]
        public int MaxPending { get; set; } = 5;

        [Description("Only online players can be invited.")]
        public bool OnlineOnly { get; set; } = true;
    }

    public sealed class MenuSection
    {
        [Description("Register the clan menu in the Server-Specific Settings tab.")]
        public bool Enabled { get; set; } = true;

        [Description("First setting id used by the menu. The plugin uses this id and the eleven following ids.")]
        public int BaseSettingId { get; set; } = 741200;

        [Description("Group header text.")]
        public string Header { get; set; } = "CLAN";

        [Description("Group header hint.")]
        public string HeaderHint { get; set; } = "";

        [Description("Label of the clan name field.")]
        public string NameLabel { get; set; } = "Clan name";

        [Description("Placeholder of the clan name field.")]
        public string NamePlaceholder { get; set; } = "e.g. RAIL";

        [Description("Hint of the clan name field.")]
        public string NameHint { get; set; } = "Used by create, delete, invite, info, accept and decline.";

        [Description("Character limit of the clan name field.")]
        public int NameLimit { get; set; } = 32;

        [Description("Label of the player picker.")]
        public string TargetLabel { get; set; } = "Player";

        [Description("Hint of the player picker.")]
        public string TargetHint { get; set; } = "Target of invite and kick.";

        [Description("First entry of the player picker, shown as long as nothing is selected.")]
        public string TargetPlaceholder { get; set; } = "Select a player";

        [Description("Replaces the first entry of the player picker when nobody else is online.")]
        public string NoTargetsOption { get; set; } = "Nobody online";

        [Description("Text of the status box before the first update.")]
        public string StatusPlaceholder { get; set; } = "Clan menu ready.";

        [Description("Text of the create button.")]
        public string CreateButton { get; set; } = "Create clan";

        [Description("Hint of the create button.")]
        public string CreateButtonHint { get; set; } = "Creates a clan with the name from the field above.";

        [Description("Text of the delete button.")]
        public string DeleteButton { get; set; } = "Delete clan";

        [Description("Hint of the delete button.")]
        public string DeleteButtonHint { get; set; } = "Deletes the clan from the field above.";

        [Description("Text of the invite button.")]
        public string InviteButton { get; set; } = "Invite player";

        [Description("Hint of the invite button.")]
        public string InviteButtonHint { get; set; } = "Invites the selected player to your clan.";

        [Description("Text of the info button.")]
        public string InfoButton { get; set; } = "Clan info";

        [Description("Hint of the info button.")]
        public string InfoButtonHint { get; set; } = "Shows the clan from the field above, or your own clan.";

        [Description("Text of the accept button.")]
        public string AcceptButton { get; set; } = "Accept invite";

        [Description("Hint of the accept button.")]
        public string AcceptButtonHint { get; set; } = "Accepts the oldest pending invite.";

        [Description("Text of the decline button.")]
        public string DeclineButton { get; set; } = "Decline invite";

        [Description("Hint of the decline button.")]
        public string DeclineButtonHint { get; set; } = "Declines the oldest pending invite.";

        [Description("Text of the leave button.")]
        public string LeaveButton { get; set; } = "Leave clan";

        [Description("Hint of the leave button.")]
        public string LeaveButtonHint { get; set; } = "Leaves your current clan.";

        [Description("Text of the kick button.")]
        public string KickButton { get; set; } = "Kick member";

        [Description("Hint of the kick button.")]
        public string KickButtonHint { get; set; } = "Removes the selected player from your clan.";

        [Description("Also answer menu actions with a hint on screen.")]
        public bool ReplyWithHint { get; set; } = true;

        [Description("Also answer menu actions in the player console.")]
        public bool ReplyWithConsole { get; set; } = true;

        [Description("Hint duration in seconds.")]
        public float HintDuration { get; set; } = 6f;

        [Description("Send clan announcements as a broadcast instead of only telling the players involved.")]
        public bool Announcements { get; set; } = false;

        [Description("Broadcast duration in seconds.")]
        public ushort AnnouncementDuration { get; set; } = 6;
    }

    public sealed class MessageSection
    {
        [Description("Sender has no permission for the command. Placeholders: none.")]
        public string NoPermission { get; set; } = "You do not have permission to use clan commands.";

        [Description("Unknown sub-command. Placeholders: {action}, {usage}.")]
        public string UnknownAction { get; set; } = "Unknown clan action '{action}'. Use {usage}";

        [Description("Command overview shown by the root command. Placeholders: {command}, {create}, {delete}, {invite}, {info}, {accept}, {decline}, {leave}, {kick}, {list}.")]
        public string Usage { get; set; } = "Clan commands: {command} {create} <name>, {command} {delete} [name], {command} {invite} <player> [clan], {command} {info} [name], {command} {accept} [clan], {command} {decline} [clan], {command} {leave}, {command} {kick} <player> [clan], {command} {list}";

        [Description("Action needs a player sender. Placeholders: none.")]
        public string PlayerOnly { get; set; } = "This action has to be used by a player in game.";

        [Description("Player lookup failed. Placeholders: {query}.")]
        public string PlayerNotFound { get; set; } = "No player found for '{query}'.";

        [Description("Sender is not in a clan. Placeholders: none.")]
        public string SenderNotInClan { get; set; } = "You are not in a clan.";

        [Description("Clan lookup failed. Placeholders: {clan}.")]
        public string ClanNotFound { get; set; } = "The clan '{clan}' does not exist.";

        [Description("Clan name argument missing. Placeholders: {usage}.")]
        public string NameRequired { get; set; } = "Add a clan name. Usage: {usage}";

        [Description("Player argument missing. Placeholders: {usage}.")]
        public string PlayerRequired { get; set; } = "Add a player name or id. Usage: {usage}";

        [Description("Usage of the create sub-command. Placeholders: {command}, {create}.")]
        public string CreateUsage { get; set; } = "{command} {create} <name>";

        [Description("Clan name is shorter than the configured minimum. Placeholders: {min}.")]
        public string NameTooShort { get; set; } = "A clan name needs at least {min} characters.";

        [Description("Clan name is longer than the configured maximum. Placeholders: {max}.")]
        public string NameTooLong { get; set; } = "A clan name can have at most {max} characters.";

        [Description("Clan name does not match the configured pattern. Placeholders: none.")]
        public string NameInvalid { get; set; } = "That clan name contains characters that are not allowed.";

        [Description("Clan name is on the blocked list. Placeholders: {clan}.")]
        public string NameBlocked { get; set; } = "The name '{clan}' is not allowed.";

        [Description("A clan with that name already exists. Placeholders: {clan}.")]
        public string ClanExists { get; set; } = "A clan named '{clan}' already exists.";

        [Description("Sender already is in a clan. Placeholders: {clan}.")]
        public string AlreadyInClan { get; set; } = "You are already in the clan '{clan}'.";

        [Description("Sender reached the configured clan limit. Placeholders: {limit}.")]
        public string ClanLimitReached { get; set; } = "You can be in {limit} clan(s) at the same time.";

        [Description("Console create target already is in a clan. Placeholders: {player}, {clan}.")]
        public string OwnerAlreadyInClan { get; set; } = "{player} is already in the clan '{clan}'.";

        [Description("Clan created by the sender. Placeholders: {clan}.")]
        public string ClanCreated { get; set; } = "Clan '{clan}' created, you are the leader.";

        [Description("Clan created through the console. Placeholders: {clan}, {player}.")]
        public string ClanCreatedFor { get; set; } = "Clan '{clan}' created, {player} is the leader.";

        [Description("Announcement sent to the clan members when a player joins. Placeholders: {player}, {clan}.")]
        public string MemberJoined { get; set; } = "{player} joined the clan {clan}.";

        [Description("Server wide announcement when a clan is created. Placeholders: {player}, {clan}.")]
        public string ClanCreatedAnnouncement { get; set; } = "The clan {clan} was created by {player}.";

        [Description("Usage of the delete sub-command. Placeholders: {command}, {delete}.")]
        public string DeleteUsage { get; set; } = "{command} {delete} [name]";

        [Description("Clan deleted by the sender. Placeholders: {clan}.")]
        public string ClanDeleted { get; set; } = "Clan '{clan}' deleted.";

        [Description("Server wide announcement when a clan is deleted. Placeholders: {player}, {clan}.")]
        public string ClanDeletedAnnouncement { get; set; } = "The clan {clan} was deleted by {player}.";

        [Description("Action reserved to the clan leader. Placeholders: {clan}.")]
        public string NotLeader { get; set; } = "Only the leader of '{clan}' can do that.";

        [Description("Usage of the invite sub-command. Placeholders: {command}, {invite}.")]
        public string InviteUsage { get; set; } = "{command} {invite} <player> [clan]";

        [Description("Invite target is the sender. Placeholders: none.")]
        public string InviteSelf { get; set; } = "You cannot invite yourself.";

        [Description("Invite sent by the sender. Placeholders: {player}, {clan}.")]
        public string InviteSent { get; set; } = "Invite sent to {player} for the clan '{clan}'.";

        [Description("Invite message sent to the target. Placeholders: {player}, {clan}, {command}, {accept}, {decline}, {seconds}.")]
        public string InviteReceived { get; set; } = "{player} invited you to the clan '{clan}'. Use {command} {accept} {clan} to join or {command} {decline} {clan} to refuse. The invite expires in {seconds} seconds.";

        [Description("Invite already pending for that clan. Placeholders: {player}, {clan}.")]
        public string InvitePending { get; set; } = "{player} already has an invite for '{clan}'.";

        [Description("Invite target is in a clan already. Placeholders: {player}.")]
        public string InviteTargetInClan { get; set; } = "{player} is already in a clan.";

        [Description("Clan reached the member limit. Placeholders: {clan}, {max}.")]
        public string InviteClanFull { get; set; } = "The clan '{clan}' already has {max} members.";

        [Description("Invite target holds too many invites. Placeholders: {player}, {max}.")]
        public string InviteLimitReached { get; set; } = "{player} already holds {max} invites.";

        [Description("Invite expired before it was answered. Placeholders: {clan}.")]
        public string InviteExpired { get; set; } = "The invite for '{clan}' expired.";

        [Description("No pending invite found. Placeholders: {clan}.")]
        public string InviteNotFound { get; set; } = "You have no pending invite for '{clan}'.";

        [Description("Usage of the accept sub-command. Placeholders: {command}, {accept}.")]
        public string AcceptUsage { get; set; } = "{command} {accept} [clan]";

        [Description("Usage of the decline sub-command. Placeholders: {command}, {decline}.")]
        public string DeclineUsage { get; set; } = "{command} {decline} [clan]";

        [Description("Invite accepted by the target. Placeholders: {clan}.")]
        public string InviteAccepted { get; set; } = "You joined the clan '{clan}'.";

        [Description("Notice sent to the inviter when the invite was accepted. Placeholders: {player}, {clan}.")]
        public string InviteAcceptedNotice { get; set; } = "{player} accepted your invite for the clan '{clan}'.";

        [Description("Invite declined by the target. Placeholders: {clan}.")]
        public string InviteDeclined { get; set; } = "You declined the invite for '{clan}'.";

        [Description("Notice sent to the inviter when the invite was declined. Placeholders: {player}, {clan}.")]
        public string InviteDeclinedNotice { get; set; } = "{player} declined your invite for the clan '{clan}'.";

        [Description("Usage of the leave sub-command. Placeholders: {command}, {leave}.")]
        public string LeaveUsage { get; set; } = "{command} {leave}";

        [Description("Member left the clan. Placeholders: {clan}.")]
        public string ClanLeft { get; set; } = "You left the clan '{clan}'.";

        [Description("Clan leader tried to leave. Placeholders: {clan}, {command}, {delete}.")]
        public string LeaderCannotLeave { get; set; } = "You are the leader of '{clan}'. Use {command} {delete} {clan} if you want to remove the clan.";

        [Description("Last member left, the clan was removed. Placeholders: {clan}.")]
        public string ClanDisbanded { get; set; } = "You were the last member, the clan '{clan}' was deleted.";

        [Description("Notice sent to the clan when a member leaves. Placeholders: {player}, {clan}.")]
        public string MemberLeft { get; set; } = "{player} left the clan {clan}.";

        [Description("Usage of the kick sub-command. Placeholders: {command}, {kick}.")]
        public string KickUsage { get; set; } = "{command} {kick} <player> [clan]";

        [Description("Kick target is not a member. Placeholders: {player}, {clan}.")]
        public string KickNotMember { get; set; } = "{player} is not a member of '{clan}'.";

        [Description("The clan leader cannot be kicked. Placeholders: {clan}.")]
        public string KickLeader { get; set; } = "The leader of '{clan}' cannot be kicked.";

        [Description("Member was kicked. Placeholders: {player}, {clan}.")]
        public string MemberKicked { get; set; } = "{player} was removed from the clan '{clan}'.";

        [Description("Message sent to the kicked player. Placeholders: {clan}.")]
        public string KickedFromClan { get; set; } = "You were removed from the clan '{clan}'.";

        [Description("Usage of the info sub-command. Placeholders: {command}, {info}.")]
        public string InfoUsage { get; set; } = "{command} {info} [name]";

        [Description("Clan info block. Placeholders: {clan}, {tag}, {leader}, {members}, {count}, {max}, {created}.")]
        public string ClanInfo { get; set; } = "Clan: {clan}\nTag: {tag}\nLeader: {leader}\nMembers ({count}/{max}):\n{members}\nCreated: {created}";

        [Description("One line of the member list. Placeholders: {player}, {role}.")]
        public string MemberLine { get; set; } = " - {player} ({role})";

        [Description("Role text of the clan leader. Placeholders: none.")]
        public string RoleLeader { get; set; } = "leader";

        [Description("Role text of a clan member. Placeholders: none.")]
        public string RoleMember { get; set; } = "member";

        [Description("Name shown for members that were not seen yet. Placeholders: none.")]
        public string UnknownMember { get; set; } = "unknown";

        [Description("Usage of the list sub-command. Placeholders: {command}, {list}.")]
        public string ListUsage { get; set; } = "{command} {list}";

        [Description("Clan list block. Placeholders: {count}, {clans}.")]
        public string ClanList { get; set; } = "Clans ({count}):\n{clans}";

        [Description("One line of the clan list. Placeholders: {clan}, {leader}, {count}, {max}.")]
        public string ClanListLine { get; set; } = " - {clan} | leader {leader} | {count}/{max}";

        [Description("Shown when no clan exists yet. Placeholders: none.")]
        public string ClanListEmpty { get; set; } = "No clans yet.";

        [Description("Status box of a member. Placeholders: {clan}, {role}, {count}, {max}.")]
        public string StatusMember { get; set; } = "Clan: {clan} | {role} | {count}/{max} members";

        [Description("Status box of a player without a clan. Placeholders: none.")]
        public string StatusNoClan { get; set; } = "You are not in a clan.";

        [Description("Status box invite line. Placeholders: {invites}.")]
        public string StatusInvites { get; set; } = "Invites: {invites}";

        [Description("One invite inside the status box. Placeholders: {clan}, {player}, {seconds}.")]
        public string StatusInviteLine { get; set; } = "{clan} from {player} ({seconds}s)";

        [Description("Status box entry for a player who already has all invites. Placeholders: none.")]
        public string StatusInvitesFull { get; set; } = "too many invites";

        [Description("Clan name is too long to fit in front of the player name. Placeholders: {clan}, {limit}.")]
        public string TagTooLong { get; set; } = "The tag of '{clan}' does not fit in front of the player name and was cut to {limit} characters.";

        [Description("Data file could not be read or written. Placeholders: {error}.")]
        public string StorageError { get; set; } = "Clan data could not be saved: {error}";
    }
}
