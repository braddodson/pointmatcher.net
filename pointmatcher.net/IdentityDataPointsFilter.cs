using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pointmatcher.net
{
    public class IdentityDataPointsFilter : IDataPointsFilter
    {
        public DataPoints Filter(DataPoints input)
        {
            return input;
        }
    }
}
