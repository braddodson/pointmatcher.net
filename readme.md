Pointmatcher.net
================

pointmatcher.net is a modular Iterative Closest Point library for .net inspired by
[libpointmatcher](https://github.com/ethz-asl/libpointmatcher) and [libnabo](https://github.com/ethz-asl/libnabo)

pointmatcher.net can take a point cloud and iteratively find a Euclidean transformation that will align it with a reference point cloud.

Usage
-----
To use the library in your .net app, simply Add a Reference to the dll and use something like this

```
DataPoints reading = ...; // initialize your point cloud reading here
DataPoints reference = ...; // initialize your reference point cloud here
EuclideanTransform initialTransform = ...; // your initial guess at the transform from reading to reference
ICP icp = new ICP();
icp.ReadingDataPointsFilters = new RandomSamplingDataPointsFilter(prob: 0.1f);
icp.ReferenceDataPointsFilters = new SamplingSurfaceNormalDataPointsFilter(SamplingMethod.RandomSampling, ratio: 0.2f);
icp.OutlierFilter = new TrimmedDistOutlierFilter(ratio: 0.5f);
var transform = icp.Compute(reading, reference, initialTransform);
```

Limitations
-----------
pointmatcher.net currently only has a minimal set of the filters available in libpointmatcher. It has also been less extensively tested and optimized.

Dependencies
------------
pointmatcher.net depends on:

 * [Math.net numerics](https://github.com/mathnet/mathnet-numerics) - A matrix library for .net

 * [System.Numerics.Vectors](http://www.nuget.org/packages/System.Numerics.Vectors) - A .net api for SIMD instructions