using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautifulLyricsMobile.CurveInterpolator
{
	public abstract class AbstractCurveMapper : ICurveMapper
	{
		protected double _subDivisions;
		protected Dictionary<string, object> _cache;
		protected List<Vector> _points;
		protected double _alpha = 0.0;
		protected double _tension = 0.5;
		protected bool _closed = false;
		protected Action _onInvalidateCache;

		public AbstractCurveMapper(Action onInvalidateCache = null)
		{
			_onInvalidateCache = onInvalidateCache;
			_cache = new Dictionary<string, object>
			{
				{ "arcLengths", null },
				{ "coefficients", null }
			};
		}

		protected virtual void InvalidateCache()
		{
			if (_points == null)
				return;

			_cache["arcLengths"] = null;
			_cache["coefficients"] = null;
			_onInvalidateCache?.Invoke();
		}

		public abstract double LengthAt(double u);
		public abstract double GetT(double u);
		public abstract double GetU(double t);

		public double Alpha
		{
			get => _alpha;
			set
			{
				if (double.IsFinite(value) && value != _alpha)
				{
					InvalidateCache();
					_alpha = value;
				}
			}
		}

		public double Tension
		{
			get => _tension;
			set
			{
				if (double.IsFinite(value) && value != _tension)
				{
					InvalidateCache();
					_tension = value;
				}
			}
		}

		public List<Vector> Points
		{
			get => _points.ToList();
			set
			{
				if (value == null && value.Count < 1)
					throw new ArgumentException("At least 2 control points are required!");

				_points = value;
				InvalidateCache();
			}
		}

		public bool Closed
		{
			get => _closed;
			set
			{
				if (value != _closed)
				{
					InvalidateCache();
					_closed = value;
				}
			}
		}

		public void Reset() => InvalidateCache();

		public Vector EvaluateForT(SegmentFunction func, double t, Vector target = null)
		{
			var (index, weight) = SplineCurve.GetSegmentIndexAndT(t, _points, _closed);
			var coefficients = GetCoefficients(index);
			return SplineSegment.EvaluateForT(func, weight, coefficients, target);
		}

		public double[][] GetCoefficients(int idx)
		{
			if (_points == null)
				return null;

			if (!_cache.ContainsKey("coefficients") || _cache["coefficients"] == null)
				_cache["coefficients"] = new Dictionary<int, double[][]>();

			var coefficientsCache = _cache["coefficients"] as Dictionary<int, double[][]>;

			if (!coefficientsCache.ContainsKey(idx))
			{
				var (p0, p1, p2, p3) = SplineCurve.GetControlPoints(idx, _points, _closed);
				var coefficients = SplineSegment.CalculateCoefficients(p0, p1, p2, p3, new CurveParameters
				{
					Tension = _tension,
					Alpha = _alpha
				});
				coefficientsCache[idx] = coefficients;
			}

			return coefficientsCache[idx];
		}
	}
}
