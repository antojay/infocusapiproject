using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Infocus.Common
{
    [Serializable]
    public class ConnectionInfo
    {
        public ConnectionInfo()
        {
        }
        public ConnectionInfo(String user, String password)
        {
            User = user;
            Password = password;
        }
        public ConnectionInfo(String user, String password, String server) : this(user, password)
        {
            Server = server;
        }
        public ConnectionInfo(Boolean isTrusted, String server)
        {
            IsTrusted = isTrusted;
            Server = server;
        }
        public String User
        {
            get;
            set;
        }

        public String Password
        {
            get;
            set;
        }

        public String Server
        {
            get;
            set;
        }

        public Boolean IsTrusted
        {
            get;
            set;
        }
    }
}
