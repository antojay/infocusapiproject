using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.Framework.SapBone
{
    public sealed class BoneDatabaseValidValuesList : List<BoneDatabaseValidValue>
    {
        public BoneDatabaseValidValuesList()
            : base()
        {
        }

        public static BoneDatabaseValidValuesList YesNoValidValuesList
        {
            get
            {
                BoneDatabaseValidValuesList list = new BoneDatabaseValidValuesList();
                list.Add("Y", "Yes");
                list.Add("N", "No");
                return list;
            }
        }

        public void Add(String value, String description)
        {
            Add(new BoneDatabaseValidValue(value, description));
        }
    }
}
