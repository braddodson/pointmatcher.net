using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra.Generic;
using MathNet.Numerics.LinearAlgebra.Storage;

namespace knearest
{
    public enum SearchType
    {
        /// <summary>
        /// brute force, check distance to every point in the data
        /// </summary>
        BruteForce = 0,
        /// <summary>
        /// kd-tree with linear heap, good for small k (~up to 30)
        /// </summary>
        KdTreeLinearHeap,
        /// <summary>
        /// kd-tree with tree heap, good for large k (~from 30)
        /// </summary>
        KdTreeTreeHeap,
    }

    [Flags]
    public enum SearchOptionFlags
    {
        /// <summary>
        /// allows the return of the same point as the query, if this point is in the data cloud; forbidden by default
        /// </summary>
        AllowSelfMatch = 1,

        /*
        /// <summary>
        /// sort points by distances, when k > 1; do not sort by default
        /// </summary>
        SortResults = 2,*/
    }

    public interface INearestNeighborSearch
    {
        /// <summary>
        /// Finds the k nearest neighbors to the query points (represented as columns of the query matrix)
        /// </summary>
        /// <param name="query">The search points, represented as columns of the matrix</param>
        /// <param name="indices">
        /// Receives the indices of the k nearest reference points for each search point.
        /// Must have the same number of columns as the query matrix, and k rows.
        /// </param>
        /// <param name="dists2">
        /// Receives the squared distances to the k nearest reference points. Must have the same dimensions as indices.
        /// </param>
        /// <param name="maxRadii">The maximum distance to search for each query point</param>
        /// <param name="k">The number of matches to find for each query point</param>
        /// <param name="epsilon">The maximum allowable error. The search might miss a point which is closer by less than epsilon.</param>
        /// <param name="optionFlags">Specifies options (whether to allow matching identical points)</param>
        /// <returns>The number of leaf nodes visited in the search process</returns>
        ulong knn(
            DenseColumnMajorMatrixStorage<float> query,
            DenseColumnMajorMatrixStorage<int> indices,
            DenseColumnMajorMatrixStorage<float> dists2,
            Vector<float> maxRadii,
            int k,
            float epsilon,
            SearchOptionFlags optionFlags);
    }
}
