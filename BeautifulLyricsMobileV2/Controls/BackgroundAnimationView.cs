using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using System.Diagnostics;

namespace BeautifulLyricsMobileV2.Controls
{
	public class BackgroundAnimationView : SKCanvasView, IDisposable
	{
		private SKPaint[] blobPaints;
		private SKBitmap image;
		private Stopwatch stopwatch = new Stopwatch();
		private Blob[] blobs;

		private SKBitmap? renderedBitmap = null;
		private readonly Lock renderLock = new Lock();

		private int renderWidth = 0;
		private int renderHeight = 0;
		private float deviceWidth = 0;
		private float deviceHeight = 0;

		private IDispatcherTimer timer;
		CancellationToken cancel;

		public BackgroundAnimationView(SKBitmap _image, CancellationToken cancelToken)
		{
			image = _image;
			cancel = cancelToken;

			renderWidth = image.Width;
			renderHeight = image.Height;
			deviceWidth = (float)DeviceDisplay.Current.MainDisplayInfo.Width;
			deviceHeight = (float)DeviceDisplay.Current.MainDisplayInfo.Height;

			blobs = [new Blob
			{
				// Top left
				CenterX = 200,
				CenterY = 300,
				Radius = 500,
				Scale = 1.2f,
				Rotation = (float)(0.3f * Math.PI * 2f),
				CropRect = new SKRect(0, 0, image.Width, image.Height),
				Opposite = true
			},
			new Blob
			{
				// Top right
				CenterX = 1000,
				CenterY = 150,
				Radius = 750,
				Scale = 1.2f,
				Rotation = (float)(0.3f * Math.PI * 2f),
				CropRect = new SKRect(0, 0, image.Width, image.Height),
				Opposite = true
			},
			new Blob
			{
				// Bottom left
				CenterX = 150,
				CenterY = 1800,
				Radius = 750,
				Scale = 1.2f,
				Rotation = (float)(0.3f * Math.PI * 2f),
				CropRect = new SKRect(0, 0, image.Width, image.Height),
				Opposite = true
			},
			new Blob
			{
				// Right middle
				CenterX = 1000,
				CenterY = 1000,
				Radius = 1200,
				Scale = 1.2f,
				Rotation = (float)(0.6f * Math.PI * 2f),
				CropRect = new SKRect(0, 0, image.Width, image.Height),
				Opposite = false
			}];

			timer = Dispatcher.CreateTimer();
			timer.Interval = TimeSpan.FromMilliseconds(33);
			timer.Tick += OnTimerTick;

			timer.Start();
			stopwatch.Start();
		}

		private void OnTimerTick(object? sender, EventArgs e)
		{
			Task.Run(() =>
			{
				try
				{
					cancel.ThrowIfCancellationRequested();

					var bitmap = new SKBitmap(image.Width, image.Height);
					var info = new SKImageInfo(image.Width, image.Height);
					using var surface = SKSurface.Create(info);
					var canvas = surface.Canvas;

					float time = (float)stopwatch.Elapsed.TotalMinutes * 2;

					int i = 0;
					foreach (var blob in blobs)
					{
						using var paint = new SKPaint
						{
							Style = SKPaintStyle.StrokeAndFill,
							Shader = DistortImage(image, blob, time),
							IsAntialias = true,
							BlendMode = SKBlendMode.SrcOver
						};

						var path = CreateBlobPath(blob, time, i);
						canvas.DrawPath(path, paint);

						i++;
					}

					using var snapshot = surface.Snapshot();
					using var blurPaint = new SKPaint
					{
						ImageFilter = SKImageFilter.CreateBlur(75, 75)
					};

					canvas.Clear();
					canvas.DrawImage(snapshot, 0, 0, blurPaint);

					using var img = surface.Snapshot();
					var newbitmap = new SKBitmap(info);
					img.ReadPixels(info, newbitmap.GetPixels(), info.RowBytes, 0, 0);

					MainThread.BeginInvokeOnMainThread(() =>
					{
						lock (renderLock)
						{
							renderedBitmap?.Dispose();
							renderedBitmap = newbitmap;
						}

						InvalidateSurface();
					});
				}
				catch (OperationCanceledException)
				{

				}
			});
		}

		protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
		{
			base.OnPaintSurface(e);

			SKBitmap? bitmap;

			lock (renderLock)
				bitmap = renderedBitmap;

			if(bitmap is null)
			{
				e.Surface.Canvas.Clear();
				return;
			}

			var dest = SKRect.Create(e.Info.Width, e.Info.Height);
			e.Surface.Canvas.DrawBitmap(bitmap, dest);

			/*try
			{
				var canvas = e.Surface.Canvas;

				if (renderedBitmap != null)
					canvas.DrawBitmap(renderedBitmap, new SKRectI(0, 0, renderWidth, renderHeight), new SKRect(0, 0, deviceWidth, deviceHeight));
			}
			catch(Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}*/
		}

		private SKPath CreateBlobPath(Blob blob, float time, int index)
		{
			try
			{
				const int numPoints = 12;
				const float angleStep = 360f / numPoints;

				SKPath path = new SKPath();

				for (int i = 0; i < numPoints; i++)
				{
					float angle = (float)((Math.PI / 180) * (angleStep * i));
					float offset = (MathF.Sin(angle * 3 + time * 2 + index) * blob.Radius / 5) * 0.05f;

					float x = blob.CenterX + (blob.Radius + offset) * MathF.Cos(angle) * 3;
					float y = blob.CenterY + (blob.Radius + offset) * MathF.Sin(angle) * 3;

					if (i == 0)
						path.MoveTo(x, y);
					else
						path.LineTo(x, y);
				}

				path.Close();
				return path;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
				return null;
			}
		}

		private SKShader DistortImage(SKBitmap image, Blob blob, float time)
		{
			try
			{
				var matrix = SKMatrix.CreateRotation((blob.Rotation + time) * (blob.Opposite ? -1 : 1));
				matrix = SKMatrix.Concat(matrix, SKMatrix.CreateScale(1.5f, 0.5f));
				matrix = SKMatrix.Concat(matrix, SKMatrix.CreateTranslation(blob.CenterX, blob.CenterY));
				matrix = SKMatrix.Concat(matrix, SKMatrix.CreateSkew(0.8f * MathF.Sin(time), 0.5f * MathF.Cos(time)));

				using var surface = SKSurface.Create(new SKImageInfo(Math.Max(1, (int)blob.CropRect.Width), Math.Max(1, (int)blob.CropRect.Height)));
				var canvas = surface.Canvas;

				canvas.DrawBitmap(image, 0, 0);
				return SKShader.CreateBitmap(SKBitmap.FromImage(surface.Snapshot()), SKShaderTileMode.Mirror, SKShaderTileMode.Repeat, matrix);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
				return null;
			}
		}

		public void Dispose()
		{

		}
	}

	public class Blob
	{
		public float CenterX { get; set; }
		public float CenterY { get; set; }
		public float Radius { get; set; }
		public float Scale { get; set; }
		public float Rotation { get; set; }
		public SKRect CropRect { get; set; }
		public bool Opposite { get; set; }
	}
}
