using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using VhaBot.Configuration;

namespace VhaBot.Configuration
{
    public partial class SlavesForm : Form
    {
        public SlavesForm(ConfigurationBot bot)
        {
            InitializeComponent();
            this.Bot = bot;
            if (bot != null && bot.Slaves != null)
                foreach (ConfigurationSlave slave in bot.Slaves)
                    this.listBots.Items.Add(slave);
        }

        private ConfigurationBot Bot;
        private ConfigurationSlave WorkingBot = null;
        private bool WorkingNew = false;

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
        }

        private void CleanEditMode()
        {
            this.txtAccount.Text = string.Empty;
            this.txtPassword.Text = string.Empty;
            this.txtCharacter.Text = string.Empty;
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
            if (this.WorkingNew)
            {
                ConfigurationSlave bot = new ConfigurationSlave();
                bot.Account = this.txtAccount.Text;
                bot.Password = this.txtPassword.Text;
                bot.Character = this.txtCharacter.Text;
                bot.Enabled = this.cbEnabled.Checked;
                this.listBots.Items.Add(bot);
                this.CleanEditMode();
            }
            else
            {
                this.WorkingBot.Account = this.txtAccount.Text;
                this.WorkingBot.Password = this.txtPassword.Text;
                this.WorkingBot.Character = this.txtCharacter.Text;
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
                ConfigurationSlave bot = (ConfigurationSlave)this.listBots.SelectedItem;
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
                ConfigurationSlave bot = (ConfigurationSlave)this.listBots.SelectedItem;
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
                ConfigurationSlave bot = (ConfigurationSlave)this.listBots.SelectedItem;
                if (bot == null)
                    return;

                if (MessageBox.Show("Are you sure you want to remove " + bot.ToString() + "?", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
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

        private void SlavesForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult result = MessageBox.Show("Do you wish to keep these new settings?", "Question", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                List<ConfigurationSlave> slaves = new List<ConfigurationSlave>();
                foreach (object obj in this.listBots.Items)
                {
                    try
                    {
                        ConfigurationSlave slave = (ConfigurationSlave)obj;
                        slaves.Add(slave);
                    }
                    catch { }
                }
                if (this.Bot != null)
                    this.Bot.Slaves = slaves.ToArray();
            }
            else if (result == DialogResult.Cancel)
                e.Cancel = true;
        }
    }
}