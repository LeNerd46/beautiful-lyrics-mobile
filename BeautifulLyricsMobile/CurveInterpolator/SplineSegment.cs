/*
 * 
 * C# implementation of curve-interpolator by kjerandp
 * https://github.com/kjerandp/curve-interpolator/blob/f7e32e0616d8eefa361ec3ce339a00054c2ce4c1/src/core/spline-segment.ts
 * https://github.com/kjerandp/curve-interpolator/blob/f7e32e0616d8eefa361ec3ce339a00054c2ce4c1/src/core/spline-curve.ts
 * 
 */

namespace BeautifulLyricsMobile.CurveInterpolator
{
	public static class SplineSegment
	{
		public static double[] CalcKnotSequence(Vector p0, Vector p1, Vector p2, Vector p3, double? alpha = 0)
		{
			if (alpha == null || alpha == 0)
				return [0, 1, 2, 3];

			double deltaT(Vector u, Vector v) => Math.Pow(CurvyMath.SumOfSquares(u, v), 0.5 * (double)alpha);

			double t1 = deltaT(p1, p0);
			double t2 = deltaT(p2, p1) + t1;
			double t3 = deltaT(p3, p2) + t1;

			return [0, t1, t2, t3];
		}

		public static double DerivativeAtT(double t, List<double> coefficients)
		{
			double t2 = t * t;
			double a = coefficients[0];
			double b = coefficients[1];
			double c = coefficients[2];

			return 3 * a * t2 + 2 * b * t + c;
		}

		public static double[][] CalculateCoefficients(Vector p0, Vector p1, Vector p2, Vector p3, CurveParameters options)
		{
			double tension = double.IsFinite(options.Tension) ? options.Tension : 0.5;
			double? alpha = double.IsFinite(options.Alpha) ? options.Alpha : null;
			double[] knotSequence = alpha > 0 ? CalcKnotSequence(p0, p1, p2, p3, alpha) : null;
			double[][] coefficientList = new double[p0.Numbers.Count][];

			for (int k = 0; k < p0.Numbers.Count; k++)
			{
				double u = 0;
				double v = 0;

				double v0 = p0.Numbers[k];
				double v1 = p1.Numbers[k];
				double v2 = p2.Numbers[k];
				double v3 = p3.Numbers[k];

				if (knotSequence == null)
				{
					u = (1 - tension) * (v2 - v0) * 0.5;
					v = (1 - tension) * (v3 - v1) * 0.5;
				}
				else
				{
					var (t0, t1, t2, t3) = (knotSequence[0], knotSequence[1], knotSequence[2], knotSequence[3]);
					if (t1 - t2 != 0)
					{
						if (t0 - t1 != 0 && t0 - t2 != 0)
							u = (1 - tension) * (t2 - t1) * ((v0 - v1) / (t0 - t1) - (v0 - v2) / (t0 - t2) + (v1 - v2) / (t1 - t2));

						if (t1 - t3 != 0 && t2 - t3 != 0)
							v = (1 - tension) * (t2 - t1) * ((v1 - v2) / (t1 - t2) - (v1 - v3) / (t1 - t3) + (v2 - v3) / (t2 - t3));
					}
				}

				double a = 2 * v1 - 2 * v2 + u + v;
				double b = -3 * v1 + 3 * v2 - 2 * u - v;
				double c = u;
				double d = v1;

				coefficientList[k] = [a, b, c, d];
			}

			return coefficientList;
		}

		public static double ValueAtT(double t, double[] coefficients)
		{
			var t2 = t * t;
			var t3 = t * t2;
			double a = coefficients[0];
			double b = coefficients[1];
			double c = coefficients[2];
			double d = coefficients[3];
			return a * t3 + b * t2 + c * t + d;
		}

		public static double[] FindRootsOfT(double lookup, double[] coefficients)
		{
			double a = coefficients[0];
			double b = coefficients[1];
			double c = coefficients[2];
			double d = coefficients[3];
			var x = d - lookup;

			if(a == 0 && b == 0 && c == 0 && x == 0)
				return [0]; // Whole segment matches - how to deal with this? My note: What does that mean?

			var roots = CurvyMath.GetCubicRoots(a, b, c, x);
			return roots.Where(t => t > -Math.Pow(2, -42) && t <= t + Math.Pow(2, -42)).Select(t => Utils.Clamp(t)).ToArray();
		}

		public static Vector EvaluateForT(SegmentFunction func, double t, double[][] coefficients, Vector target = null)
		{
			if (target == null)
			{
				List<double> numbers = new List<double>(coefficients.Length);

				for (int i = 0; i < coefficients.Length; i++)
				{
					numbers.Add(0);
				}

				target = new Vector
				{
					Numbers = numbers
				};
			}

			for (int k = 0; k < coefficients.Length; k++)
			{
				target.Numbers[k] = func(t, coefficients[k]);
			}

			return target;
		}
	}

	public static class SplineCurve
	{
		public static Vector ExtrapolateControlPoint(Vector u, Vector v)
		{
			double[] e = new double[u.Numbers.Count];

			for (int i = 0; i < u.Numbers.Count; i++)
			{
				e[i] = 2 * u.Numbers[i] - v.Numbers[i];
			}

			return new Vector
			{
				Numbers = [.. e]
			};
		}

		public static (Vector p0, Vector p1, Vector p2, Vector p3) GetControlPoints(int idx, List<Vector> points, bool closed)
		{
			int maxIndex = points.Count - 1;

			Vector p0, p1, p2, p3;

			if (closed)
			{
				p0 = points[idx - 1 < 0 ? maxIndex : idx - 1];
				p1 = points[idx % points.Count];
				p2 = points[(idx + 1) % points.Count];
				p3 = points[(idx + 2) % points.Count];
			}
			else
			{
				if (idx == maxIndex)
					throw new Exception("There is no spline segment at this index for a closed curve!");

				p1 = points[idx];
				p2 = points[idx + 1];

				p0 = idx > 0 ? points[idx - 1] : ExtrapolateControlPoint(p1, p2);
				p3 = idx < maxIndex - 1 ? points[idx + 2] : ExtrapolateControlPoint(p2, p1);
			}

			return (p0, p1, p2, p3);
		}

		public static (int index, double weight) GetSegmentIndexAndT(double ct, List<Vector> points, bool closed = false)
		{
			int nPoints = closed ? points.Count : points.Count - 1;

			if (ct == 1.0)
				return (nPoints - 1, 1.0);

			var p = nPoints * ct;
			var index = (int)Math.Floor(p);
			var weight = p - index;

			return (index, weight);
		}
	}
}