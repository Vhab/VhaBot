using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot.Communication
{
    public delegate ClientCommunication AuthorizeClientDelegate(string id, string key);

    public class ServerCommunication : MarshalByRefObject
    {
        public static AuthorizeClientDelegate OnAuthorizeClient;

        public ClientCommunication AuthorizeClient(string id, string key)
        {
            if (OnAuthorizeClient != null)
                return OnAuthorizeClient(id, key);
            else
                return null;
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
}
