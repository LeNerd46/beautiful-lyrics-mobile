/*
 * 
 * C# implementation of cubic splie by morganherlocker
 * https://github.com/morganherlocker/cubic-spline
 * 
 */

namespace BeautifulLyricsMobileV2.Entities
{
	internal class Spline
	{
		public List<double> XS { get; }
		public List<double> YS { get; }
		public List<double> KS { get; }

		public Spline(List<double> xs, List<double> ys)
		{
			XS = xs;
			YS = ys;

			List<double> list = [];

			for (int i = 0; i < XS.Count; i++)
			{
				list.Add(0);
			}

			KS = GetNaturalKs(list);
		}

		public List<double> GetNaturalKs(List<double> ks)
		{
			int n = XS.Count - 1;
			List<List<double>> A = ZerosMat(n + 1, n + 2);

			for (int i = 1; i < n; i++)
			{
				A[i][i - 1] = 1 / (XS[i] - XS[i - 1]);
				A[i][i] = 2 * (1 / (XS[i] - XS[i - 1]) + 1 / (XS[i + 1] - XS[i]));
				A[i][i + 1] = 1 / (XS[i + 1] - XS[i]);
				A[i][n + 1] = 3 * ((YS[i] - YS[i - 1]) / ((XS[i] - XS[i - 1]) * (XS[i] - XS[i - 1])) + (YS[i + 1] - YS[i]) / ((XS[i + 1] - XS[i]) * (XS[i + 1] - XS[i])));
			}

			A[0][0] = 2 / (XS[1] - XS[0]);
			A[0][1] = 1 / (XS[1] - XS[0]);
			A[0][n + 1] = (3 * (YS[1] - YS[0])) / ((XS[1] - XS[0]) * (XS[1] - XS[0]));

			A[n][n - 1] = 1 / (XS[n] - XS[n - 1]);
			A[n][n] = 2 / (XS[n] - XS[n - 1]);
			A[n][n + 1] = (3 * (YS[n] - YS[n - 1])) / ((XS[n] - XS[n - 1]) * (XS[n] - XS[n - 1]));

			return Solve(A, ks);
		}

		public int GetIndexBefore(double target)
		{
			int low = 0;
			int high = XS.Count;
			int mid = 0;

			while (low < high)
			{
				mid = (int)Math.Floor((double)((low + high) / 2));

				if (XS[mid] < target && mid != low)
					low = mid;
				else if (XS[mid] >= target && mid != high)
					high = mid;
				else
					high = low;
			}

			if (low == XS.Count - 1)
				return XS.Count - 1;

			return low + 1;
		}

		public double At(double x)
		{
			int i = GetIndexBefore(x);
			double t = (x - XS[i - 1]) / (XS[i] - XS[i - 1]);

			double a = KS[i - 1] * (XS[i] - XS[i - 1]) - (YS[i] - YS[i - 1]);
			double b = -KS[i] * (XS[i] - XS[i - 1]) + (YS[i] - YS[i - 1]);
			double q = (1 - t) * YS[i - 1] + t * YS[i] + t * (1 - t) * (a * (1 - t) + b * t);

			return q;
		}

		public List<double> Solve(List<List<double>> A, List<double> ks)
		{
			int m = A.Count;
			int h = 0;
			int k = 0;

			while (h < m && k <= m)
			{
				int iMax = 0;
				double max = double.NegativeInfinity;

				for (int i = h; i < m; i++)
				{
					double v = Math.Abs(A[i][k]);

					if (v > max)
					{
						iMax = i;
						max = v;
					}
				}

				if (A[iMax][k] == 0)
					k++;
				else
				{
					SwapRows(A, h, iMax);

					for (int i = h + 1; i < m; i++)
					{
						double f = A[i][k] / A[h][k];
						A[i][k] = 0;

						for (int j = k + 1; j <= m; j++)
						{
							A[i][j] -= A[h][j] * f;
						}
					}

					h++;
					k++;
				}
			}

			for (int i = m - 1; i >= 0; i--)
			{
				double v = 0;

				if (A[i][i] != 0)
					v = A[i][m] / A[i][i];

				ks[i] = v;

				for (int j = i - 1; j >= 0; j--)
				{
					A[j][m] -= A[j][i] * v;
					A[j][i] = 0;
				}
			}

			return ks;
		}

		public List<List<double>> ZerosMat(double r, int c)
		{
			List<List<double>> A = [];

			for (int i = 0; i < r; i++)
			{
				List<double> list = [];

				for (int j = 0; j < c; j++)
				{
					list.Add(0);
				}

				A.Add(list);
			}

			return A;
		}

		public void SwapRows(List<List<double>> m, int k, int l)
		{
			var p = m[k];
			m[k] = m[l];
			m[l] = p;
		}
	}

	internal class SplineOld
	{
		public List<double> XS { get; }
		public List<double> YS { get; }
		public List<double> KS { get; }

		public SplineOld(List<double> xs, List<double> ys)
		{
			XS = xs;
			YS = ys;

			List<double> list = [];

			for (int i = 0; i < XS.Count; i++)
			{
				list.Add(0);
			}

			KS = GetNaturalKs(list);
		}

		public List<double> GetNaturalKs(List<double> ks)
		{
			int n = XS.Count - 1;
			var A = ZerosMat(n + 1, n + 2);

			for (int i = 1; i < n; i++)
			{
				A[i][i - 1] = 1 / (XS[i] - XS[i - 1]);
				A[i][i] = 2 * (1 / (XS[i] - XS[i - 1]) + 1 / (XS[i + 1] - XS[i]));
				A[i][i + 1] = 1 / (XS[i + 1] - XS[i]);
				A[i][n + 1] = 3 * ((YS[i] - YS[i - 1]) / ((XS[i] - XS[i - 1]) * (XS[i] - XS[i - 1])) + (YS[i + 1] - YS[i]) / ((XS[i + 1] - XS[i]) * (XS[i + 1] - XS[i])));
			}

			A[0][0] = 2 / (XS[1] - XS[0]);
			A[0][1] = 1 / (XS[1] - XS[0]);
			A[0][n + 1] = 3 * (YS[1] - YS[0]) / ((XS[1] - XS[0]) * (XS[1] - XS[0]));

			A[n][n - 1] = 1 / (XS[n] - XS[n - 1]);
			A[n][n] = 2 / (XS[n] - XS[n - 1]);
			A[n][n + 1] = 3 * (YS[n] - YS[n - 1]) / ((XS[n] - XS[n - 1]) * (XS[n] - XS[n - 1]));

			return Solve(A, ks);
		}

		public int GetIndexBefore(double target)
		{
			int low = 0;
			int high = XS.Count;
			int mid = 0;

			while (low < high)
			{
				mid = (int)Math.Floor((double)((low + high) / 2));

				if (XS[mid] < target && mid != low)
					low = mid;
				else if (XS[mid] >= target && mid != high)
					high = mid;
				else
					high = low;
			}

			if (low == XS.Count - 1)
				return XS.Count - 1;

			return low + 1;
		}

		public double At(double x)
		{
			int i = GetIndexBefore(x);
			var t = (x - XS[i - 1]) / (XS[i] - XS[i - 1]);
			var a = KS[i - 1] * (XS[i] - XS[i - 1]) - (YS[i] - YS[i - 1]);
			var b = -KS[i] * (XS[i] - XS[i - 1]) + (YS[i] - YS[i - 1]);

			return (1 - t) * YS[i - 1] + t * YS[i] + t * (1 - t) * (a * (1 - t) + b * t);
		}

		private List<double> Solve(List<List<double>> A, List<double> ks)
		{
			int m = A.Count;
			int h = 0;
			int k = 0;

			while (h < m && k <= m)
			{
				int iMax = 0;
				int max = -int.MaxValue; // Close enough

				for (int i = h; i < m; i++)
				{
					int v = Math.Abs((int)A[i][k]);

					if (v > max)
					{
						iMax = i;
						max = v;
					}
				}

				if (A[iMax][k] == 0)
					k++;
				else
				{
					SwapRows(A, h, iMax);

					for (int i = h; i < m; i++)
					{
						var f = A[i][k] / A[h][k];
						A[i][k] = 0;

						for (int j = k + 1; j <= m; j++)
						{
							A[i][j] -= A[h][j] * f;
						}

						h++;
						k++;
					}
				}
			}

			for (int i = m - 1; i >= 0; i--)
			{
				double v = 0;

				if (A[i][i] != 0)
					v = A[i][m] / A[i][i];

				ks[i] = v;

				for (int j = i - 1; j >= 0; j--)
				{
					A[j][m] -= A[j][i] * v;
					A[j][i] = 0;
				}
			}

			return ks;
		}

		private List<List<double>> ZerosMat(int r, int c)
		{
			List<List<double>> A = [];

			for (int i = 0; i < r; i++)
			{
				List<double> list = [];

				for (int j = 0; j < c; j++)
				{
					list.Add(0);
				}

				A.Add(list);
			}

			return A;
		}

		private void SwapRows(List<List<double>> m, int k, int l)
		{
			(m[l], m[k]) = (m[k], m[l]);
		}
	}
}