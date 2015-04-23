using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace pointmatcher.net
{
    public static class VectorHelpers
    {
        public static Vector3 Mean(DataPoint[] points)
        {
            return Sum(points) / points.Length;
        }

        public static Vector3 Sum(IEnumerable<DataPoint> points)
        {
            return Sum(points.Select(p => p.point));
        }

        public static Vector3 Sum(IEnumerable<Vector3> vectors)
        {
            var sum = Vector3.Zero;
            foreach (var v in vectors)
            {
                sum += v;
            }

            return sum;
        }

        public static Vector3 ToVector3(MathNet.Numerics.LinearAlgebra.Generic.Vector<float> v)
        {
            return new Vector3(v[0], v[1], v[2]);
        }

        public static MathNet.Numerics.LinearAlgebra.Generic.Vector<float> ToVector(Vector3 v)
        {
            return new MathNet.Numerics.LinearAlgebra.Single.DenseVector(new[] { v.X, v.Y, v.Z });
        }

        public static float AngularDistance(Quaternion q1, Quaternion q2)
        {
            float dot = Quaternion.Dot(q1, q2);
            dot = Math.Min(dot, 1);
            dot = Math.Max(dot, -1);

            return (float)(2 * Math.Acos(dot));
        }

        public static float AverageSqDistance(DataPoints points, DataPoints points2)
        {
            float sum = 0;
            for (int i = 0; i < points.points.Length; i++)
            {
                sum += (points.points[i].point - points2.points[i].point).LengthSquared();
            }

            return sum / points.points.Length;
        }
    }
}
