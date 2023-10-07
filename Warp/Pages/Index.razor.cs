using Microsoft.AspNetCore.Components;
using SkiaSharp;
using SkiaSharp.Views.Blazor;
using Warp.Game;
using Renderer = Warp.Game.Renderer;

namespace Warp.Pages;

public partial class Index : IDisposable
{
	private static readonly int _viewportWidth = 960;
	private static readonly int _viewportHeight = 540;

	private SKBitmap? _bitmap;
	private Renderer? _renderer;
	private FpsCounter? _fpsCounter;
	private SKCanvasView? _canvasView;
	private readonly SKPaint _paint = new()
	{
		IsAntialias = true,
		StrokeWidth = 5f,
		StrokeCap = SKStrokeCap.Round,
		TextAlign = SKTextAlign.Center,
		TextSize = 24,
		Color = SKColors.Green
	};
	private bool disposedValue;

	[Inject] private NavigationManager? NavigationManager { get; set; }

	protected async override Task OnInitializedAsync()
	{
		await base.OnInitializedAsync();
	}

	private static string Style => $"width:{_viewportWidth}px; height={_viewportHeight}px; margin:auto; display:block";

	private void OnPaintSurface(SKPaintSurfaceEventArgs e)
	{
		var surfaceSize = e.Info.Size;
		_renderer ??= new Renderer(surfaceSize.Width, surfaceSize.Height);
		_bitmap ??= new SKBitmap(
			e.Info.Width,
			e.Info.Height,
			SKColorType.Rgba8888,
			SKAlphaType.Premul);
		_fpsCounter ??= new FpsCounter();

		unsafe
		{
			fixed (uint* ptr = _renderer.UpdateFrameBuffer())
			{
				_bitmap.SetPixels((IntPtr)ptr);
			}
		}

		var canvas = e.Surface.Canvas;
		canvas.Clear(SKColors.Black);
		canvas.DrawBitmap(_bitmap, new SKRect(0, 0, surfaceSize.Width, surfaceSize.Height));
		var fps = _fpsCounter.GetCurrentFps();

		canvas.DrawText($"{fps:0.00}fps", surfaceSize.Width / 2, surfaceSize.Height - 10f, _paint);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			if (disposing)
			{
				_paint.Dispose();
			}

			// TODO: free unmanaged resources (unmanaged objects) and override finalizer
			// TODO: set large fields to null
			disposedValue = true;
		}
	}

	public void Dispose()
	{
		// Do not change this code. Put clean up code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
