using System;
using System.Collections.Generic;
using ClanSystem.Core;
using Exiled.API.Features;
using UserSettings.ServerSpecific;

namespace ClanSystem.Menu
{
    public sealed class ClanMenu
    {
        private const int DebounceMilliseconds = 400;

        private readonly Plugin plugin;
        private readonly Dictionary<string, List<int>> targetIds = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        private readonly HashSet<string> pendingRefresh = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, DateTime> lastPress = new Dictionary<string, DateTime>(StringComparer.Ordinal);
        private ServerSpecificSettingBase[] registered = new ServerSpecificSettingBase[0];
        private SSTextArea statusSetting;

        public ClanMenu(Plugin plugin)
        {
            this.plugin = plugin;
        }

        private MenuSection Settings
        {
            get { return plugin.Config.Menu; }
        }

        private int HeaderId
        {
            get { return Settings.BaseSettingId; }
        }

        private int NameId
        {
            get { return Settings.BaseSettingId + 1; }
        }

        private int TargetId
        {
            get { return Settings.BaseSettingId + 2; }
        }

        private int StatusId
        {
            get { return Settings.BaseSettingId + 3; }
        }

        private int CreateId
        {
            get { return Settings.BaseSettingId + 4; }
        }

        private int DeleteId
        {
            get { return Settings.BaseSettingId + 5; }
        }

        private int InviteId
        {
            get { return Settings.BaseSettingId + 6; }
        }

        private int InfoId
        {
            get { return Settings.BaseSettingId + 7; }
        }

        private int AcceptId
        {
            get { return Settings.BaseSettingId + 8; }
        }

        private int DeclineId
        {
            get { return Settings.BaseSettingId + 9; }
        }

        private int LeaveId
        {
            get { return Settings.BaseSettingId + 10; }
        }

        private int KickId
        {
            get { return Settings.BaseSettingId + 11; }
        }

        public void Enable()
        {
            if (!Settings.Enabled)
                return;

            registered = BuildShared();

            ServerSpecificSettingBase[] current = ServerSpecificSettingsSync.DefinedSettings ?? new ServerSpecificSettingBase[0];
            ServerSpecificSettingsSync.DefinedSettings = Join(registered, current);

            ServerSpecificSettingsSync.ServerOnSettingValueReceived += OnSettingReceived;
            ServerSpecificSettingsSync.ServerOnStatusReceived += OnStatusReceived;
            ServerSpecificSettingsSync.SendToAll();
        }

        public void Disable()
        {
            ServerSpecificSettingsSync.ServerOnSettingValueReceived -= OnSettingReceived;
            ServerSpecificSettingsSync.ServerOnStatusReceived -= OnStatusReceived;

            targetIds.Clear();
            pendingRefresh.Clear();
            lastPress.Clear();
            statusSetting = null;

            if (registered.Length == 0)
                return;

            List<ServerSpecificSettingBase> keep = new List<ServerSpecificSettingBase>();
            ServerSpecificSettingBase[] current = ServerSpecificSettingsSync.DefinedSettings ?? new ServerSpecificSettingBase[0];

            foreach (ServerSpecificSettingBase setting in current)
            {
                if (setting != null && !IsOurs(setting))
                    keep.Add(setting);
            }

            ServerSpecificSettingsSync.DefinedSettings = keep.ToArray();
            registered = new ServerSpecificSettingBase[0];
            ServerSpecificSettingsSync.SendToAll();
        }

        public void RefreshAll()
        {
            if (!Settings.Enabled)
                return;

            foreach (Player player in Player.List)
                Refresh(player);
        }

        public void Refresh(Player player)
        {
            if (!Settings.Enabled || player == null || !player.IsConnected)
                return;

            PushStatus(player, null);

            if (IsTabOpen(player))
            {
                string userId = player.UserId;

                if (!string.IsNullOrEmpty(userId))
                    pendingRefresh.Add(userId);

                return;
            }

            Send(player);
        }

        public void Send(Player player)
        {
            if (!Settings.Enabled || player == null || player.ReferenceHub == null || !player.IsConnected)
                return;

            string userId = player.UserId;

            if (string.IsNullOrEmpty(userId))
                return;

            ServerSpecificSettingBase[] current = ServerSpecificSettingsSync.DefinedSettings;

            if (current == null || current.Length == 0)
                return;

            List<int> ids;
            List<ServerSpecificSettingBase> pack = new List<ServerSpecificSettingBase>();

            foreach (ServerSpecificSettingBase setting in current)
            {
                if (setting == null)
                    continue;

                if (setting.SettingId == TargetId && setting is SSDropdownSetting)
                {
                    pack.Add(CreateTargetDropdown(player, out ids));
                    targetIds[userId] = ids;
                    continue;
                }

                if (setting.SettingId == StatusId && setting is SSTextArea)
                {
                    pack.Add(CreateStatus(player));
                    continue;
                }

                pack.Add(setting);
            }

            try
            {
                ServerSpecificSettingsSync.SendToPlayer(player.ReferenceHub, pack.ToArray());
            }
            catch (Exception exception)
            {
                Debug("Could not send the clan menu to " + player.Nickname + ": " + exception.Message);
            }
        }

        public string BuildStatus(Player player)
        {
            MessageSection messages = plugin.Config.Messages;
            Clan clan = plugin.Store.GetByMember(player.UserId);

            string text = clan == null
                ? messages.StatusNoClan
                : plugin.Parts()
                    .Set("clan", clan.Name)
                    .Set("role", plugin.Service.Role(clan, player.UserId))
                    .Set("count", clan.Members.Count)
                    .Set("max", plugin.Config.Rules.MaxMembers)
                    .Apply(messages.StatusMember);

            List<PendingInvite> invites = plugin.Service.InvitesOf(player);

            if (invites.Count > 0)
            {
                List<string> lines = new List<string>();

                foreach (PendingInvite invite in invites)
                {
                    lines.Add(plugin.Parts()
                        .Set("clan", invite.Clan)
                        .Set("player", invite.InviterName)
                        .Set("seconds", invite.SecondsLeft)
                        .Apply(messages.StatusInviteLine));
                }

                text = text + "\n" + plugin.Parts().Set("invites", string.Join(", ", lines.ToArray())).Apply(messages.StatusInvites);
            }

            return text;
        }

        private void PushStatus(Player player, string content)
        {
            if (statusSetting == null || player == null || player.ReferenceHub == null)
                return;

            ReferenceHub hub = player.ReferenceHub;
            string text = string.IsNullOrWhiteSpace(content) ? BuildStatus(player) : content;

            try
            {
                statusSetting.SendTextUpdate(text, true, candidate => candidate == hub);
            }
            catch (Exception exception)
            {
                Debug("Could not update the clan status of " + player.Nickname + ": " + exception.Message);
            }
        }

        private void Debug(string message)
        {
            plugin.Debug(message);
        }

        private static bool IsTabOpen(Player player)
        {
            return player.ReferenceHub != null && ServerSpecificSettingsSync.IsTabOpenForUser(player.ReferenceHub);
        }

        private void OnStatusReceived(ReferenceHub hub, SSSUserStatusReport report)
        {
            if (hub == null || report.TabOpen)
                return;

            Player player = PlayerLookup.ByHub(hub);

            if (player == null)
                return;

            string userId = player.UserId;

            if (string.IsNullOrEmpty(userId) || !pendingRefresh.Remove(userId))
                return;

            Send(player);
        }

        private void OnSettingReceived(ReferenceHub hub, ServerSpecificSettingBase setting)
        {
            if (hub == null || setting == null || !IsOurs(setting))
                return;

            Player player = PlayerLookup.ByHub(hub);

            if (player == null)
                return;

            try
            {
                int id = setting.SettingId;

                if (IsDebounced(player, id))
                    return;

                if (id == CreateId)
                {
                    Handle(player, plugin.Service.Create(player, null, ReadName(player)), false);
                }
                else if (id == DeleteId)
                {
                    Handle(player, plugin.Service.Delete(player, ReadName(player)), false);
                }
                else if (id == InviteId)
                {
                    Handle(player, plugin.Service.Invite(player, ReadTarget(player), ReadName(player)), false);
                }
                else if (id == InfoId)
                {
                    Handle(player, plugin.Service.Info(player, ReadName(player)), true);
                }
                else if (id == AcceptId)
                {
                    Handle(player, plugin.Service.Accept(player, ReadName(player)), false);
                }
                else if (id == DeclineId)
                {
                    Handle(player, plugin.Service.Decline(player, ReadName(player)), false);
                }
                else if (id == LeaveId)
                {
                    Handle(player, plugin.Service.Leave(player), false);
                }
                else if (id == KickId)
                {
                    Handle(player, plugin.Service.Kick(player, ReadTarget(player), ReadName(player)), false);
                }
            }
            catch (Exception exception)
            {
                Log.Error("[ClanSystem] Menu action failed for " + player.Nickname + ": " + exception);
            }
        }

        private bool IsDebounced(Player player, int id)
        {
            string key = player.UserId + ":" + id;
            DateTime now = DateTime.UtcNow;

            if (lastPress.TryGetValue(key, out DateTime last) && (now - last).TotalMilliseconds < DebounceMilliseconds)
                return true;

            lastPress[key] = now;
            return false;
        }

        private void Handle(Player player, ClanOutcome outcome, bool keepStatus)
        {
            if (player == null)
                return;

            string message = outcome.Message;

            if (!string.IsNullOrWhiteSpace(message))
            {
                if (Settings.ReplyWithConsole)
                    player.SendConsoleMessage(message, outcome.Success ? "white" : "red");

                if (Settings.ReplyWithHint)
                    player.ShowHint(message, Settings.HintDuration);
            }

            if (keepStatus && outcome.Success && !string.IsNullOrWhiteSpace(message))
            {
                PushStatus(player, message);
                return;
            }

            Refresh(player);
        }

        private string ReadName(Player player)
        {
            if (player.ReferenceHub == null)
                return string.Empty;

            SSPlaintextSetting setting = ServerSpecificSettingsSync.GetSettingOfUser<SSPlaintextSetting>(player.ReferenceHub, NameId);

            return setting == null || string.IsNullOrWhiteSpace(setting.SyncInputText) ? string.Empty : setting.SyncInputText.Trim();
        }

        private Player ReadTarget(Player player)
        {
            if (player.ReferenceHub == null)
                return null;

            string userId = player.UserId;

            if (string.IsNullOrEmpty(userId))
                return null;

            SSDropdownSetting setting = ServerSpecificSettingsSync.GetSettingOfUser<SSDropdownSetting>(player.ReferenceHub, TargetId);

            if (setting == null || !targetIds.TryGetValue(userId, out List<int> ids))
                return null;

            int index = setting.SyncSelectionIndexRaw;

            if (index < 0 || index >= ids.Count)
                return null;

            int playerId = ids[index];

            return playerId < 0 ? null : PlayerLookup.ById(playerId);
        }

        private ServerSpecificSettingBase[] BuildShared()
        {
            List<int> ids;
            List<ServerSpecificSettingBase> list = new List<ServerSpecificSettingBase>();

            list.Add(new SSGroupHeader(HeaderId, Settings.Header, true, Settings.HeaderHint));
            list.Add(new SSPlaintextSetting(NameId, Settings.NameLabel, Settings.NamePlaceholder, Settings.NameLimit, hint: Settings.NameHint));
            list.Add(CreateTargetDropdown(null, out ids));
            statusSetting = CreateStatus(null);
            list.Add(statusSetting);
            list.Add(new SSButton(CreateId, Settings.CreateButton, Settings.CreateButton, null, Settings.CreateButtonHint));
            list.Add(new SSButton(DeleteId, Settings.DeleteButton, Settings.DeleteButton, null, Settings.DeleteButtonHint));
            list.Add(new SSButton(InviteId, Settings.InviteButton, Settings.InviteButton, null, Settings.InviteButtonHint));
            list.Add(new SSButton(InfoId, Settings.InfoButton, Settings.InfoButton, null, Settings.InfoButtonHint));
            list.Add(new SSButton(AcceptId, Settings.AcceptButton, Settings.AcceptButton, null, Settings.AcceptButtonHint));
            list.Add(new SSButton(DeclineId, Settings.DeclineButton, Settings.DeclineButton, null, Settings.DeclineButtonHint));
            list.Add(new SSButton(LeaveId, Settings.LeaveButton, Settings.LeaveButton, null, Settings.LeaveButtonHint));
            list.Add(new SSButton(KickId, Settings.KickButton, Settings.KickButton, null, Settings.KickButtonHint));

            return list.ToArray();
        }

        private SSDropdownSetting CreateTargetDropdown(Player player, out List<int> ids)
        {
            ids = new List<int> { -1 };
            List<string> options = new List<string> { Settings.TargetPlaceholder };

            if (player != null)
            {
                foreach (Player other in Player.List)
                {
                    if (other == null || ClanService.Same(other, player))
                        continue;

                    string name = string.IsNullOrWhiteSpace(other.Nickname) ? "Player " + other.Id : other.Nickname;
                    options.Add(name + " (" + other.Id + ")");
                    ids.Add(other.Id);
                }
            }

            if (ids.Count == 1)
                options[0] = Settings.NoTargetsOption;

            return new SSDropdownSetting(TargetId, Settings.TargetLabel, options.ToArray(), 0, SSDropdownSetting.DropdownEntryType.Scrollable, Settings.TargetHint, byte.MaxValue, false);
        }

        private SSTextArea CreateStatus(Player player)
        {
            string content = player == null ? Settings.StatusPlaceholder : BuildStatus(player);

            return new SSTextArea(StatusId, content, SSTextArea.FoldoutMode.NotCollapsable);
        }

        private bool IsOurs(ServerSpecificSettingBase setting)
        {
            if (setting == null)
                return false;

            foreach (ServerSpecificSettingBase mine in registered)
            {
                if (mine == null)
                    continue;

                if (ReferenceEquals(setting, mine) || ReferenceEquals(setting.OriginalDefinition, mine) || ReferenceEquals(mine.OriginalDefinition, setting))
                    return true;
            }

            return false;
        }

        private static ServerSpecificSettingBase[] Join(ServerSpecificSettingBase[] first, ServerSpecificSettingBase[] second)
        {
            List<ServerSpecificSettingBase> result = new List<ServerSpecificSettingBase>();

            if (first != null)
                result.AddRange(first);

            if (second != null)
                result.AddRange(second);

            return result.ToArray();
        }
    }
}
