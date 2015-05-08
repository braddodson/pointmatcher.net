using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using MathNet.Numerics.LinearAlgebra.Generic;
using MathNet.Numerics.LinearAlgebra.Generic.Factorization;
using MathNet.Numerics.LinearAlgebra.Single.Factorization;

namespace pointmatcher.net
{
    public enum SamplingMethod
    {
        RandomSampling,
        Bin
    }

    public class SamplingSurfaceNormalDataPointsFilter : IDataPointsFilter
    {
        private float ratio;
		private int knn;
		private SamplingMethod samplingMethod; 
		private float maxBoxDim;
        private Random r = new Random();

        public SamplingSurfaceNormalDataPointsFilter(
            SamplingMethod samplingMethod = SamplingMethod.RandomSampling,
            float ratio = 0.5f,
            int knn = 7,
            float maxBoxDim = float.PositiveInfinity)
        {
            this.samplingMethod = samplingMethod;
            this.ratio = ratio;
            this.knn = knn;
            this.maxBoxDim = maxBoxDim;
        }

        public DataPoints Filter(DataPoints input)
        {
            int pointsCount = input.points.Length;

            var buildData = new BuildData
            {
                points = input.points,
                indices = Enumerable.Range(0, pointsCount).ToArray(),
                pointsToKeep = new List<DataPoint>(),
                unfitPointsCount = 0
            };
	        // build the new point cloud
	        buildNew(
		        buildData,
		        0,
		        pointsCount,
		        MinBound(input.points.Select(p => p.point)),
                MaxBound(input.points.Select(p => p.point))
	        );

            return new DataPoints
            {
                points = buildData.pointsToKeep.ToArray(),
                contiansNormals = true
            };
        }

        /// <summary>
        /// This method essentially recurses, building a k-d tree of the points between first and last
        /// Once the number of points is smaller than knn, it uses that set of points to compute a surface normal
        /// From that set of points, 
        /// </summary>        
        void buildNew(BuildData data, int first, int last, Vector3 minValues, Vector3 maxValues)
        {
	        int count = last - first;
	        if (count <= knn)
	        {
		        // compute for this range
		        fuseRange(data, first, last);
		        // TODO: make another filter that creates constant-density clouds,
		        // typically by stopping recursion after the median of the bounding cuboid
		        // is below a threshold, or that the number of points falls under a threshold
		        return;
	        }

	        // find the largest dimension of the box
	        int cutDim = MaxDim(maxValues - minValues);

	        // compute number of elements
	        int rightCount = count/2;
	        int leftCount = count - rightCount;
	        Debug.Assert(last - rightCount == first + leftCount);

	        // select the cut point and partition the indices around it
            var pts = data.points;
            QuickSelect.Select(data.indices, first, last - 1, first + leftCount, i => GetAt(data.points[i].point, cutDim));

	        // get value
	        int cutIndex = data.indices[first+leftCount];
	        float cutVal = GetAt(data.points[cutIndex].point, cutDim);

	        // update bounds for left
	        Vector3 leftMaxValues = SetAt(maxValues, cutDim, cutVal);
	        // update bounds for right
	        Vector3 rightMinValues = SetAt(minValues, cutDim, cutVal);

	        // recurse
	        buildNew(data, first, first + leftCount, minValues, leftMaxValues);
	        buildNew(data, first + leftCount, last, rightMinValues, maxValues);
        }

        void fuseRange(BuildData data, int first, int last)
        {
	        int colCount = last-first;
            
            var sum = Vector3.Zero;
	        for (int i = 0; i < colCount; i++)
            {
                sum += data.points[data.indices[first + i]].point;
            }

            var mean = sum / colCount;
            var NN = new MathNet.Numerics.LinearAlgebra.Single.DenseMatrix(3, colCount);
            var min = new Vector3(float.MaxValue);
            var max = new Vector3(float.MinValue);
            for (int i = 0; i < colCount; i++)
            {
                var v = data.points[data.indices[first + i]].point;
                var p = v - mean;
                NN.At(0, i, p.X);
                NN.At(1, i, p.Y);
                NN.At(2, i, p.Z);

                min = Vector3.Min(min, v);
                max = Vector3.Max(max, v);
            }

	        Vector3 box = max - min;
	        float boxDim = GetAt(box, MaxDim(box));
	        // drop box if it is too large
	        if (boxDim > maxBoxDim)
	        {
		        data.unfitPointsCount += colCount;
		        return;
	        }

	        // compute covariance
	        var C = NN.TransposeAndMultiply(NN);
	        var eigen = C.Evd();
	        // Ensure that the matrix is suited for eigenvalues calculation
		    if(eigen.Rank < 2)
		    {
                data.unfitPointsCount += colCount;
			    return;
		    }

            var normal = computeNormal(eigen.EigenValues(), eigen.EigenVectors());

	        /*T densitie = 0;
	        if(keepDensities)
		        densitie = SurfaceNormalDataPointsFilter::computeDensity(NN);*/

	        // Filter points randomly
	        if(samplingMethod == SamplingMethod.RandomSampling)
	        {
		        for(int i=0; i<colCount; i++)
		        {
			        float x = (float)r.NextDouble();
			        if(x < ratio)
			        {
				        // Keep points with their descriptors
				        int k = data.indices[first+i];
				        // Mark the indices which will be part of the final data
				        data.pointsToKeep.Add(new DataPoint
                            {
                                point = data.points[k].point,
                                normal = normal,
                            }); ;
			        }
		        }
	        }
	        else if (samplingMethod == SamplingMethod.Bin)
	        {
		        // Use the average and norm
                data.pointsToKeep.Add(new DataPoint
                    {
                        point = mean,
                        normal = normal
                    });
	       }
        }

        Vector3 computeNormal(MathNet.Numerics.LinearAlgebra.Generic.Vector<Complex> eigenVa, Matrix<float> eigenVe)
        {
	        // Keep the smallest eigenvector as surface normal
	        int smallestId = 0;
	        float smallestValue = float.MaxValue;
	        for(int j = 0; j < eigenVe.ColumnCount; j++)
	        {
                float lambda = (float)eigenVa[j].Real;
                if (lambda < smallestValue)
		        {
			        smallestId = j;
                    smallestValue = lambda;
		        }
	        }

            var normalVector = eigenVe.Column(smallestId);
            return ToVector3(normalVector);
        }

        private static Vector3 ToVector3(MathNet.Numerics.LinearAlgebra.Generic.Vector<float> v)
        {
            return new Vector3(v[0], v[1], v[2]);
        }

        private static Vector3 MinBound(IEnumerable<Vector3> vectors)
        {
            var result = new Vector3(float.MaxValue);
            foreach (var v in vectors)
            {
                result = Vector3.Min(result, v);
            }

            return result;
        }

        private static Vector3 MaxBound(IEnumerable<Vector3> vectors)
        {
            var result = new Vector3(float.MinValue);
            foreach (var v in vectors)
            {
                result = Vector3.Max(result, v);
            }

            return result;
        }

        static float GetAt(Vector3 vector, int index)
        {
            switch (index)
            {
                case 0:
                    return vector.X;
                case 1:
                    return vector.Y;
                case 2:
                    return vector.Z;
                default:
                    throw new ArgumentException("index is invalid", "index");
            }
        }

        Vector3 SetAt(Vector3 vector, int index, float v)
        {
            switch (index)
            {
                case 0:
                    vector.X = v;
                    break;
                case 1:
                    vector.Y = v;
                    break;
                case 2:
                    vector.Z = v;
                    break;
            }

            return vector;
        }

        int MaxDim(Vector3 v)
        {
            if (v.X > v.Y && v.X > v.Z)
            {
                return 0;
            }
            else if (v.Y > v.Z)
            {
                return 1;
            }
            else
            {
                return 2;
            }
        }

        struct BuildData
		{			
			public int[] indices;
            public DataPoint[] points;
            public int unfitPointsCount;

            public List<DataPoint> pointsToKeep;
		}
    }
}
