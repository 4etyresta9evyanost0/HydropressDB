using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Sql;
using System.Data.SqlClient;

namespace HydropressDB
{
    internal class DbManagement : ViewModel
    {
        public HydropressDBDataSet MainDataSet;
        public HydropressUserDBDataSet UserDataSet;
        public DbManagement() {}

    }
}
