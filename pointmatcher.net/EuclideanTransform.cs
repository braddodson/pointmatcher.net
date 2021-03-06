﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace pointmatcher.net
{   
    public struct EuclideanTransform
    {
        public Quaternion rotation;
        public Vector3 translation;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 Apply(Vector3 v)
        {
            return Vector3.Transform(v, this.rotation) + this.translation;
        }

        public EuclideanTransform Inverse()
        {
            EuclideanTransform result;
            // the rotation is the opposite of the applied rotation
            result.rotation = Quaternion.Conjugate(this.rotation);
            result.translation = Vector3.Transform(-1 * this.translation, result.rotation);
            return result;
        }

        /// p2 = r * p + t
        /// p = (p2 - t0) * r^-1
        /// p = r^-1 * p2 - t * r^-1

        /// <summary>
        /// Computes a transform that represents applying e2 then e1
        /// </summary>
        public static EuclideanTransform operator *(EuclideanTransform e1, EuclideanTransform e2)
        {
            EuclideanTransform result;
            result.rotation = e1.rotation * e2.rotation;
            result.translation = Vector3.Transform(e2.translation, e1.rotation) + e1.translation;
            return result;
        }

        public static EuclideanTransform Identity
        {
            get
            {
                return new EuclideanTransform
                {
                    translation = new Vector3(),
                    rotation = Quaternion.Identity
                };
            }
        }
    }
}
