using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using System.Diagnostics;

namespace BeautifulLyricsMobileV2.Controls
{
	public class BackgroundAnimationView : SKCanvasView
	{
		private SKPaint[] blobPaints;
		private SKBitmap image;
		private Stopwatch stopwatch = new Stopwatch();
		private Blob[] blobs;

		private SKBitmap? renderedBitmap = null;
		private readonly Lock renderLock = new Lock();

		private int renderWidth = 640;
		private int renderHeight = 640;
		private float deviceWidth = 0;
		private float deviceHeight = 0;

		private IDispatcherTimer timer;
		CancellationToken cancel;
		private int rendering;

		private SKBitmap? previousImage;
		private DateTime? transitionStart;
		private bool isTransitioning = false;
		readonly TimeSpan duration = TimeSpan.FromSeconds(2);

		private bool blurred = true;

		public BackgroundAnimationView() { }

		public void Initalize()
		{
			deviceWidth = (float)DeviceDisplay.Current.MainDisplayInfo.Width;
			deviceHeight = (float)DeviceDisplay.Current.MainDisplayInfo.Height;

			float w = renderWidth;   // the bitmap width you’re drawing into
			float h = renderHeight;  // the bitmap height

			blobs =
			[
				// Top-Left
				new Blob {
					CenterX = w * 0.20f,
					CenterY = h * 0.20f,
					Radius = 100,
					Scale = 1.1f,
					Rotation = 0.1f,
					CropRect = new SKRect(0, 0, w, h),
					Opposite = false,
					Tint = SKColors.LawnGreen
				},
				// Top-Right
				new Blob {
					CenterX = w * 0.80f,
					CenterY = h * 0.20f,
					Radius = 60,
					Scale = 1.2f,
					Rotation = 0.3f,
					CropRect = new SKRect(0, 0, w, h),
					Opposite = true,
					Tint = SKColors.Lavender
				},
				// Bottom-Left
				new Blob {
					CenterX = w * 0.20f,
					CenterY = h * 0.80f,
					Radius = 150,
					Scale = 1.3f,
					Rotation = 0.5f,
					CropRect = new SKRect(0, 0, w, h),
					Opposite = false,
					Tint = SKColors.LightPink
				},
				// Bottom-Right
				new Blob {
					CenterX = w * 0.80f,
					CenterY = h * 0.80f,
					Radius = 80,
					Scale = 1.1f,
					Rotation = 0.8f,
					CropRect = new SKRect(0, 0, w, h),
					Opposite = true,
					Tint = SKColors.CornflowerBlue
				},
				// Center
				new Blob {
					CenterX = w * 0.50f,
					CenterY = h * 0.50f,
					Radius = 55,
					Scale = 1.8f,
					Rotation = 0.2f,
					CropRect = new SKRect(0, 0, w, h),
					Opposite = false,
					Tint = SKColors.MediumPurple
				},
				// Middle-Top
				new Blob {
					CenterX = w * 0.50f,
					CenterY = h * 0.10f,
					Radius = 70,
					Scale = 1.1f,
					Rotation = 0.4f,
					CropRect = new SKRect(0, 0, w, h),
					Opposite = true,
					Tint = SKColors.LightYellow
				},
				// Middle-Left
				new Blob {
					CenterX = w * 0.10f,
					CenterY = h * 0.50f,
					Radius = 800,
					Scale = 1.3f,
					Rotation = 0.6f,
					Opacity = 100,
					CropRect = new SKRect(0, 0, w, h),
					Opposite= false,
					Tint = SKColors.Red
				},
				// Middle-Right
				new Blob {
					CenterX = w * 0.90f,
					CenterY = h * 0.50f,
					Radius = 70,
					Scale = 1.2f,
					Rotation = 0.7f,
					CropRect = new SKRect(0, 0, w, h),
					Opposite = true,
					Tint = SKColors.Indigo
		}];

			timer = Dispatcher.CreateTimer();
			timer.Interval = TimeSpan.FromMilliseconds(33);
			timer.Tick += OnTimerTick;

			timer.Start();
			stopwatch.Start();
		}

		public void UpdateImage(SKBitmap _image, CancellationToken cancelToken)
		{
			if (renderedBitmap != null)
			{
				previousImage = renderedBitmap.Copy();
				renderedBitmap.Dispose();

				isTransitioning = true;
				transitionStart = DateTime.Now;
			}

			image = _image;
			cancel = cancelToken;

			renderWidth = image.Width;
			renderHeight = image.Height;
		}

		public void UpdateBlurState()
		{
			blurred = !blurred;
		}

		private void OnTimerTick(object? sender, EventArgs e)
		{
			if (Interlocked.Exchange(ref rendering, 1) == 1) return;

			Task.Run(() =>
			{
				try
				{
					cancel.ThrowIfCancellationRequested();

					var bitmap = new SKBitmap(renderWidth, renderHeight);
					var info = new SKImageInfo(renderWidth, renderHeight);
					using var surface = SKSurface.Create(info);
					var canvas = surface.Canvas;

					float time = (float)stopwatch.Elapsed.TotalMinutes * 2;

					int i = 0;
					foreach (var blob in blobs)
					{
						using var paint = new SKPaint
						{
							Style = SKPaintStyle.StrokeAndFill,
							Shader = DistortImage(image, blob, time, i),
							IsAntialias = true,
							BlendMode = SKBlendMode.SrcOver,
							Color = SKColors.White.WithAlpha(i == 6 ? (byte)((MathF.Sin(time + i) * 0.5f + 0.5f) * 255) : (byte)255)
							// ColorFilter = SKColorFilter.CreateBlendMode(blob.Tint.WithAlpha(128), SKBlendMode.Modulate)
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

					if (blurred)
						canvas.DrawImage(snapshot, 0, 0, blurPaint);
					else
						canvas.DrawImage(snapshot, 0, 0);

					using var img = surface.Snapshot();
					var newbitmap = new SKBitmap(info);
					img.ReadPixels(info, newbitmap.GetPixels(), info.RowBytes, 0, 0);

					SKBitmap imageToShow = newbitmap;
					if (isTransitioning && previousImage != null)
					{
						var elapsed = DateTime.Now - transitionStart;
						float t = Math.Min((float)(elapsed.Value.TotalMilliseconds / duration.TotalMilliseconds), 1f);

						using var blendSurface = SKSurface.Create(info);
						var blendCanvas = blendSurface.Canvas;
						blendCanvas.Clear();

						using var p = new SKPaint { Color = new SKColor(255, 255, 255, (byte)((1 - t) * 255)) };
						blendCanvas.DrawBitmap(previousImage, SKRect.Create(info.Width, info.Height), p);

						using var n = new SKPaint { Color = new SKColor(255, 255, 255, (byte)(t * 255)) };
						blendCanvas.DrawBitmap(newbitmap, SKRect.Create(info.Width, info.Height), n);

						using var blendedImage = blendSurface.Snapshot();
						imageToShow = new SKBitmap(info);
						blendedImage.ReadPixels(info, imageToShow.GetPixels(), info.RowBytes, 0, 0);

						if (t >= 1f)
						{
							isTransitioning = false;
							previousImage?.Dispose();
							previousImage = null;
						}
					}

					MainThread.BeginInvokeOnMainThread(() =>
					{
						lock (renderLock)
						{
							renderedBitmap?.Dispose();
							renderedBitmap = imageToShow;
						}

						InvalidateSurface();
					});
				}
				catch (OperationCanceledException) { }
				finally { Interlocked.Exchange(ref rendering, 0); }
			});
		}

		protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
		{
			base.OnPaintSurface(e);

			SKBitmap? bitmap;

			lock (renderLock)
				bitmap = renderedBitmap;

			if (bitmap is null)
			{
				e.Surface.Canvas.Clear();
				return;
			}

			var dest = SKRect.Create(e.Info.Width, e.Info.Height);
			e.Surface.Canvas.DrawBitmap(bitmap, dest);
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

		private SKShader DistortImage(SKBitmap image, Blob blob, float time, int index)
		{
			try
			{
				float sx = blob.Scale + 0.2f * MathF.Sin(time + index);
				float sy = blob.Scale + 0.2f * 0.6f * MathF.Cos(time + index);

				float rotationSpeed = 0.3f * index;
				float skewX = 0.6f * MathF.Sin(time * (0.5f + index * 0.1f));
				float skewY = 0.4f * MathF.Cos(time * (0.3f + index * 0.1f));

				var matrix = SKMatrix.CreateRotation((blob.Rotation + time * rotationSpeed) * (blob.Opposite ? -1 : 1));
				matrix = SKMatrix.Concat(matrix, SKMatrix.CreateScale(sx, sy));
				matrix = SKMatrix.Concat(matrix, SKMatrix.CreateTranslation(blob.CenterX, blob.CenterY));
				// matrix = SKMatrix.Concat(matrix, SKMatrix.CreateSkew(0.8f * MathF.Sin(time), 0.5f * MathF.Cos(time)));
				matrix = SKMatrix.Concat(matrix, SKMatrix.CreateSkew(skewX, skewY));

				using var surface = SKSurface.Create(new SKImageInfo(Math.Max(1, (int)blob.CropRect.Width), Math.Max(1, (int)blob.CropRect.Height)));
				var canvas = surface.Canvas;

				canvas.DrawBitmap(image, 0, 0);

				using var img = surface.Snapshot();
				return img.ToShader(SKShaderTileMode.Mirror, SKShaderTileMode.Repeat, matrix);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
				return null;
			}
		}
	}

	public class Blob
	{
		/// <summary>
		/// The X position of the blob
		/// </summary>/
		public float CenterX { get; set; }

		/// <summary>
		/// The Y position of the blob
		/// </summary>
		public float CenterY { get; set; }

		/// <summary>
		/// How large the blob is
		/// </summary>
		public float Radius { get; set; }

		/// <summary>
		/// How much the blob should be scaled
		/// </summary>
		public float Scale { get; set; }

		/// <summary>
		/// How fast the blob's rotation should be
		/// </summary>
		public float Rotation { get; set; }

		/// <summary>
		/// How much it should fade in and out
		/// </summary>
		public byte Opacity { get; set; } = 255;

		/// <summary>
		/// How much to crop the album art
		/// </summary>
		public SKRect CropRect { get; set; }

		/// <summary>
		/// Which direction the blob should rotate in
		/// </summary>
		public bool Opposite { get; set; }

		/// <summary>
		/// The color to make the blob. For debug purposes
		/// </summary>
		public SKColor Tint { get; set; } = SKColors.White;
	}
}
