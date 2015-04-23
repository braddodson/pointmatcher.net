using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra.Generic;
using MathNet.Numerics.LinearAlgebra.Storage;

namespace knearest
{
    public class BruteForceNearestNeighbor : INearestNeighborSearch
    {
        private Matrix<float> cloud;

        public BruteForceNearestNeighbor(Matrix<float> cloud)
        {
            this.cloud = cloud;
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
            for (int i = 0; i < query.ColumnCount; i++)
            {
                onePointKnn(query, i, indices, dists2, maxRadii, k, epsilon, optionFlags);
            }

            return 0;
        }

        private ulong onePointKnn(
            DenseColumnMajorMatrixStorage<float> query,
            int i,
            DenseColumnMajorMatrixStorage<int> indices,
            DenseColumnMajorMatrixStorage<float> dists2,
            Vector<float> maxRadii,
            int k,
            float epsilon,
            SearchOptionFlags optionFlags)
        {
            var results = new ListPriorityQueue<int>(maxSize: k);

            var queryMatrix = new MathNet.Numerics.LinearAlgebra.Single.DenseMatrix(query);
            var q = queryMatrix.Column(i);

            for (int j = 0; j < cloud.ColumnCount; j++)
            {
                var c = cloud.Column(j);

                var diff = (c - q);
                float l2 = diff * diff;

                results.Enqueue(j, l2);
            }

            int kIdx = 0;
            foreach (var j in results.Items)
            {
                indices.At(kIdx, i, j);
                kIdx++;
            }

            kIdx = 0;
            foreach (var d2 in results.Priorities)
            {
                dists2.At(kIdx, i, d2);
            }

            return 0;
        }
    }
}
