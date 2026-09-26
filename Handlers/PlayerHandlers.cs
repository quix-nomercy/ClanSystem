using System;
using ClanSystem.Core;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;

namespace ClanSystem.Handlers
{
    public sealed class PlayerHandlers : IDisposable
    {
        private readonly Plugin plugin;

        public PlayerHandlers(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public void Enable()
        {
            Exiled.Events.Handlers.Player.Verified += OnVerified;
            Exiled.Events.Handlers.Player.Left += OnLeft;
            Exiled.Events.Handlers.Player.ChangingNickname += OnChangingNickname;
        }

        public void Disable()
        {
            Exiled.Events.Handlers.Player.Verified -= OnVerified;
            Exiled.Events.Handlers.Player.Left -= OnLeft;
            Exiled.Events.Handlers.Player.ChangingNickname -= OnChangingNickname;
        }

        public void Dispose()
        {
            Disable();
        }

        private void OnVerified(VerifiedEventArgs ev)
        {
            Clan clan = plugin.Store.GetByMember(ev.Player.UserId);

            if (clan != null)
            {
                clan.RememberName(ev.Player.UserId, ev.Player.Nickname);
                plugin.Store.Save();
            }

            plugin.Tag.Apply(ev.Player);
            plugin.Invites.Prune();
            plugin.Menu.RefreshAll();
        }

        private void OnLeft(LeftEventArgs ev)
        {
            plugin.Invites.RemoveAll(ev.Player.UserId);
            plugin.Tag.Forget(ev.Player.UserId);
            plugin.Menu.RefreshAll();
        }

        private void OnChangingNickname(ChangingNicknameEventArgs ev)
        {
            if (ev.Player == null || !plugin.Config.Tag.Show)
                return;

            Clan clan = plugin.Store.GetByMember(ev.Player.UserId);

            if (clan == null)
                return;

            string stripped = plugin.Tag.StripTag(clan, ev.NewName);
            string baseName = string.IsNullOrWhiteSpace(stripped) ? ev.Player.Nickname : stripped;
            string display = plugin.Tag.BuildDisplayName(clan, baseName);

            if (!string.Equals(display, ev.NewName, StringComparison.Ordinal))
                ev.NewName = display;
        }
    }
}
