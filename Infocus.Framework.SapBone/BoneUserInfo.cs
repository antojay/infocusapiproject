using System;
using System.Collections.Generic;
using System.Text;

namespace Infocus.Framework.SapBone
{
    public sealed class BoneUserInfo
    {
        public Int32 UserId
        {
            get;
            internal set;
        }
        public String UserName
        {
            get;
            internal set;
        }

        public String DisplayName
        {
            get;
            internal set;
        }

        public Boolean IsSuperUser
        {
            get;
            internal set;
        }

        public String EmailAddress
        {
            get;
            internal set;
        }

        private readonly List<BoneUserPermissionInfo> _userPermissions = new List<BoneUserPermissionInfo>();

        public List<BoneUserPermissionInfo> UserPermissions
        {
            get { return _userPermissions; }
        } 

        
    }
}
