using MathNet.Numerics.LinearAlgebra.Generic;
using MathNet.Numerics.LinearAlgebra.Single;
using MathNet.Numerics.LinearAlgebra.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace knearest
{
    public class KdTreeNearestNeighborSearch : INearestNeighborSearch
    {
        /// <summary>
        /// number of bits required to store dimension index + number of dimensions
        /// </summary>
		private int dimBitCount;

		/// <summary>
		/// mask to access dim
		/// </summary>
		private uint dimMask;

        private uint dimensions;
        private int bucketSize;
        private List<BucketEntry> buckets = new List<BucketEntry>();
        private List<Node> nodes = new List<Node>();
        private DenseColumnMajorMatrixStorage<float> cloud;

        public KdTreeNearestNeighborSearch(DenseColumnMajorMatrixStorage<float> cloud, int bucketSize = 8)
        {
            this.bucketSize = bucketSize;
            if (bucketSize < 2)
			    throw new ArgumentException(string.Format("Requested bucket size {0}, but must be larger than 2", bucketSize), "bucketSize");

            this.dimensions = (uint)cloud.RowCount;
            this.dimBitCount = GetStorageBitCount(dimensions);
            this.dimMask = (uint)((1 << dimBitCount) - 1);
            this.cloud = cloud;

		    if (cloud.ColumnCount <= bucketSize)
		    {
			    // make a single-bucket tree
			    for (int i = 0; i < cloud.ColumnCount; i++)
                {
				    buckets.Add(new BucketEntry(i));
                }

			    nodes.Add(Node.ConstuctLeafNode(CreateDimChildBucketSize((uint)this.dimensions, (uint)cloud.ColumnCount), 0));
			    return;
		    }
		
		    int maxNodeCount = (0x1 << (int)(32-dimBitCount)) - 1;
		    int estimatedNodeCount = cloud.ColumnCount / (bucketSize / 2);
		    if (estimatedNodeCount > maxNodeCount)
		    {
			    throw new ArgumentException(
                    string.Format(
                        "Cloud has a risk to have more nodes ({0}) than the kd-tree allows ({1}). The kd-tree has {2} bits for dimensions and {3} bits for node indices",
                        estimatedNodeCount,
                        maxNodeCount,
                        dimBitCount,
                        32-dimBitCount));
		    }
		
		    // build point vector and compute bounds
		    List<int> buildPoints = new List<int>(cloud.ColumnCount);
		    for (int i = 0; i < cloud.ColumnCount; ++i)
		    {
                buildPoints.Add(i);
		    }

            Vector<float> minBound = new DenseVector(cloud.RowCount);
            Vector<float> maxBound = new DenseVector(cloud.RowCount);
            for (int i = 0; i < cloud.ColumnCount; i++)
            {
                for (int j = 0; j < cloud.RowCount; j++)
                {
                    float v = cloud.At(j, i);
                    minBound[j] = Math.Min(minBound[j], v);
                    maxBound[j] = Math.Max(maxBound[j], v);
                }
            }

            // create nodes
            this.BuildNodes(buildPoints, 0, buildPoints.Count, minBound, maxBound);
        }

        public ulong knn(
            DenseColumnMajorMatrixStorage<float> query,
            DenseColumnMajorMatrixStorage<int> indices,
            DenseColumnMajorMatrixStorage<float> dists2,
            Vector<float> maxRadii,
            int k,
            float epsilon,
            SearchOptionFlags optionFlags)
	    {
		    checkSizesKnn(query, indices, dists2, k, optionFlags, maxRadii);
		
		    bool allowSelfMatch = optionFlags.HasFlag(SearchOptionFlags.AllowSelfMatch);
		    //bool sortResults = optionFlags.HasFlag(SearchOptionFlags.SortResults);
		    float maxError2 = ((1+epsilon)*(1+epsilon));
		
		    Debug.Assert(nodes.Count > 0);
		    ulong leafTouchedCount = 0;

		    var off = new float[this.dimensions];
		
            // TODO: Parallel.for?
		    for (int i = 0; i < query.ColumnCount; ++i)
		    {
                var heap = new ListPriorityQueue<BucketEntry>(k);
			    float maxRadius = maxRadii[i];
			    float maxRadius2 = maxRadius * maxRadius;
			    leafTouchedCount += onePointKnn(query, indices, dists2, i, heap, off, maxError2, maxRadius2, allowSelfMatch);
		    }

		    return leafTouchedCount;
	    }

        ulong onePointKnn(
            DenseColumnMajorMatrixStorage<float> query,
            DenseColumnMajorMatrixStorage<int> indices,
            DenseColumnMajorMatrixStorage<float> dists2,
            int i,
            IPriorityQueue<BucketEntry> heap,
            float[] off,
            float maxError2,
            float maxRadius2,
            bool allowSelfMatch)
	    {
            Array.Clear(off, 0, off.Length); // reset the maximum off values to 0
		    //heap.reset();
		    ulong leafTouchedCount = 0;
		
            leafTouchedCount += recurseKnn(query, i, 0, 0, heap, off, maxError2, maxRadius2, allowSelfMatch);
		    
		    /*if (sortResults)
			    heap.sort();*/

            int j = 0;
            foreach (var idx in heap.Items)
            {
                indices.At(j, i, idx.index);
                j++;
            }

            j = 0;
            foreach (var dist in heap.Priorities)
            {
                dists2.At(j, i, dist);
                j++;
            }

		    return leafTouchedCount;
	    }

        ulong recurseKnn(DenseColumnMajorMatrixStorage<float> query, int queryIdx, int n, float rd, IPriorityQueue<BucketEntry> heap, float[] off, float maxError2, float maxRadius2, bool allowSelfMatch)
	    {
		    uint cd = GetDim(this.nodes[n].dimChildBucketSize);
            int queryColumnStartIdx = queryIdx * query.RowCount;
		
		    if (cd == this.dimensions)
		    {
			    // this means we have arrived at a leaf node
                int bucketIdx = (int)this.nodes[n].bucketIndex;
			    uint bucketSize = GetChildBucketSize(nodes[n].dimChildBucketSize);
			    for (int i = 0; i < bucketSize; ++i)
			    {
                    int cloudIdx = this.buckets[bucketIdx].index;
                    int cloudColumnStartIdx = cloudIdx * cloud.RowCount;
				    float dist = 0;
                    for (int j = 0; j < this.cloud.RowCount; j++)
                    {
                        float diff = this.cloud.Data[j + cloudColumnStartIdx] - query.Data[j + queryColumnStartIdx];
                        dist += diff * diff;
                    }

				    if ((dist <= maxRadius2) &&
					    /*(dist < heap.MaxPriority) && */ //this check is included in heap.Enqueue
					    (allowSelfMatch || (dist > float.Epsilon))
				    )
                    {
                        heap.Enqueue(this.buckets[bucketIdx], dist);
                    }

				    ++bucketIdx;
			    }

			    return (ulong)(bucketSize);
		    }
		    else
		    {
			    int rightChild = (int)GetChildBucketSize(this.nodes[n].dimChildBucketSize);
			    ulong leafVisitedCount = 0;
			    float old_off = off[(int)cd];
			    float new_off = query.Data[(int)cd + queryColumnStartIdx] - this.nodes[n].cutVal;
			    if (new_off > 0)
			    {
					leafVisitedCount += recurseKnn(query, queryIdx, rightChild, rd, heap, off, maxError2, maxRadius2, allowSelfMatch);
				    rd += - old_off*old_off + new_off*new_off;
				    if ((rd <= maxRadius2) &&
					    (rd * maxError2 < heap.MaxPriority))
				    {
					    off[(int)cd] = new_off;
						leafVisitedCount += recurseKnn(query, queryIdx, n + 1, rd, heap, off, maxError2, maxRadius2, allowSelfMatch);
					    off[(int)cd] = old_off;
				    }
			    }
			    else
			    {
					leafVisitedCount += recurseKnn(query, queryIdx, n+1, rd, heap, off, maxError2, maxRadius2, allowSelfMatch);
				    rd += - old_off*old_off + new_off*new_off;
				    if ((rd <= maxRadius2) &&
					    (rd * maxError2 < heap.MaxPriority))
				    {
					    off[(int)cd] = new_off;
						leafVisitedCount += recurseKnn(query, queryIdx, rightChild, rd, heap, off, maxError2, maxRadius2, allowSelfMatch);
                        off[(int)cd] = old_off;
				    }
			    }

			    return leafVisitedCount;
		    }
	    }
	

        private int BuildNodes(List<int> buildPoints, int first, int last, Vector<float> minValues, Vector<float> maxValues)
	    {
		    int count = last - first;
		    Debug.Assert(count >= 1);
		    int pos = this.nodes.Count;
		
            // if we could fit all points into a single bucket
		    if (count <= (int)this.bucketSize)
		    {
			    int initBucketsSize = buckets.Count;
			    for (int i = 0; i < count; ++i)
			    {
				    int index = buildPoints[first + i];
				    Debug.Assert(index < cloud.ColumnCount);
				    this.buckets.Add(new BucketEntry(index));
			    }

			    nodes.Add(Node.ConstuctLeafNode(CreateDimChildBucketSize(this.dimensions, (uint)count), (uint)initBucketsSize));
			    return pos;
		    }
		    
		    // find the largest dimension of the box
		    int cutDim = (maxValues - minValues).MaximumIndex();
		    float idealCutVal = (maxValues[cutDim] + minValues[cutDim])/2;
		
		    // get bounds from actual points
		    var minMaxVals = getBounds(buildPoints, first, last, cutDim);
		
		    // correct cut following bounds
		    float cutVal;
		    if (idealCutVal < minMaxVals.Item1)
			    cutVal = minMaxVals.Item1;
		    else if (idealCutVal > minMaxVals.Item2)
			    cutVal = minMaxVals.Item2;
		    else
			    cutVal = idealCutVal;
		
		    int l = 0;
		    int r = count-1;
		    // partition points around cutVal
		    while (true)
		    {
			    while (l < count && cloud[cutDim, buildPoints[first+l]] < cutVal)
				    ++l;
			    while (r >= 0 && cloud[cutDim, buildPoints[first+r]] >= cutVal)
				    --r;
			    if (l > r)
				    break;

                Swap(buildPoints, first+l, first+r);
			    ++l; --r;
		    }
		    
            int br1 = l;	// now: points[0..br1-1] < cutVal <= points[br1..count-1]
		    r = count-1;
		    // partition points[br1..count-1] around cutVal
		    while (true)
		    {
			    while (l < count && cloud[cutDim, buildPoints[first+l]] <= cutVal)
				    ++l;
			    while (r >= br1 && cloud[cutDim, buildPoints[first+r]] > cutVal)
				    --r;
			    if (l > r)
				    break;

			    Swap(buildPoints, first+l, first+r);
			    ++l; --r;
		    }
		    
            int br2 = l; // now: points[br1..br2-1] == cutVal < points[br2..count-1]
		
		    // find best split index
		    int leftCount;
		    if (idealCutVal < minMaxVals.Item1)
			    leftCount = 1;
		    else if (idealCutVal > minMaxVals.Item2)
			    leftCount = count-1;
		    else if (br1 > count / 2)
			    leftCount = br1;
		    else if (br2 < count / 2)
			    leftCount = br2;
		    else
			    leftCount = count / 2;
		    Debug.Assert(leftCount > 0);
		    Debug.Assert(leftCount < count);
		
		    // update bounds for left
		    Vector<float> leftMaxValues = maxValues.Clone();
		    leftMaxValues[cutDim] = cutVal;
		    // update bounds for right
		    Vector<float> rightMinValues = minValues.Clone();
		    rightMinValues[cutDim] = cutVal;
		
		    // add this
		    nodes.Add(Node.ConstructSplitNode(0, cutVal));
		
		    // recurse
		    int leftChild = BuildNodes(buildPoints, first, first + leftCount, minValues, leftMaxValues);
		    Debug.Assert(leftChild == pos + 1);
		    int rightChild = BuildNodes(buildPoints, first + leftCount, last, rightMinValues, maxValues);
		
		    // write right child index and return
            var currentNode = nodes[pos];
            currentNode.dimChildBucketSize = CreateDimChildBucketSize((uint)cutDim,(uint)rightChild);
            nodes[pos] = currentNode;
		    return pos;
	    }

        private static void Swap(List<int> buildPoints, int i, int j)
        {
            int t = buildPoints[i];
            buildPoints[i] = buildPoints[j];
            buildPoints[j] = t;
        }

        private static int GetStorageBitCount(uint v)
	    {
		    for (int i = 0; i < 64; ++i)
		    {
			    if (v == 0)
				    return i;
			    v >>= 1;
		    }
		    return 64;
	    }

        private Tuple<float, float> getBounds(List<int> buildPoints, int first, int last, int dim)
	    {
		    float minVal = float.MaxValue;
		    float maxVal = float.MinValue;
		
		    for (int i = first; i < last; i++)
		    {
			    float val = cloud[dim, buildPoints[i]];
			    minVal = Math.Min(val, minVal);
			    maxVal = Math.Max(val, maxVal);
		    }
		
		    return Tuple.Create(minVal, maxVal);
	    }

        private uint CreateDimChildBucketSize(uint dim, uint childIndex)
		{
            return dim | (childIndex << this.dimBitCount);
        }

        uint GetDim(uint dimChildBucketSize)
		{
            return dimChildBucketSize & dimMask;
        }

        //! get the child index or the bucket size out of the compound index
		uint GetChildBucketSize(uint dimChildBucketSize)
		{
            return dimChildBucketSize >> dimBitCount;
        }

        private void checkSizesKnn(
            DenseColumnMajorMatrixStorage<float> query,
            DenseColumnMajorMatrixStorage<int> indices,
            DenseColumnMajorMatrixStorage<float> dists2,
            int k,
            SearchOptionFlags optionFlags,
            Vector<float> maxRadii)
	    {
			if (k > cloud.ColumnCount)
				throw new ArgumentException(string.Format("Requesting more points (%1%) than available in cloud (%2%)", k, cloud.ColumnCount));
		    if (query.RowCount < this.dimensions)
			    throw new ArgumentException(string.Format("Query has less dimensions (%1%) than requested for cloud (%2%)", query.RowCount, this.dimensions));
		    if (indices.RowCount != k)
			    throw new ArgumentException(string.Format("Index matrix has a different number of rows (%1%) than k (%2%)", indices.RowCount, k));
		    if (indices.ColumnCount != query.ColumnCount)
			    throw new ArgumentException(string.Format("Index matrix has a different number of columns (%1%) than query (%2%)", indices.ColumnCount, query.ColumnCount));
		    if (dists2.RowCount != k)
			    throw new ArgumentException(string.Format("Distance matrix has a different number of rows (%1%) than k (%2%)", dists2.RowCount, k));
		    if (dists2.ColumnCount != query.ColumnCount)
			    throw new ArgumentException(string.Format("Distance matrix has a different number of columns (%1%) than query (%2%)", dists2.RowCount, query.ColumnCount));
		    if (maxRadii != null && (maxRadii.Count != query.ColumnCount))
			    throw new ArgumentException(string.Format("Maximum radii vector has not the same length (%1%) than query has columns (%2%)", maxRadii.Count, k));
	    }

        /// <summary>
        /// Search Node
        /// </summary>
        private struct Node
		{
            /// <summary>
            /// cut dimension for split nodes (dimBitCount lsb), index of right node or number of bucket(rest). Note that left index is current+1
            /// </summary>
			public uint dimChildBucketSize;

            /// <summary>
            /// for split node, split value
            /// </summary>
			public float cutVal;

            /// <summary>
            /// for leaf node, pointer to bucket
            /// </summary>
			public uint bucketIndex;
		
            /// <summary>
            /// construct a split node
            /// </summary>
            /// <param name="?"></param>
			public static Node ConstructSplitNode(uint dimChild, float cutVal)
            {
                return new Node
                {
                    dimChildBucketSize = dimChild,
                    cutVal = cutVal,
                };
            }

			/// <summary>
			/// construct a leaf node
			/// </summary>
			/// <param name="?"></param>
            public static Node ConstuctLeafNode(uint bucketSize, uint bucketIndex)
            {
                return new Node
                {
				    dimChildBucketSize = bucketSize,
                    bucketIndex = bucketIndex,
                };
            }
		}

        private struct BucketEntry
		{
            public BucketEntry(int index)
            {
                this.index = index;
            }

            /// <summary>
            /// Index of point
            /// </summary>
			public int index;
		}
    }
}
