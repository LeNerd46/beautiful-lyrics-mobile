﻿using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using System.Diagnostics;

namespace BeautifulLyricsMobile.Controls
{
	public class BlobAnimationView : SKCanvasView, IDisposable
	{
		private SKPaint[] blobPaints;
		private SKBitmap image;
		private Random random = new Random();
		private Stopwatch stopwatch = new Stopwatch();
		private List<Blob> blobs = [];
		private const int blobCount = 10;

		private SKBitmap renderedBitmap = null;
		private Task computeTask = null;
		private int renderWidth = 0;
		private int renderHeight = 0;
		CancellationTokenSource cancel = null;

		private readonly object busyLock = new object();
		private bool isBusy = false;

		private IDispatcherTimer timer;

		public BlobAnimationView(SKBitmap _image, CancellationTokenSource cancelToken = null)
		{
			/*blobPaints = paints.Select(x => new SKPaint
			{
				Color = x,
				IsAntialias = true,
				BlendMode = SKBlendMode.Color
			}).ToArray();*/

			image = _image;

			if (cancelToken != null)
				cancel = cancelToken;
			else
				cancel = new CancellationTokenSource();

			var blurFilter = SKImageFilter.CreateBlur(100, 100);

			using var paint = new SKPaint
			{
				ImageFilter = blurFilter
			};

			var resizedbitmap = new SKBitmap(1000, 1000);
			var info = new SKImageInfo(resizedbitmap.Width, resizedbitmap.Height);
			var surface = SKSurface.Create(info);
			var canvas = surface.Canvas;

			canvas.Clear();
			// canvas.DrawBitmap(image, new SKRectI(0, 0, image.Width, image.Height), new SKRect(0, 0, 900, 900));
			renderedBitmap = SKBitmap.FromImage(surface.Snapshot());

			// Top left
			blobs.Add(new Blob
			{
				CenterX = 200,
				CenterY = 300,
				Radius = 500,
				Scale = 1.2f,
				Rotation = (float)(0.3f * Math.PI * 2f),
				CropRect = new SKRect(0, 0, image.Width, image.Height),
				Opposite = true
			});

			// Top right
			blobs.Add(new Blob
			{
				CenterX = 1000,
				CenterY = 150,
				Radius = 750,
				Scale = 1.2f,
				Rotation = (float)(0.3f * Math.PI * 2f),
				CropRect = new SKRect(0, 0, image.Width, image.Height),
				Opposite = true
			});

			// Bottom left
			blobs.Add(new Blob
			{
				CenterX = 150,
				CenterY = 1800,
				Radius = 750,
				Scale = 1.2f,
				Rotation = (float)(0.3f * Math.PI * 2f),
				CropRect = new SKRect(0, 0, image.Width, image.Height),
				Opposite = true
			});

			// Right middle
			blobs.Add(new Blob
			{
				CenterX = 1000,
				CenterY = 1000,
				Radius = 1200,
				Scale = 1.2f,
				Rotation = (float)(0.6f * Math.PI * 2f),
				CropRect = new SKRect(0, 0, image.Width, image.Height),
				Opposite = false
			});

			renderWidth = image.Width;
			renderHeight = image.Height;

			/*for (int i = 0; i < blobCount; i++)
			{
				blobs.Add(new Blob
				{
					CenterX = random.Next(50, 2000),
					CenterY = random.Next(50, 2000),
					Radius = random.Next(50, 100),
					Scale = (float)(1 + random.NextDouble() * 0.5f),
					Rotation = (float)(random.NextDouble() * Math.PI * 2f),
					CropRect = new SKRect(random.Next(0, image.Width / 2), random.Next(0, image.Height / 2), random.Next(image.Width / 2, image.Width), random.Next(image.Height / 2, image.Height))
				});
			}*/

			timer = Dispatcher.CreateTimer();
			timer.Interval = TimeSpan.FromMilliseconds(33);
			timer.Tick += OnTimerTick;

			timer.Start();
			stopwatch.Start();
		}

		private async void OnTimerTick(object? sender, EventArgs e)
		{
			if (isBusy) return;
			isBusy = true;

			await Task.Run(() =>
			{
				try
				{
					lock (busyLock)
					{
						var bitmap = new SKBitmap(image.Width, image.Height);
						var info = new SKImageInfo(image.Width, image.Height);
						using var surface = SKSurface.Create(info);
						var canvas = surface.Canvas;

						float time = (float)stopwatch.Elapsed.TotalMinutes * 2;

						int i = 0;
						foreach (var blob in blobs)
						{
							if (cancel == null) return;

							if (cancel.IsCancellationRequested)
							{
								canvas.Dispose();
								bitmap.Dispose();
								renderedBitmap.Dispose();

								isBusy = false;
								return;
							}

							using var paint = new SKPaint
							{
								Style = SKPaintStyle.StrokeAndFill,
								Shader = CreateBlobFeatherShader(blob, DistortImage(image, blob, time)),
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
						surface.Snapshot().ReadPixels(info, bitmap.GetPixels(), bitmap.RowBytes);


						MainThread.BeginInvokeOnMainThread(() =>
						{
							if (cancel == null) return;

							if (cancel.IsCancellationRequested)
							{
								canvas.Dispose();
								bitmap.Dispose();
								renderedBitmap.Dispose();

								isBusy = false;
								return;
							}

							var oldBitmap = renderedBitmap;
							renderedBitmap = bitmap;
							oldBitmap?.Dispose();
							InvalidateSurface();
						});
					}
				}
				catch(Exception ex)
				{
					Console.WriteLine(ex.Message);
				}
				finally
				{
					isBusy = false;
				}
			}, cancel.Token).ContinueWith((task) =>
			{
				if(task.IsFaulted)
				{
					Console.WriteLine(task.Exception.Message);
				}
			});
		}

		protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
		{
			try
			{
				base.OnPaintSurface(e);

				var canvas = e.Surface.Canvas;

				if (renderedBitmap != null)
					canvas.DrawBitmap(renderedBitmap, new SKRectI(0, 0, renderWidth, renderHeight), new SKRect(0, 0, (float)DeviceDisplay.Current.MainDisplayInfo.Width, (float)DeviceDisplay.Current.MainDisplayInfo.Height));
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}

			// canvas.DrawBitmap(renderedBitmap, 0, 0);
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
				Console.WriteLine(ex.Message);
				return null;
			}
		}

		private SKShader DistortImage(SKBitmap image, Blob blob, float time)
		{
			try
			{
				// var matrix = SKMatrix.CreateSkew(0.3f * MathF.Sin(time), 0.1f * MathF.Cos(time));
				// matrix = SKMatrix.Concat(matrix, SKMatrix.CreateTranslation(50 * MathF.Cos(time), 30 * MathF.Sin(time)));

				// var matrix = SKMatrix.CreateRotation((blob.Rotation * (blob.Opposite ? -1 : 1)) + time);
				var matrix = SKMatrix.CreateRotation((blob.Rotation + time) * (blob.Opposite ? -1 : 1));
				matrix = SKMatrix.Concat(matrix, SKMatrix.CreateScale(1.5f, 0.5f));
				matrix = SKMatrix.Concat(matrix, SKMatrix.CreateTranslation(blob.CenterX, blob.CenterY));
				matrix = SKMatrix.Concat(matrix, SKMatrix.CreateSkew(0.8f * MathF.Sin(time), 0.5f * MathF.Cos(time)));
				// matrix = SKMatrix.Concat(matrix, SKMatrix.CreateTranslation(50 * MathF.Cos(time), 30 * MathF.Sin(time)));

				using var surface = SKSurface.Create(new SKImageInfo(Math.Max(1, (int)blob.CropRect.Width), Math.Max(1, (int)blob.CropRect.Height)));
				var canvas = surface.Canvas;
				// canvas.DrawBitmap(image, blob.CropRect, new SKRect(0, 0, image.Width, image.Height));
				canvas.DrawBitmap(image, 0, 0);

				return SKShader.CreateBitmap(SKBitmap.FromImage(surface.Snapshot()), SKShaderTileMode.Mirror, SKShaderTileMode.Repeat, matrix);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return null;
			}

			// return SKShader.CreateBitmap(image, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat, matrix);
		}

		private SKShader CreateBlobFeatherShader(Blob blob, SKShader imageShader)
		{
			// var gradient = SKShader.CreateRadialGradient(new SKPoint(blob.CenterX, blob.CenterY), blob.Radius * blob.Scale, new SKColor[] { SKColors.White.WithAlpha(255), SKColors.White.WithAlpha(0) }, [0.8f, 1f], SKShaderTileMode.Clamp);

			// return SKShader.CreateCompose(gradient, imageShader);

			return imageShader;
		}

		public void Dispose()
		{
			if(timer != null)
			{
				timer.Stop();
				timer.Tick -= OnTimerTick;
				timer = null;
			}

			if(cancel != null)
			{
				if (!cancel.IsCancellationRequested)
					cancel.Cancel();

				cancel.Dispose();
				cancel = null;
			}

			renderedBitmap?.Dispose();
			renderedBitmap = null;

			image?.Dispose();
			image = null;
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
