using Exiled.API.Features;

namespace ClanSystem.Handlers
{
    public sealed class ServerHandlers
    {
        private readonly Plugin plugin;

        public ServerHandlers(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public void Enable()
        {
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
        }

        public void Disable()
        {
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
        }

        private void OnRoundStarted()
        {
            plugin.Invites.Prune();

            foreach (Player player in Player.List)
                plugin.Tag.Apply(player);

            plugin.Menu.RefreshAll();
        }
    }
}
