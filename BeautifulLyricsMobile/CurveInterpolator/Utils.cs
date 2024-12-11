/*
 * 
 * C# implementation of curve-interpolator by kjerandp
 * https://github.com/kjerandp/curve-interpolator/blob/master/src/core/utils.ts
 * 
 */

namespace BeautifulLyricsMobile.CurveInterpolator
{
	public static class Utils
	{
		public static Vector Fill(Vector v, double val)
		{
			for (int i = 0; i < v.Numbers.Count; i++)
			{
				v.Numbers[i] = val;
			}

			return v;
		}

		public static Vector Map(Vector v, Func<double, double, double> func)
		{
			for (int i = 0; i < v.Numbers.Count; i++)
			{
				v.Numbers[i] = func(v.Numbers[i], i);
			}

			return v;
		}

		public static double Reduce(Vector v, Func<double, double, double, double> func, double r = 0)
		{
			for (int i = 0; i < v.Numbers.Count; i++)
			{
				r = func(r, v.Numbers[i], i);
			}

			return r;
		}

		public static Vector CopyValues(Vector source, Vector? target)
		{
			if(target == null)
			{
				List<double> numbers = new List<double>(source.Numbers.Count);

				for (int i = 0; i < source.Numbers.Count; i++)
				{
					numbers.Add(0);
				}

				target = new Vector
				{
					Numbers = numbers
				};
			}

			for (int i = 0; i < source.Numbers.Count; i++)
			{
				target.Numbers[i] = source.Numbers[i];
			}

			return target;
		}

		public static double[][] Simplify2D(double[][] inputArr, double maxOffset = 0.001, double maxDistance = 10)
		{
			if (inputArr.Length <= 4)
				return inputArr;

			double o0 = inputArr[0][0];
			double o1 = inputArr[0][1];

			var arr = new List<double[]>(inputArr.Length);
			foreach (var d in inputArr)
			{
				arr.Add([d[0] - o0, d[1] - o1]);
			}

			double a0 = arr[0][0];
			double a1 = arr[0][1];
			var sim = new List<double[]> { inputArr[0] };

			for (int i = 0; i + 1 < arr.Count; i++)
			{
				double t0 = arr[i][0];
				double t1 = arr[i][1];
				double b0 = arr[i + 1][0];
				double b1 = arr[i + 1][1];

				if(b0 - t0 != 0 || b1 - t1 != 0)
				{
					// Proximity check
					double proximity = Math.Abs(a0 * b1 - a1 * b0 + b0 * t1 - b1 * t0 + a1 * t0 - a0 * t1) / Math.Sqrt(Math.Pow(b0 - a0, 2) + Math.Pow(b1 - a1, 2));

					double[] dir = { a0 - t0, a1 - t1 };
					double len = Math.Sqrt(Math.Pow(dir[0], 2) + Math.Pow(dir[1], 2));

					if(proximity > maxOffset || len > maxDistance)
					{
						sim.Add([t0 + o0, t1 + o1]);
						a0 = t0;
						a1 = t1;
					}
				}
			}

			double[] last = arr[arr.Count - 1];
			sim.Add([last[0] + o0, last[1] + o1]);

			return [.. sim];
		}

		public static double Clamp(double value, int min = 0, int max = 1)
		{
			if (value < min)
				return min;

			if (value > max)
				return max;

			return value;
		}

		public static int BinarySearch(double targetValue, int[] accumulatedValues)
		{
			int min = accumulatedValues[0];
			int max = accumulatedValues[accumulatedValues.Length - 1];

			if(targetValue >= max)
				return accumulatedValues.Length - 1;

			if (targetValue <= min)
				return 0;

			int left = 0;
			int right = accumulatedValues.Length - 1;

			while(left <= right)
			{
				int mid = (int)MathF.Floor((left + right) / 2);
				int lMid = accumulatedValues[mid];

				if (lMid < targetValue)
					left = mid + 1;
				else if (lMid > targetValue)
					right = mid - 1;
				else
					return mid;
			}

			return Math.Max(0, right);
		}
	}
}
