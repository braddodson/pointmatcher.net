using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pointmatcher.net
{
    public static class KnownCorrespondenceErrorMinimizer
    {
        public static EuclideanTransform IterativeSolveForTransform(ErrorElements errorElements, IErrorMinimizer minimizer)
        {
            var match = new ErrorElements();
            match.reference = errorElements.reference;
            match.reading = errorElements.reading;
            match.weights = errorElements.weights;

            var t = EuclideanTransform.Identity;
            for (int i = 0; i < 100; i++)
            {
                t = minimizer.SolveForTransform(match) * t;
                match.reading = ICP.ApplyTransformation(errorElements.reading, t);
            }

            return t;
        }
    }
}
