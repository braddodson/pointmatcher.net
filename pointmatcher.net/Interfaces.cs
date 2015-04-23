using MathNet.Numerics.LinearAlgebra.Generic;
using MathNet.Numerics.LinearAlgebra.Single;
using MathNet.Numerics.LinearAlgebra.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace pointmatcher.net
{
    public struct DataPoint
    {
        public Vector3 point;
        public Vector3 normal;
    }

    public class DataPoints
    {
        public DataPoint[] points;
        public bool contiansNormals;
    }

    public class Matches
    {		
        /// <summary>
        /// Squared distances to closest points
        /// Columns represent different query points, rows are k matches
        /// </summary>
		public DenseColumnMajorMatrixStorage<float> Dists;

        /// <summary>
        /// Identifiers of closest points
        /// </summary>
		public DenseColumnMajorMatrixStorage<int> Ids;
		
		public float GetDistsQuantile(float quantile)
        {
            float[] d = Dists.Data;
            int[] indices = Enumerable.Range(0, d.Length).ToArray();
            int n = (int)(d.Length * quantile);
            QuickSelect.Select(indices, 0, d.Length - 1, n, i => d[i]);
            return d[indices[n]];
        }
    }

    public interface IDataPointsFilter
    {
        DataPoints Filter(DataPoints input);
    }

    public interface IMatcherFactory
    {
        IMatcher ConstructMatcher(DataPoints reference);
    }

    public interface IMatcher
    {
        Matches FindClosests(DataPoints filteredReading);
    }

    public interface IOutlierFilter
    {
        Matrix<float> ComputeOutlierWeights(DataPoints filteredReading, DataPoints filteredReference, Matches matches);
    }

    public interface IErrorMinimizer
    {
        EuclideanTransform SolveForTransform(ErrorElements mPts);
    }

    public interface ITransformationCheckerFactory
    {
        ITransformationChecker CreateTransformationChecker();
    }

    public interface ITransformationChecker
    {
        bool ShouldContinue(EuclideanTransform transform);
    }

    public interface IInspector
    {
        void Inspect(DataPoints pointSet, string name);
    }

    public class NoOpInspector : IInspector
    {
        public void Inspect(DataPoints pointSet, string name)
        {
            
        }
    }
}
