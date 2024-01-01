using KristofferStrube.Blazor.WebAudio;
using Microsoft.JSInterop;
using Warp.Interfaces;
using Warp.Models.Audio;

namespace Warp.Services;

public class WebAudioService(
	IJSRuntime jsRuntime,
	HttpClient httpClient) : IAudioService, IAsyncDisposable
{
	private AudioContext? _audioContext;
	private AudioDestinationNode? _audioDestinationNode;
	private List<Track> _tracks = [];
	private readonly IJSRuntime _jsRuntime = jsRuntime;
	private readonly HttpClient _httpClient = httpClient;

	public bool TracksLoaded { get; private set; }

	public async Task LoadTracksAsync(List<Track> tracks)
	{
		ArgumentNullException.ThrowIfNull(tracks);

		_audioContext ??= await AudioContext.CreateAsync(_jsRuntime);
		_audioDestinationNode ??= await _audioContext.GetDestinationAsync();
		_tracks = tracks;
		foreach (var track in _tracks)
		{
			await track.LoadAsync(_httpClient, _audioContext);
		}

		TracksLoaded = true;
	}

	public async ValueTask DisposeAsync()
	{
		if (_audioDestinationNode is not null)
		{
			await _audioDestinationNode.DisposeAsync();
		}

		if (_audioContext is not null)
		{
			await _audioContext.DisposeAsync();
		}
	}
}
