using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot.Communication
{
    public delegate ClientCommunication AuthorizeClientDelegate(string id, string key);
    public delegate ManagerCommunication AuthorizeManagerDelegate(string username, string password);

    public class ServerCommunication : MarshalByRefObject
    {
        public static AuthorizeClientDelegate OnAuthorizeClient;
        public static AuthorizeManagerDelegate OnAuthorizeManager;

        public ClientCommunication AuthorizeClient(string id, string key)
        {
            if (OnAuthorizeClient != null)
                return OnAuthorizeClient(id, key);
            else
                return null;
        }

        public ManagerCommunication AuthorizeManager(string username, string password)
        {
            if (OnAuthorizeManager != null)
                return OnAuthorizeManager(username, password);
            else
                return null;
        }
    }
}
