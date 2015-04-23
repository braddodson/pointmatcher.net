using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace pointmatcher.net
{
    public class DefaultTransformationCheckerFactory : ITransformationCheckerFactory
    {
        public ITransformationChecker CreateTransformationChecker()
        {
            var checkers = new List<ITransformationChecker>();
            checkers.Add(new CounterTransformationChecker());
            checkers.Add(new DifferentialTransformationChecker());
            return new CompositeTransformationChecker(checkers);
        }
    }

    public class CompositeTransformationChecker : ITransformationChecker
    {
        private IEnumerable<ITransformationChecker> checkers;

        public CompositeTransformationChecker(IEnumerable<ITransformationChecker> checkers)
        {
            this.checkers = checkers;
        }

        public bool ShouldContinue(EuclideanTransform transform)
        {
            return checkers.All(c => c.ShouldContinue(transform));
        }
    }

    public class DifferentialTransformationChecker : ITransformationChecker
    {
        private List<EuclideanTransform> transforms = new List<EuclideanTransform>();
        private int smoothLength = 3;
        private float minDiffRotErr = 0.001f;
        private float minDiffTransErr = 1.0f; //0.001f;

        // TODO: make params settable by constructor?

        public bool ShouldContinue(EuclideanTransform transform)
        {
            transforms.Add(transform);
	
            float rotErr = 0, transErr = 0;

	        if(this.transforms.Count > smoothLength)
	        {
		        for(int i = transforms.Count-1; i >= transforms.Count-smoothLength; i--)
		        {
                    //Compute the mean derivative
                    rotErr += Math.Abs(VectorHelpers.AngularDistance(transforms[i].rotation, transforms[i-1].rotation));
                    transErr += (transforms[i].translation - transforms[i-1].translation).Length();
		        }

		        if(rotErr / smoothLength < this.minDiffRotErr && transErr / smoothLength < this.minDiffTransErr)
			        return false;
	        }
	
	        if (float.IsNaN(rotErr))
		        throw new ArithmeticException("abs rotation norm not a number");
	        if (float.IsNaN(transErr))
                throw new ArithmeticException("abs translation norm not a number");

            return true;
        }
    }

    public class CounterTransformationChecker : ITransformationChecker
    {
        private int iterationCount = 0;
        private int maxIterationCount;

        public CounterTransformationChecker(int maxIterationCount = 100)
        {
            this.maxIterationCount = maxIterationCount;
        }

        public bool ShouldContinue(EuclideanTransform transform)
        {
            iterationCount++;
            return (iterationCount <= maxIterationCount);
        }
    }
}
