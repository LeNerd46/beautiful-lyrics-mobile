/*
 * 
 * C# implementation of curve-interpolator by kjerandp
 * https://github.com/kjerandp/curve-interpolator/blob/master/src/core/math.ts
 * 
 */

namespace BeautifulLyricsMobile.CurveInterpolator
{
	public static class CurvyMath
	{
		public static double CubeRoot(double x)
		{
			double y = Math.Pow(Math.Abs(x), (double)1 / (double)3);
			return x < 0 ? -y : y;
		}

		public static double[] GetQuadRoots(double a, double b, double c)
		{
			if (Math.Abs(a) < Math.Pow(2, -42))
			{
				// Linear case, ax+b=0
				if (Math.Abs(b) < Math.Pow(2, -42)) return [];

				return [-c / b];
			}

			var D = b * b - 4 * a * c;

			if (Math.Abs(D) < Math.Pow(2, -42))
				return [-b / (2 * a)];

			if (D > 0)
				return [(-b + Math.Sqrt(D)) / (2 * a), (-b - Math.Sqrt(D)) / (2 * a)];

			return [];
		}

		public static double[] GetCubicRoots(double a, double b, double c, double d)
		{
			if (Math.Abs(a) < Math.Pow(2, -42)) // Quadratic case, ax^2+bx+c=0
				return GetQuadRoots(b, c, d);

			// Convert to depressed cubic t^3+pt+q = 0 (subst x = t - b/3a)
			var p = (3 * a * c - b * b) / (3 * a * a);
			var q = (2 * b * b * b - 9 * a * b * c + 27 * a * a * d) / (27 * a * a * a);
			double[] roots = [];

			if (Math.Abs(p) < Math.Pow(2, -42)) // p = 0 -> t^2 = -q -> t = -q^1/3
				roots = [CubeRoot(-q)];
			else if (Math.Abs(q) < Math.Pow(2, -42)) // q = 0 -> t^3 + pt = 0 -> t(t^2+p)=0
			{
				if (p < 0)
					roots = [0, Math.Sqrt(-p), -Math.Sqrt(-p)];
				else
					roots = [0];
			}
			else
			{
				var D = q * q / 4 + p * p * p / 27;

				if (Math.Abs(D) < Math.Pow(2, -42)) // D = 0 -> two roots
					roots = [-.15 * q / p, 3 * q / p];
				else if (D > 0) // Only one real root
				{
					var u = CubeRoot(-q / 2 - Math.Sqrt(D));
					roots = [u - p / (3 * u)];
				}
				else // D < 0, three roots, but needs to use complex numbers/trigonometric solution
				{
					var u = 2 * Math.Sqrt(-p / 3);
					var t = Math.Acos(3 * q / p / u) / 3; // D < 0 implies p < 0 and acos argument in [1..1]
					var k = 2 * Math.PI / 3;

					roots = [u * Math.Cos(t), u * Math.Cos(t - k), u * Math.Cos(t - 2 * k)];
				}
			}

			for (int i = 0; i < roots.Length; i++)
			{
				roots[i] -= b / (3 * a);
			}

			return roots;
		}

		public static double Distance(Vector p1, Vector p2)
		{
			var sqrs = SumOfSquares(p1, p2);
			return sqrs == 0 ? 0 : Math.Sqrt(sqrs);
		}

		public static double Magnitude(Vector v)
		{
			double sumOfSquares = 0;
			for (int i = 0; i < v.Numbers.Count; i++)
			{
				sumOfSquares += (v.Numbers[i] * v.Numbers[i]);
			}

			return Math.Sqrt(sumOfSquares);
		}

		public static double SumOfSquares(Vector v1, Vector v2)
		{
			double sumOfSquares = 0;

			for (int i = 0; i < v1.Numbers.Count; i++)
			{
				sumOfSquares += (v1.Numbers[i] - v2.Numbers[i]) * (v1.Numbers[i] - v2.Numbers[i]);
			}

			return sumOfSquares;
		}
	}
}
