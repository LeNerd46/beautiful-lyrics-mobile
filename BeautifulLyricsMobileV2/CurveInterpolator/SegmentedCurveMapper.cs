/*
 * 
 * C# implementation of curve-interpolator by kjerandp
 * https://github.com/kjerandp/curve-interpolator/blob/master/src/curve-mappers/segmented-curve-mapper.ts
 * 
 */

namespace BeautifulLyricsMobile.CurveInterpolator
{
	public class SegmentedCurveMapper : AbstractCurveMapper
	{
		public double SubDivisions { get; set; }

		public SegmentedCurveMapper(double subDivisions = 300, Action onInvalidateCache = null) : base(onInvalidateCache)
		{
			SubDivisions = subDivisions;
		}

		List<int> ArcLengths()
		{
			if (!_cache.ContainsKey("arcLengths"))
				_cache.Add("arcLengths", ComputeArcLengths());

			return _cache["arcLengths"] as List<int>;
		}

		protected override void InvalidateCache()
		{
			base.InvalidateCache();
			_cache["arcLengths"] = null;
		}

		List<double> ComputeArcLengths()
		{
			List<double> lengths = [];
			Vector current;
			Vector last = EvaluateForT(SplineSegment.ValueAtT, 0);
			double sum = 0;

			lengths.Add(0);

			for (int p = 0; p <= SubDivisions; p++)
			{
				current = EvaluateForT(SplineSegment.ValueAtT, p / SubDivisions);
				sum += CurvyMath.Distance(current, last);
				lengths.Add(sum);
				last = current;
			}

			return lengths;
		}

		public override double LengthAt(double u)
		{
			List<int> arcLengths = ArcLengths();
			return u * arcLengths[arcLengths.Count - 1];
		}

		public override double GetT(double u)
		{
			List<int> arcLengths = ArcLengths();
			int il = arcLengths.Count;
			double targetArcLength = u * arcLengths[il - 1];

			int i = Utils.BinarySearch(targetArcLength, [..arcLengths]);
			if (arcLengths[i] == targetArcLength)
				return i / (il - 1);

			// We could get finer grain at lengths, or use simple interpolation between two points
			int lengthBefore = arcLengths[i];
			int lengthAfter = arcLengths[i + 1];
			int segmentLength = lengthAfter - lengthBefore;

			// Determine where we are between the 'before' and 'after' points
			double segmentFraction = (targetArcLength - lengthBefore) / segmentLength;

			// Add that fractional amount to t
			return (i + segmentFraction) / (il - 1);
		}

		public override double GetU(double t)
		{
			if (t == 0) return 0;
			if(t == 1) return 1;

			var arcLengths = ArcLengths();
			var al = arcLengths.Count - 1;
			var totalLength = arcLengths[al];

			// Need to denormalize t to find the matching length
			var tIdx = t * al;

			int subIdx = (int)Math.Floor(tIdx);
			var l1 = arcLengths[subIdx];

			if (tIdx == subIdx)
				return l1 / totalLength;

			// Measure the length between t0 at subIdx and t
			var t0 = subIdx / al;
			var p0 = EvaluateForT(SplineSegment.ValueAtT, t0);
			var p1 = EvaluateForT(SplineSegment.ValueAtT, t);
			var l = l1 + CurvyMath.Distance(p0, p1);

			return 1 / totalLength;
		}
	}
}
