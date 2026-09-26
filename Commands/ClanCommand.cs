using System;
using System.Collections.Generic;
using System.Linq;
using ClanSystem.Core;
using CommandSystem;
using Exiled.API.Features;
using RemoteAdmin;

namespace ClanSystem.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    [CommandHandler(typeof(ClientCommandHandler))]
    public sealed class ClanCommand : ICommand
    {
        public string Command
        {
            get { return Plugin.Instance == null ? "clan" : Plugin.Instance.RootCommand; }
        }

        public string[] Aliases
        {
            get { return Plugin.Instance == null ? new[] { "clans" } : Plugin.Instance.RootAliases; }
        }

        public string Description
        {
            get { return "Clan management: create, delete, invite, info, accept, decline, leave, kick and list."; }
        }

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Plugin plugin = Plugin.Instance;

            if (plugin == null)
            {
                response = "ClanSystem is not enabled.";
                return false;
            }

            if (!HasPermission(plugin, sender))
            {
                response = plugin.Config.Messages.NoPermission;
                return false;
            }

            List<string> args = arguments.ToList();

            if (args.Count == 0)
            {
                response = plugin.Parts().Apply(plugin.Config.Messages.Usage);
                return true;
            }

            Player player = PlayerLookup.FromSender(sender);
            string action = args[0];
            List<string> rest = args.Skip(1).ToList();
            CommandSection command = plugin.Config.Command;

            if (Names.Matches(command.Create, action))
            {
                Player owner = null;

                if (rest.Count > 1)
                {
                    owner = PlayerLookup.ByQuery(rest[1]);

                    if (owner == null)
                    {
                        response = NotFound(plugin, rest[1]);
                        return false;
                    }
                }

                return Respond(plugin.Service.Create(player, owner, Argument(rest, 0)), out response);
            }

            if (Names.Matches(command.Delete, action))
                return Respond(plugin.Service.Delete(player, Argument(rest, 0)), out response);

            if (Names.Matches(command.Invite, action))
            {
                if (rest.Count == 0)
                {
                    response = plugin.Parts().Set("usage", plugin.Usage(ClanAction.Invite)).Apply(plugin.Config.Messages.PlayerRequired);
                    return false;
                }

                Player target = PlayerLookup.ByQuery(rest[0]);

                if (target == null)
                {
                    response = NotFound(plugin, rest[0]);
                    return false;
                }

                return Respond(plugin.Service.Invite(player, target, Argument(rest, 1)), out response);
            }

            if (Names.Matches(command.Info, action))
                return Respond(plugin.Service.Info(player, Argument(rest, 0)), out response);

            if (Names.Matches(command.Accept, action))
                return Respond(plugin.Service.Accept(player, Argument(rest, 0)), out response);

            if (Names.Matches(command.Decline, action))
                return Respond(plugin.Service.Decline(player, Argument(rest, 0)), out response);

            if (Names.Matches(command.Leave, action))
                return Respond(plugin.Service.Leave(player), out response);

            if (Names.Matches(command.Kick, action))
            {
                if (rest.Count == 0)
                {
                    response = plugin.Parts().Set("usage", plugin.Usage(ClanAction.Kick)).Apply(plugin.Config.Messages.PlayerRequired);
                    return false;
                }

                Player target = PlayerLookup.ByQuery(rest[0]);

                if (target == null)
                {
                    response = NotFound(plugin, rest[0]);
                    return false;
                }

                return Respond(plugin.Service.Kick(player, target, Argument(rest, 1)), out response);
            }

            if (Names.Matches(command.ListNames, action))
                return Respond(plugin.Service.List(), out response);

            response = plugin.Parts()
                .Set("action", action)
                .Set("usage", plugin.Parts().Apply(plugin.Config.Messages.Usage))
                .Apply(plugin.Config.Messages.UnknownAction);

            return false;
        }

        private static string Argument(List<string> args, int index)
        {
            return index >= 0 && index < args.Count ? args[index] : null;
        }

        private static bool Respond(ClanOutcome outcome, out string response)
        {
            response = outcome.Message;
            return outcome.Success;
        }

        private static string NotFound(Plugin plugin, string query)
        {
            return plugin.Parts().Set("query", query).Apply(plugin.Config.Messages.PlayerNotFound);
        }

        private static bool HasPermission(Plugin plugin, ICommandSender sender)
        {
            CommandSection command = plugin.Config.Command;

            if (!command.RequirePermission)
                return true;

            if (sender == null || sender is ServerConsoleSender)
                return true;

            CommandSender commandSender = sender as CommandSender;

            if (commandSender == null)
                return false;

            if (commandSender.FullPermissions)
                return true;

            return PermissionsHandler.IsPermitted(commandSender.Permissions, command.PermissionFlag);
        }
    }
}
