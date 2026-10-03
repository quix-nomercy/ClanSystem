using System;
using ClanSystem.Core;
using ClanSystem.Handlers;
using ClanSystem.Menu;
using Exiled.API.Features;

namespace ClanSystem
{
    public sealed class Plugin : Plugin<Config>
    {
        public static Plugin Instance { get; private set; }

        public override string Name => "ClanSystem";

        public override string Prefix => "clan_system";

        public override string Author => "quix";

        public override Version Version => new Version(1, 0, 0);

        public ClanStore Store { get; private set; }

        public PendingInvites Invites { get; private set; }

        public ClanTag Tag { get; private set; }

        public ClanService Service { get; private set; }

        public ClanMenu Menu { get; private set; }

        private PlayerHandlers playerHandlers;

        private ServerHandlers serverHandlers;

        public string RootCommand
        {
            get { return string.IsNullOrWhiteSpace(Config.Command.Name) ? "clan" : Config.Command.Name.Trim(); }
        }

        public string[] RootAliases
        {
            get { return Names.All(Config.Command.Aliases, "clans"); }
        }

        public override void OnEnabled()
        {
            Instance = this;

            Store = new ClanStore(this);
            Store.Load();
            Invites = new PendingInvites();
            Tag = new ClanTag(this);
            Service = new ClanService(this);
            Menu = new ClanMenu(this);

            playerHandlers = new PlayerHandlers(this);
            serverHandlers = new ServerHandlers(this);

            playerHandlers.Enable();
            serverHandlers.Enable();
            Menu.Enable();

            foreach (Player player in Player.List)
                Tag.Apply(player);

            Menu.RefreshAll();

            Log.Info("[ClanSystem] Enabled with " + Store.Count + " clan(s).");
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            if (playerHandlers != null)
                playerHandlers.Disable();

            if (serverHandlers != null)
                serverHandlers.Disable();

            if (Menu != null)
                Menu.Disable();

            foreach (Player player in Player.List)
                Tag.Clear(player);

            if (Store != null)
                Store.Save();

            if (Invites != null)
                Invites.Clear();

            playerHandlers = null;
            serverHandlers = null;
            Menu = null;
            Service = null;
            Tag = null;
            Invites = null;
            Store = null;
            Instance = null;

            base.OnDisabled();
        }

        public MessageParts Parts()
        {
            CommandSection command = Config.Command;

            return new MessageParts()
                .Set("command", RootCommand)
                .Set("create", Names.Primary(command.Create, "create"))
                .Set("delete", Names.Primary(command.Delete, "delete"))
                .Set("invite", Names.Primary(command.Invite, "invite"))
                .Set("info", Names.Primary(command.Info, "info"))
                .Set("accept", Names.Primary(command.Accept, "accept"))
                .Set("decline", Names.Primary(command.Decline, "decline"))
                .Set("leave", Names.Primary(command.Leave, "leave"))
                .Set("kick", Names.Primary(command.Kick, "kick"))
                .Set("list", Names.Primary(command.ListNames, "list"));
        }

        public string Usage(ClanAction action)
        {
            MessageSection messages = Config.Messages;
            string template;

            switch (action)
            {
                case ClanAction.Create:
                    template = messages.CreateUsage;
                    break;
                case ClanAction.Delete:
                    template = messages.DeleteUsage;
                    break;
                case ClanAction.Invite:
                    template = messages.InviteUsage;
                    break;
                case ClanAction.Info:
                    template = messages.InfoUsage;
                    break;
                case ClanAction.Accept:
                    template = messages.AcceptUsage;
                    break;
                case ClanAction.Decline:
                    template = messages.DeclineUsage;
                    break;
                case ClanAction.Leave:
                    template = messages.LeaveUsage;
                    break;
                case ClanAction.Kick:
                    template = messages.KickUsage;
                    break;
                default:
                    template = messages.ListUsage;
                    break;
            }

            return Parts().Apply(template);
        }

        public void Announce(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            Map.Broadcast(Config.Menu.AnnouncementDuration, message);
        }

        public void NotifyStorageError(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
                return;

            string message = Parts().Set("error", error).Apply(Config.Messages.StorageError);

            foreach (Player player in Player.List)
            {
                if (player == null || !player.RemoteAdminAccess)
                    continue;

                player.SendConsoleMessage(message, "red");
            }
        }

        public void Debug(string message)
        {
            if (!Config.Debug || string.IsNullOrWhiteSpace(message))
                return;

            Log.Debug("[ClanSystem] " + message);
        }
    }
}
