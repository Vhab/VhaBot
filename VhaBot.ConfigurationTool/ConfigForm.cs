using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using VhaBot;

namespace VhaBot.Configuration
{
    public partial class ConfigForm : Form
    {
        public static string CONFIGFILE = "config.xml";
        public ConfigForm()
        {
            InitializeComponent();
            this.LoadConfig();
        }

        private ConfigurationBot WorkingBot = null;
        private bool WorkingNew = false;

        private void LoadConfig()
        {
            ConfigurationBase config = ConfigurationReader.Read(ConfigForm.CONFIGFILE);
            if (config == null)
            {
                // Create the objects myself to obtain default values
                config = new ConfigurationBase();
                config.Core = new ConfigurationCore();
            }
            this.txtCentralServer.Text = config.Core.CentralServer;
            this.txtConfigPath.Text = config.Core.ConfigPath;
            this.txtPluginsPath.Text = config.Core.PluginsPath;
            this.txtSkinsPath.Text = config.Core.SkinsPath;
            this.txtCachePath.Text = config.Core.CachePath;
            this.cbDebug.Checked = config.Core.Debug;
            this.listBots.Items.Clear();
            if (config.Bots != null)
                foreach (ConfigurationBot bot in config.Bots)
                    this.listBots.Items.Add(bot);
        }

        private void SaveConfig()
        {
            ConfigurationBase config = new ConfigurationBase();
            List<ConfigurationBot> bots = new List<ConfigurationBot>();
            foreach (object bot in this.listBots.Items)
            {
                try
                {
                    bots.Add((ConfigurationBot)bot);
                }
                catch { }
            }
            config.Bots = bots.ToArray();
            config.Core = new ConfigurationCore();
            config.Core.CentralServer= this.txtCentralServer.Text;
            config.Core.ConfigPath = this.txtConfigPath.Text;
            config.Core.PluginsPath = this.txtPluginsPath.Text;
            config.Core.SkinsPath = this.txtSkinsPath.Text;
            config.Core.CachePath = this.txtCachePath.Text;
            config.Core.Debug = this.cbDebug.Checked;
            if (ConfigurationWriter.Write(ConfigForm.CONFIGFILE, config) == false)
                MessageBox.Show("Unable to save config file!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void SwitchMode(bool toEdit)
        {
            this.listBots.Enabled = !toEdit;
            this.btnNew.Enabled = !toEdit;

            if (this.listBots.SelectedIndex < 0)
                this.SwitchTools(false);
            else
                this.SwitchTools(!toEdit);

            this.txtAccount.Enabled = toEdit;
            this.txtPassword.Enabled = toEdit;
            this.txtCharacter.Enabled = toEdit;
            this.cbDimension.Enabled = toEdit;
            this.txtAdmin.Enabled = toEdit;
            this.cbEnabled.Enabled = toEdit;

            this.btnAccept.Enabled = toEdit;
            this.btnCancel.Enabled = toEdit;
        }

        private void SwitchTools(bool enabled)
        {
            this.btnUp.Enabled = enabled;
            this.btnDown.Enabled = enabled;
            this.btnEdit.Enabled = enabled;
            this.btnDelete.Enabled = enabled;
            this.btnSlaves.Enabled = enabled;
        }

        private void CleanEditMode()
        {
            this.txtAccount.Text = string.Empty;
            this.txtPassword.Text = string.Empty;
            this.txtCharacter.Text = string.Empty;
            this.cbDimension.Text = "Atlantean";
            this.txtAdmin.Text = string.Empty;
            this.cbEnabled.Checked = false;
        }

        private void btnNew_Click(object sender, EventArgs e)
        {
            this.WorkingNew = true;
            this.WorkingBot = null;
            this.CleanEditMode();
            this.SwitchMode(true);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.SwitchMode(false);
            if (this.WorkingNew)
                this.CleanEditMode();
        }

        private void btnAccept_Click(object sender, EventArgs e)
        {
            if (this.txtAccount.Text == string.Empty)
            {
                MessageBox.Show("Missing Account Name", "Error");
                return;
            }
            if (this.txtPassword.Text == string.Empty)
            {
                MessageBox.Show("Missing Account Password", "Error");
                return;
            }
            if (this.txtCharacter.Text == string.Empty)
            {
                MessageBox.Show("Missing Character Name", "Error");
                return;
            }
            if (this.cbDimension.Text == string.Empty)
            {
                MessageBox.Show("Missing Dimension", "Error");
                return;
            }
            if (this.txtAdmin.Text == string.Empty)
            {
                MessageBox.Show("Missing Bot Admin", "Error");
                return;
            }
            if (this.WorkingNew)
            {
                ConfigurationBot bot = new ConfigurationBot();
                bot.Account = this.txtAccount.Text;
                bot.Password = this.txtPassword.Text;
                bot.Character = this.txtCharacter.Text;
                bot.Admin = this.txtAdmin.Text;
                bot.Dimension = this.cbDimension.Text;
                bot.Enabled = this.cbEnabled.Checked;
                this.listBots.Items.Add(bot);
                this.CleanEditMode();
            }
            else
            {
                this.WorkingBot.Account = this.txtAccount.Text;
                this.WorkingBot.Password = this.txtPassword.Text;
                this.WorkingBot.Character = this.txtCharacter.Text;
                this.WorkingBot.Admin = this.txtAdmin.Text;
                this.WorkingBot.Dimension = this.cbDimension.Text;
                this.WorkingBot.Enabled = this.cbEnabled.Checked;
                if (this.listBots.SelectedIndex >= 0)
                    this.listBots.Items[this.listBots.SelectedIndex] = this.WorkingBot;
            }
            this.SwitchMode(false);
        }

        private void listBots_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                ConfigurationBot bot = (ConfigurationBot)this.listBots.SelectedItem;
                if (bot == null)
                {
                    this.CleanEditMode();
                    this.SwitchTools(false);
                    return;
                }
                this.txtAccount.Text = bot.Account;
                this.txtPassword.Text = string.Empty;
                for (int i = 0; i < bot.Password.Length; i++)
                    this.txtPassword.Text += "*";
                this.txtCharacter.Text = bot.Character;
                this.cbDimension.Text = bot.Dimension;
                this.txtAdmin.Text = bot.Admin;
                this.cbEnabled.Checked = bot.Enabled;
                this.WorkingBot = bot;

                this.SwitchTools(true);
            }
            catch { }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            try
            {
                ConfigurationBot bot = (ConfigurationBot)this.listBots.SelectedItem;
                if (bot == null)
                    return;

                this.WorkingNew = false;
                this.WorkingBot = bot;
                this.txtPassword.Text = bot.Password;
                this.SwitchMode(true);
            }
            catch { }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                ConfigurationBot bot = (ConfigurationBot)this.listBots.SelectedItem;
                if (bot == null)
                    return;

                if (MessageBox.Show("Are you sure you wish to remove " + bot.ToString() + "?", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    this.listBots.Items.Remove(bot);
                }
            }
            catch { }
        }

        private void btnUp_Click(object sender, EventArgs e)
        {
            try
            {
                int index = this.listBots.SelectedIndex;
                if (index > 0 && this.listBots.Items.Count > 1)
                {
                    object tmp = this.listBots.Items[index - 1];
                    if (tmp == null)
                        return;
                    this.listBots.Items[index - 1] = this.listBots.Items[index];
                    this.listBots.SelectedIndex = index - 1;
                    this.listBots.Items[index] = tmp;
                }
            }
            catch { }
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            try
            {
                int index = this.listBots.SelectedIndex;
                if (index < (this.listBots.Items.Count - 1) && index >= 0)
                {
                    object tmp = this.listBots.Items[index + 1];
                    if (tmp == null)
                        return;
                    this.listBots.Items[index + 1] = this.listBots.Items[index];
                    this.listBots.SelectedIndex = index + 1;
                    this.listBots.Items[index] = tmp;
                }
            }
            catch { }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.SaveConfig();
        }

        private void reloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you wish to reload the config file? All changes will be discarted.", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                this.LoadConfig();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ConfigForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult result = MessageBox.Show("Do you wish to save before exiting?", "Question", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (result == DialogResult.Cancel)
                e.Cancel = true;
            if (result == DialogResult.Yes)
                this.SaveConfig();
        }

        private void btnSlaves_Click(object sender, EventArgs e)
        {
            try
            {
                ConfigurationBot bot = (ConfigurationBot)this.listBots.SelectedItem;
                if (bot == null)
                    return;

                SlavesForm form = new SlavesForm(bot);
                form.ShowDialog();
                form.Dispose();
            }
            catch { }
            
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("This tool is designed to create and edit configuration files for VhaBot.\nVhaBot is an Anarchy Online chat bot created by Vhab.\nVhaBot is a free product created by and for the Anarchy Online community and may not to used for commercial purposes.\nFor more information visit our forums at: http://forums.vhabot.net/\n\nVhaBot © 2005-2007 Vhab", "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}