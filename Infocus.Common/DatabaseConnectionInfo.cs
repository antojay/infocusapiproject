using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Infocus.Common
{
    [Serializable]
    public class DatabaseConnectionInfo : ConnectionInfo
    {
        public String Database
        {
            get;
            set;
        }

        public DatabaseConnectionInfo()
        {
        }
        public DatabaseConnectionInfo(String user, String password) : base(user, password)
        {
        }
        public DatabaseConnectionInfo(String user, String password, String server) : base(user, password, server)
        {
        }
        public DatabaseConnectionInfo(Boolean isTrusted, String server) : base(isTrusted, server)
        {
        }

        public DatabaseConnectionInfo(String user, String password, String server, String database)
            : base(user, password, server)
        {
            Database = database;
        }

        public DatabaseConnectionInfo(Boolean isTrusted, String server, String database) : base(isTrusted, server)
        {
            Database = database;
        }

        private Int32 _connectionTimeout = 30;
        public Int32 ConnectionTimeout
        {
            get
            {
                return _connectionTimeout;
            }
            set
            {
                _connectionTimeout = value;
            }
        }

        private Int32 _queryTimeout = 60;
        public Int32 QueryTimeout
        {
            get
            {
                return _queryTimeout;
            }
            set
            {
                _queryTimeout = value;
            }
        }
    }
}
