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
	private Dictionary<string, Sprite>? _spriteSet;
	private int _time;
	private int _xPixelOffset;
	private int _yPixelOffset;
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
	private TileMap? _tileMap;

	[Inject] private ImageLoader? ImageLoader { get; set; }

	[Inject] private NavigationManager? NavigationManager { get; set; }

	[Inject] private HttpClient? HttpClient { get; set; }

	protected async override Task OnInitializedAsync()
	{
		var cacheBuster = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

		_tileMap = new TileMap(new Uri($"{NavigationManager!.BaseUri}/maps/w01-01.txt?_={cacheBuster}"));
		await _tileMap.LoadAsync(HttpClient!, default);

		var spriteMap = new SpriteMap(new Uri($"{NavigationManager!.BaseUri}/sprites/sprites.png?_={cacheBuster}"));
		await spriteMap.LoadAsync(ImageLoader!, default);

		_spriteSet = spriteMap.GetSprites();

		await base.OnInitializedAsync();
	}

	private static string Style => $"width:{_viewportWidth}px; height={_viewportHeight}px; margin:auto; display:block";

	private void OnPaintSurface(SKPaintSurfaceEventArgs e)
	{
		if (_tileMap is null || _spriteSet is null)
		{
			return;
		}

		var surfaceSize = e.Info.Size;
		_renderer ??= new Renderer(surfaceSize.Width, surfaceSize.Height);
		_bitmap ??= new SKBitmap(
			e.Info.Width,
			e.Info.Height,
			SKColorType.Rgba8888,
			SKAlphaType.Premul);
		_fpsCounter ??= new FpsCounter();

		_time++;

		_xPixelOffset = _time;
		_yPixelOffset = (int)(100 * Math.Sin((float)_time / 10));

		unsafe
		{
			fixed (uint* ptr = _renderer.UpdateFrameBuffer(
				_tileMap,
				_spriteSet,
				_xPixelOffset,
				_yPixelOffset))
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
