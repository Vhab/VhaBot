using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using AoLib.Utils;

namespace VhaBot.ShellModules
{

    public abstract class IUsers
    {
        public abstract void RemoveAll();

        public abstract bool AddUser(string username, UserLevel userlevel);
        public abstract bool AddUser(string username, UserLevel userlevel, string addedBy);
        public abstract bool AddUser(string username, UserLevel userlevel, string addedBy, long addedOn);

        public abstract bool AddAlt(string mainname, string altname);

        public abstract bool UserExists(string username);
        public abstract bool UserExists(string username, UInt32 userid);

        public abstract bool IsAlt(string username);

        public abstract string GetMain(string altname);

        public abstract string[] GetAlts(string username);

        public abstract SortedDictionary<string, string> GetAllAlts();

        public abstract void RemoveUser(string username);

        public abstract void RemoveAlt(string username);

        public abstract UserLevel GetUser(string username);

        public abstract User GetUserInformation(string username);

        public abstract SortedDictionary<string, UserLevel> GetUsers();

        public abstract bool SetUser(string username, UserLevel userlevel);

        public abstract bool Authorized(string username, UserLevel userlevel);
    }
}
