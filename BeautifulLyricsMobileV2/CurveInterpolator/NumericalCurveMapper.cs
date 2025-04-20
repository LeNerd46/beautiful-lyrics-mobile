/*
 * 
 * C# implementation of curve-interpolator by kjerandp
 * https://github.com/kjerandp/curve-interpolator/blob/master/src/curve-mappers/numerical-curve-mapper.ts
 * 
 */

namespace BeautifulLyricsMobile.CurveInterpolator
{
	public class NumericalCurveMapper : AbstractCurveMapper
	{
		int nSamples = 21;
		double[][] gauss;

		public NumericalCurveMapper(int nQuadraturePoints = 21, int nInverseSamples = 21, Action onInvalidateCache = null) : base(onInvalidateCache)
		{
			gauss = Gauss.GetGaussianQuadraturePointsAndWeights(nQuadraturePoints);
			nSamples = nInverseSamples;
		}

		protected override void InvalidateCache()
		{
			base.InvalidateCache();
			_cache["arcLengths"] = null;
			_cache["samples"] = null;
		}

		List<double> GetArcLengths()
		{
			if (!_cache.ContainsKey("arcLengths"))
				_cache.Add("arcLengths", ComputeArcLengths());
			else if(_cache["arcLengths"] == null)
				_cache["arcLengths"] = ComputeArcLengths();

			return _cache["arcLengths"] as List<double>;
		}

		List<double> ComputeArcLengths()
		{
			if (Points == null)
				return null;

			List<double> lengths = [];
			lengths.Add(0);

			int nPoints = Closed ? Points.Count : Points.Count - 1;
			double tl = 0;

			for (int i = 0; i < nPoints; i++)
			{
				var length = ComputeArcLength(i);
				tl += length;
				lengths.Add(tl);
			}

			return lengths;
		}

		public double ComputeArcLength(int index, double t0 = 0.0, double t1 = 1.0)
		{
			if (t0 == t1)
				return 0;

			var coefficients = GetCoefficients(index);
			var z = (t1 - t0) * 0.5;

			double sum = 0;
			for (int i = 0; i < gauss.Length; i++)
			{
				var T = gauss[i][0];
				var C = gauss[i][1];
				double t = z * T + z + t0;

				double dtln = CurvyMath.Magnitude(SplineSegment.EvaluateForT((tValue, coeffs) => SplineSegment.DerivativeAtT(tValue, [.. coeffs]), t, coefficients));
				sum += C * dtln;
			}

			return z * sum;
		}

		public Tuple<double[], double[], double[], double[]> GetSamples(int idx)
		{
			if (Points == null)
				return null;

			if (!_cache.ContainsKey("samples"))
				_cache.Add("samples", new Dictionary<double, Tuple<double[], double[], double[], double[]>>());
			else if (_cache["samples"] == null)
				_cache["samples"] = new Dictionary<double, Tuple<double[], double[], double[], double[]>>();

			if (!((Dictionary<double, Tuple<double[], double[], double[], double[]>>)_cache["samples"]).ContainsKey(idx))
			{
				var samples = nSamples;
				List<double> lengths = [];
				List<double> slopes = [];
				var coefficients = GetCoefficients(idx);

				for (int i = 0; i < samples; ++i)
				{
					var ti = i / (samples - 1);
					lengths.Add(ComputeArcLength(idx, 0.0, ti));

					var dtln = CurvyMath.Magnitude(SplineSegment.EvaluateForT((tValue, coeffs) => SplineSegment.DerivativeAtT(tValue, [.. coeffs]), ti, coefficients));
					var slope = dtln == 0 ? 0 : 1 / dtln;

					if (Tension > 0.95)
						slope = Utils.Clamp(slope, -1, 1);

					slopes.Add(slope);
				}

				// Precalculate the cubic interpolant coefficients
				var nCoeff = samples - 1;
				List<double> dis = [];
				List<double> cis = [];
				var liPrev = lengths[0];
				var tdiPrev = slopes[0];
				var step = 1.0 / nCoeff;

				for (int i = 0; i < nCoeff; ++i)
				{
					var li = liPrev;
					liPrev = lengths[i + 1];
					var lDiff = liPrev - li;
					var tdi = tdiPrev;
					var tdiNext = slopes[i + 1];
					tdiPrev = tdiNext;
					var si = step / lDiff;
					var di = (tdi + tdiNext - 2 * si) / (lDiff * lDiff);
					var ci = (3 * si - 2 * tdi - tdiNext) / lDiff;

					dis.Add(di);
					cis.Add(ci);
				}

				((Dictionary<double, Tuple<double[], double[], double[], double[]>>)_cache["samples"])[idx] = new Tuple<double[], double[], double[], double[]>([..lengths], [..slopes], [.. cis], [..dis]);
			}

			return ((Dictionary<double, Tuple<double[], double[], double[], double[]>>)_cache["samples"])[idx];
		}

		public double Inverse(int idx, int len)
		{
			var nCoeff = nSamples - 1;
			var step = 1.0 / nCoeff;

			var (lengths, slopes, cis, dis) = GetSamples(idx);
			var length = lengths[lengths.Length - 1];

			if (len >= length)
				return 1.0;

			if (len <= 0)
				return 0.0;

			// Find the cubic segment which has 'len'
			int i = (int)MathF.Max(0, Utils.BinarySearch(len, lengths.Select(x => (int)x).ToArray()));
			var ti = i * step;

			if (lengths[i] == len)
				return ti;

			var tdi = slopes[i];
			var di = dis[i];
			var ci = cis[i];
			var ld = len - lengths[i];

			return ((di * ld + ci) * ld + tdi) * ld + ti;
		}

		public override double LengthAt(double u)
		{
			var arcLengths = GetArcLengths();
			return u * arcLengths[arcLengths.Count - 1];
		}

		public override double GetT(double u)
		{
			var arcLengths = GetArcLengths();
			var il = arcLengths.Count;
			var targetArcLength = u * arcLengths[il - 1];

			var i = Utils.BinarySearch(targetArcLength, arcLengths.Select(x => (int)x).ToArray());
			var ti = i / (il - 1);

			if (arcLengths[i] == targetArcLength)
				return ti;

			var len = targetArcLength - arcLengths[i];
			var fraction = Inverse(i, (int)len);

			return (i + fraction) / (il - 1);
		}

		public override double GetU(double t)
		{
			if(t == 0) return 0;
			if (t == 1) return 1;

			var arcLengths = GetArcLengths();
			var al = arcLengths.Count - 1;
			var totalLength = arcLengths[al];

			// Need to denormalize t ot find the matching length
			var tIdx = t * al;

			var subIdx = (int)Math.Floor(tIdx);
			var l1 = arcLengths[subIdx];

			if (tIdx == subIdx)
				return l1 / totalLength;

			var t0 = tIdx - subIdx;
			var fraction = ComputeArcLength(subIdx, 0, t0);

			return (l1 + fraction) / totalLength;
		}
	}
}