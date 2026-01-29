using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Infocus.Framework.SapBone
{
    public sealed class BoneUserPermissionInfo
    {
        public BoneUserPermissionInfo(String userPermissionId)
        {
            Code = userPermissionId;
        }
        public String Code
        {
            get;
            set;
        }

        private PermissionType _authorization = PermissionType.None;
        public PermissionType Permission
        {
            get 
            { 
                return _authorization; 
            }
            set 
            { 
                _authorization = value; 
            }
        }

        public String FormType
        {
            get;
            set;
        }
    }

    public enum PermissionType
    {
        None,
        Readonly,
        Full
    }
}
