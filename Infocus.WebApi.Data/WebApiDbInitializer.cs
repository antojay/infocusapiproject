using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Data
{
    public class WebApiDbInitializer : System.Data.Entity.CreateDatabaseIfNotExists<WebApiDbContext>
    {
    }
}
