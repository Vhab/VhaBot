using System;
using System.Collections.Generic;
using System.Text;
using AoLib.Utils;
using VhaBot;

namespace VhaBot.Plugins
{
    public class RaidPointsTicker : PluginBase
    {
        private RaidCore _core;
        private RaidDatabase _database;
        private BotShell _bot;

        public RaidPointsTicker()
        {
            this.Name = "Raid :: Points Ticker";
            this.InternalName = "RaidPointsTicker";
            this.Version = 100;
            this.Author = "Vhab";
            this.DefaultState = PluginState.Disabled;
            this.Dependencies = new string[] { "RaidDatabase", "RaidCore" };
            this.Description = "Automatically adds points to everyone on the raid while the raid is unpaused.\nIt will add 0.1 point each minute to everyone on the raid while loaded.";
        }

        public override void OnLoad(BotShell bot)
        {
            try { this._database = (RaidDatabase)bot.Plugins.GetPlugin("RaidDatabase"); }
            catch { throw new Exception("Unable to connect to 'Raid :: Database' Plugin!"); }
            try { this._core = (RaidCore)bot.Plugins.GetPlugin("RaidCore"); }
            catch { throw new Exception("Unable to connect to 'Raid :: Core' Plugin!"); }
            if (!this._database.Connected)
                throw new Exception("Not connected to the database!");
            this._bot = bot;
            bot.Timers.Minute += new EventHandler(OnTimer);
        }

        public override void OnUnload(BotShell bot)
        {
            bot.Timers.Minute -= new EventHandler(OnTimer);
        }

        public void OnTimer(object sender, EventArgs e)
        {
            if (!this._core.Running)
                return;
            if (this._core.Paused)
                return;
            this._database.ExecuteNonQuery("UPDATE points, raiders SET points.points = points.points+1 WHERE points.main = raiders.main AND raiders.onRaid = 1 AND raiders.raidID = " + this._core.RaidID);
        }
    }
}
