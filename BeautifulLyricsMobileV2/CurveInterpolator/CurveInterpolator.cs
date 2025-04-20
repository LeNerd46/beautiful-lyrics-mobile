/*
 * 
 * C# implementation of curve-interpolator by kjerandp
 * https://github.com/kjerandp/curve-interpolator/tree/master
 * 
 */

namespace BeautifulLyricsMobile.CurveInterpolator
{
	public class CurveInterpolator
	{
		private double lMargin;
		private ICurveMapper curveMapper;
		private Dictionary<string, object> cache;

		private List<Vector> Points { get => curveMapper.Points; }
		private bool Closed { get => curveMapper.Closed; }

		public CurveInterpolator(Vector[] points, CurveInterpolatorOptions options = null)
		{
			options ??= new CurveInterpolatorOptions
			{
				Tension = 0.5d,
				Alpha = 0d,
				Closed = false,
				NumericalApproximationOrder = 5,
				NumericalInverseSamples = 10
			};

			AbstractCurveMapper curveMapper = options.ArcDivisions != 0 ? new SegmentedCurveMapper(options.ArcDivisions, () => InvalidateCache()) : new NumericalCurveMapper((int)options.NumericalApproximationOrder, (int)options.NumericalInverseSamples, () => InvalidateCache());
			curveMapper.Alpha = options.Alpha;
			curveMapper.Tension = options.Tension;
			curveMapper.Closed = options.Closed;
			curveMapper.Points = [.. points];

			lMargin = options.LMargin == 0 ? 1 - curveMapper.Tension : options.LMargin;
			this.curveMapper = curveMapper;
		}

		private CurveInterpolator InvalidateCache()
		{
			cache = new Dictionary<string, object>();
			return this;
		}

		/// <summary>
		/// Returns the time on curve at a position, given as a value between 0 and 1
		/// </summary>
		/// <param name="position">Position on a curve (0..1)</param>
		/// <param name="clampInput">Whether the input should be clamped to a valid range or not</param>
		/// <returns></returns>
		public double GetTimeFromPosition(double position, bool clampInput = false) => curveMapper.GetT(clampInput ? Utils.Clamp(position) : position);
		/// <summary>
		/// Returns the normalized position u for a normalized time value t
		/// </summary>
		/// <param name="t">Time on curve (0..1)</param>
		/// <param name="clampInput">Whether the input value should be clamped to a valid range or not</param>
		/// <returns></returns>
		public double GetPositionFromTime(double t, bool clampInput = false) => curveMapper.GetU(clampInput ? Utils.Clamp(t) : t);

		/// <summary>
		/// Returns the normalized position u for the specified length
		/// </summary>
		/// <param name="length">Time on a curve (0..1)</param>
		/// <param name="clampInput">Whether the input value should be clamped to a valid range or not</param>
		/// <returns></returns>
		public double GetPositionFromLength(int length, bool clampInput = false)
		{
			var l = clampInput ? Utils.Clamp(length, 0, length) : length;
			return curveMapper.GetU(l / length);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="position">Position on curve (0..1)</param>
		/// <param name="clampInput"></param>
		/// <returns>Length from start to position</returns>
		public double GetLengthAt(int position = 1, bool clampInput = false) => curveMapper.LengthAt(clampInput ? Utils.Clamp(position) : position);

		/// <summary>
		/// Returns the time (t) of the knot at the specified index
		/// </summary>
		/// <param name="index">Index of the knot (control/input point)</param>
		/// <returns>Time (t)</returns>
		/// <exception cref="Exception"></exception>
		public double GetTimeAtKnot(int index)
		{
			if (index < 0 || index > Points.Count - 1) throw new Exception("Invalid index!");
			if (index == 0) return 0; // First knot
			if (!Closed && index == Points.Count - 1) return 1; // Last knot

			var nCount = Closed ? Points.Count : Points.Count - 1;

			return index / nCount;
		}

		/// <summary>
		/// Interpolate a point at the given position
		/// </summary>
		/// <param name="position">Position on curve (0..1)</param>
		/// <param name="target">Optional target</param>
		/// <returns></returns>
		public Vector GetPointAt(double position, Vector target = null) => GetPointAtTIme(GetTimeFromPosition(position), target);

		/// <summary>
		/// <para>Find points on the curve intersecting a specific value along a given axis. The axis is given as an index from 0 - n, i.e. 0 = x-axis, 1 = y-axis, 2 = z-axis etc.</para>
		/// <para>The max parameter is used to specify the maxium number of solutions you want returned, where max=0 return all solutons and a negative number will reutrn the max number of solutions start from the end of the curve and a positive number starting from the beginning of the curve. Note that if max = 1 or -1, this function returns the point (unwrapped) or null if no intersects exist. In any other case an array will be returned, regardless of if there's multiple, a single, or no solution</para>
		/// </summary>
		/// <param name="v">Lookup value</param>
		/// <param name="axis">Index of axis [0=x, 1=y, 2=z ...]</param>
		/// <param name="max">Max solutions (i.e. 0=all, 1=first along curve, -1=last along curve)</param>
		/// <param name="margin"></param>
		/// <returns></returns>
		public Vector[] GetIntersects(double v, int axis = 0, int max = 0, double margin = -3284324)
		{
			if (margin == -3284324) margin = lMargin;

			var solutions = GetIntersectsAsTime(v, axis, max, margin).Select(t => GetPointAtTIme(t)).ToArray();
			return Math.Abs(max) == 1 ? (solutions.Length == 1 ? [solutions[0]] : null) : solutions;
		}

		/// <summary>
		/// Find positionso (0-1) on the curve intersected by the gien value along a given axis
		/// </summary>
		/// <param name="v">Lookup value</param>
		/// <param name="axis">Index of axis [0=x, 1=y, 2=z ...]</param>
		/// <param name="max">Max solutions (i.e. 0=all, 1=first along curve, -1=last along curve)</param>
		/// <param name="margin"></param>
		/// <returns></returns>
		public double[] GetIntersectsAsTime(double v, int axis = 0, int max = 0, double margin = -3284324)
		{
			if (margin == -3284324) margin = lMargin;

			int k = axis;
			var solutions = new HashSet<double>();
			var nPoints = Closed ? Points.Count : Points.Count - 1;

			for (int i = 0; i < nPoints && (max == 0 || solutions.Count < Math.Abs(max)); i += 1)
			{
				var idx = (max < 0 ? nPoints - (i + 1) : i);

				(Vector p0, Vector p1, Vector p2, Vector p3) = SplineCurve.GetControlPoints(idx, Points, Closed);
				var coefficients = curveMapper.GetCoefficients(idx);

				double vmin;
				double vmax;

				if (p1.Numbers[k] < p2.Numbers[k])
				{
					vmin = p1.Numbers[k];
					vmax = p2.Numbers[k];
				}
				else
				{
					vmin = p2.Numbers[k];
					vmax = p1.Numbers[k];
				}

				if(v - margin <= vmax && v + margin >= vmin)
				{
					var ts = SplineSegment.FindRootsOfT(v, coefficients[k]);

					if (max < 0)
						ts.ToList().Sort((a, b) => b.CompareTo(a));
					else if (max >= 0)
						ts.ToList().Sort((a, b) => a.CompareTo(b));

					for (int j = 0; j < ts.Length; j++)
					{
						var nt = (ts[j] + idx) / nPoints; // Normalize t
						solutions.Add(nt);

						if (max != 0 && solutions.Count == Math.Abs(max))
							break;
					}
				}
			}

			return [.. solutions];
		}

		public Vector GetPointAtTIme(double t, Vector target = null)
		{
			t = Utils.Clamp(t);

			if (t == 0)
				return Utils.CopyValues(Points[0], target);
			else if (t == 1)
				return Utils.CopyValues(Closed ? Points[0] : Points[^1], target);

			return curveMapper.EvaluateForT(SplineSegment.ValueAtT, t, target);
		}
	}

	public delegate double SegmentFunction(double t, double[] coefficients);

	public interface ICurveMapper
	{
		public double Alpha { get; set; }
		public double Tension { get; set; }
		public List<Vector> Points { get; set; }
		public bool Closed { get; set; }

		Vector EvaluateForT(SegmentFunction func, double t, Vector? target = null);
		double LengthAt(double u);
		double GetT(double u);
		double GetU(double t);
		double[][] GetCoefficients(int idx);
		void Reset();
	}

	public class Vector
	{
		public List<double> Numbers { get; set; }
		public IVectorType VectorType { get; set; }
	}

	public interface IVectorType
	{
		public double Zero { get; set; }
		public double One { get; set; }
		public double Two { get; set; }
		public double Three { get; set; }
		public double X { get; set; }
		public double Y { get; set; }
		public double Z { get; set; }
		public double W { get; set; }
		public double Length { get; set; }
	}

	public class CurveInterpolatorOptions : SplineCurveOptions
	{
		public double ArcDivisions { get; set; }
		public double NumericalApproximationOrder { get; set; }
		public double NumericalInverseSamples { get; set; }
		public double LMargin { get; set; }
	}

	public class SplineCurveOptions : CurveParameters
	{
		public bool Closed { get; set; }
	}

	public class CurveParameters
	{
		public double Tension { get; set; }
		public double Alpha { get; set; }
	}
}
