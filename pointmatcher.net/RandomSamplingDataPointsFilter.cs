using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pointmatcher.net
{
    public class RandomSamplingDataPointsFilter : IDataPointsFilter
    {
        private Random r = new Random();

        public RandomSamplingDataPointsFilter(float prob = 0.75f)
        {
            this.Prob = prob;
        }

        public float Prob { get; set; }

        public DataPoints Filter(DataPoints input)
        {
            var result = new List<DataPoint>();
            foreach (var p in input.points)
            {
                if (r.NextDouble() < this.Prob)
                {
                    result.Add(p);
                }
            }

            return new DataPoints
            {
                points = result.ToArray(),
            };
        }
    }
}
