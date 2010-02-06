using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot
{
    public class Command
    {
        public readonly UserLevel PrivateChannel;
        public readonly UserLevel PrivateMessage;
        public readonly UserLevel Organization;
        public readonly string CommandName;
        public readonly bool IsAlias;
        public readonly string Alias;
        public readonly bool Help;

        public Command(string command, bool help, UserLevel requirement) : this(command, help, requirement, requirement, requirement) { }
        public Command(string command, bool help, UserLevel requirementPrivateChannel, UserLevel requirementPrivateMessage, UserLevel requirementOrganization)
        {
            if (command == null || command == string.Empty)
                throw new ArgumentException("invalid command passed");

            this.CommandName = command.ToLower();
            this.Help = help;
            this.PrivateChannel = requirementPrivateChannel;
            this.PrivateMessage = requirementPrivateMessage;
            this.Organization = requirementOrganization;
            this.IsAlias = false;
        }

        public Command(string alias, string command)
        {
            if (alias == null || alias == string.Empty)
                throw new ArgumentException("invalid command passed");
            if (command == null || command == string.Empty)
                throw new ArgumentException("invalid command passed");

            this.CommandName = command.ToLower();
            this.Alias = alias.ToLower();
            this.IsAlias = true;
        }
    }
}
