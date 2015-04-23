using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MathNet.Numerics.LinearAlgebra.Generic;
using MathNet.Numerics.LinearAlgebra.Single;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra.Storage;
using knearest;

namespace knearestTest
{
    [TestClass]
    public class KnnTest
    {
        [TestMethod]
        public void Knn_RandomTest()
        {
            Matrix<float> queryMatrix = DenseMatrix.CreateRandom(3, 100, new ContinuousUniform(-10, 10));
            DenseColumnMajorMatrixStorage<float> query = (DenseColumnMajorMatrixStorage<float>)queryMatrix.Storage;
            Matrix<float> points = DenseMatrix.CreateRandom(3, 100, new ContinuousUniform(-10, 10));
            Vector<float> maxRadii = DenseVector.Create(100, i => float.PositiveInfinity);


            var search = new KdTreeNearestNeighborSearch((DenseColumnMajorMatrixStorage<float>)points.Storage);
            var results = DenseColumnMajorMatrixStorage<int>.OfInit(1, 100, (i,j) => 0);
            var resultDistances = DenseColumnMajorMatrixStorage<float>.OfInit(1, 100, (i, j) => 0);
            search.knn(query, results, resultDistances, maxRadii, k: 1, epsilon: float.Epsilon, optionFlags: SearchOptionFlags.AllowSelfMatch);

            var bruteForceSearch = new BruteForceNearestNeighbor(points);
            var results2 = DenseColumnMajorMatrixStorage<int>.OfInit(1, 100, (i, j) => 0);
            var resultDistances2 = DenseColumnMajorMatrixStorage<float>.OfInit(1, 100, (i, j) => 0);
            search.knn(query, results2, resultDistances2, maxRadii, k: 1, epsilon: float.Epsilon, optionFlags: SearchOptionFlags.AllowSelfMatch);

            for (int i = 0; i < results.ColumnCount; i++)
            {
                for (int j = 0; j < results.RowCount; j++)
                {
                    Assert.AreEqual(results2.At(j, i), results.At(j, i));
                }
            }
        }
    }
}
