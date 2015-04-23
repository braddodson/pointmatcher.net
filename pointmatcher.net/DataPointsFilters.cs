using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pointmatcher.net
{
    class DataPointsFilters : IDataPointsFilter
    {
        public IEnumerable<IDataPointsFilter> Filters { get; set; }

        public DataPoints Filter(DataPoints input)
        {
            var result = input;
            foreach (var filter in this.Filters)
            {
                result = filter.Filter(result);
            }

            return result;
        }
    }
}
