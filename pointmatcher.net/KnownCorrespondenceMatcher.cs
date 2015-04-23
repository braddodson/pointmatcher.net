using MathNet.Numerics.LinearAlgebra.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pointmatcher.net
{
    /// <summary>
    /// A matcher for use with ICP when the input data already corresponds to known matches between data points
    /// </summary>
    public class KnownCorrespondenceMatcherFactory : IMatcherFactory
    {
        public IMatcher ConstructMatcher(DataPoints reference)
        {
            return new KnownCorrespondenceMatcher(reference);
        }

        private class KnownCorrespondenceMatcher : IMatcher
        {
            private DataPoints reference;

            public KnownCorrespondenceMatcher(DataPoints reference)
            {
                this.reference = reference;
            }

            public Matches FindClosests(DataPoints filteredReading)
            {
                int n = this.reference.points.Length;
                var indexes = DenseColumnMajorMatrixStorage<int>.OfInit(1, n, (i, j) => j);
                var distances = DenseColumnMajorMatrixStorage<float>.OfInit(
                    1,
                    n,
                    (i, j) => (filteredReading.points[j].point - this.reference.points[j].point).LengthSquared());
                return new Matches
                {
                    Dists = distances,
                    Ids = indexes,
                };
            }
        }
    }
}
