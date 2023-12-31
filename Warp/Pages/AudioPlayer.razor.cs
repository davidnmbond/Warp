using KristofferStrube.Blazor.WebAudio;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Warp.Models.Audio;

namespace Warp.Pages;

public partial class AudioPlayer : IAsyncDisposable
{
	[Inject]
	public required IJSRuntime JSRuntime { get; set; }

	[Inject]
	public required HttpClient HttpClient { get; set; }

	private PlayState _playState = PlayState.Stopped;
	private int _trackIndex = 0;
	private TimeSpan playTime;
	private TimeSpan _startTime;
	private TimeSpan? pauseTime;
	private TimeSpan _offset;
	private List<Track> _tracks = [
		new(new("sounds/birdchirp.wav", UriKind.Relative)),
		new(new("sounds/birdchirp2.wav", UriKind.Relative)),
		new(new("sounds/birdchirp3.wav", UriKind.Relative)),
		new(new("sounds/guitar-mellow-beat-20221122-128596.mp3", UriKind.Relative))
	];

	private AudioContext? _audioContext;
	private AudioDestinationNode? _audioDestinationNode;
	private CancellationTokenSource? _taskSwitcherCancellationTokenSource;
	private Task? _trackSwitcherTask;
	private AudioBufferSourceNode? _currentAudioBufferNode;
	private Track? _currentTrack;

	protected override async Task OnInitializedAsync()
	{
		_audioContext = await AudioContext.CreateAsync(JSRuntime);
		_audioDestinationNode = await _audioContext.GetDestinationAsync();
		_taskSwitcherCancellationTokenSource = new CancellationTokenSource();
		_trackSwitcherTask = TrackSwitcherAsync(_audioContext, _taskSwitcherCancellationTokenSource.Token);
	}

	private async Task TrackSwitcherAsync(AudioContext _audioContext, CancellationToken token)
	{
		while (!token.IsCancellationRequested)
		{
			if (_playState == PlayState.Playing && _currentTrack is not null)
			{
				playTime = TimeSpan.FromSeconds(await _audioContext.GetCurrentTimeAsync()) - _startTime + _offset;
				if (playTime > _currentTrack.Duration)
				{
					await NextTrackAsync();
					StateHasChanged();
				}
			}

			try
			{
				await Task.Delay(100, token);
			}
			catch (TaskCanceledException)
			{
				return;
			}
		}
	}

	public async Task EnsureCurrentTrackLoadedAsync()
	{
		_currentTrack = _tracks[_trackIndex];

		if (_currentTrack.IsLoaded == true || _audioContext is null)
		{
			return;
		}

		try
		{
			await _currentTrack.LoadAsync(HttpClient, _audioContext);
		}
		catch (HttpRequestException e)
		{
			Console.WriteLine(e);
		}
	}

	public async Task PlayAsync()
	{
		if (_playState == PlayState.Playing || _audioContext is null || _audioDestinationNode is null)
		{
			return;
		}

		await EnsureCurrentTrackLoadedAsync();

		_currentAudioBufferNode = await _audioContext.CreateBufferSourceAsync();
		await _currentAudioBufferNode.SetBufferAsync(_currentTrack!.AudioBuffer);
		await _currentAudioBufferNode.ConnectAsync(_audioDestinationNode);
		if (pauseTime is null)
		{
			await _currentAudioBufferNode.StartAsync();
		}
		else
		{
			await _currentAudioBufferNode.StartAsync(
				when: 0, // How many seconds from now before playback should start
				offset: _offset.TotalSeconds // How many seconds into the sound should playback start
			);
		}

		_startTime = TimeSpan.FromSeconds(await _audioContext.GetCurrentTimeAsync());
		_playState = PlayState.Playing;
	}

	public async Task PauseAsync()
	{
		if (_playState != PlayState.Playing || _currentAudioBufferNode is null || _audioContext is null || _currentTrack is null)
		{
			return;
		}

		await _currentAudioBufferNode.DisconnectAsync();
		await _currentAudioBufferNode.StopAsync();

		var currentTime = TimeSpan.FromSeconds(await _audioContext.GetCurrentTimeAsync());
		pauseTime = currentTime;
		if (_offset + currentTime - _startTime > _currentTrack.Duration)
		{
			_offset = TimeSpan.Zero;
		}
		else
		{
			_offset += currentTime - _startTime;
		}

		_playState = PlayState.Paused;
	}

	public Task PreviousTrack() => SwitchTrack(_trackIndex = (_trackIndex - 1 + _tracks.Count) % _tracks.Count);

	public Task NextTrackAsync() => SwitchTrack(_trackIndex = (_trackIndex + 1) % _tracks.Count);

	private async Task SwitchTrack(int trackIndex)
	{
		// Pause if necessary
		var oldPlayState = _playState;
		if (oldPlayState == PlayState.Playing)
		{
			await PauseAsync();
		}

		// Switch track
		_trackIndex = trackIndex;
		await EnsureCurrentTrackLoadedAsync();
		_offset = TimeSpan.Zero;
		playTime = TimeSpan.Zero;

		// Resume if necessary
		if (oldPlayState == PlayState.Playing)
		{
			await PlayAsync();
		}
	}

	public async Task RandomizeTracks()
	{
		var oldPlayState = _playState;
		if (oldPlayState == PlayState.Playing)
		{
			await PauseAsync();
		}

		_tracks = _tracks
			.OrderBy(x => Random.Shared.Next())
			.ToList();

		_trackIndex = 0;
		_offset = TimeSpan.Zero;
		playTime = TimeSpan.Zero;

		if (oldPlayState == PlayState.Playing)
		{
			await PlayAsync();
		}
	}

	public async ValueTask DisposeAsync()
	{
		await PauseAsync();

		_taskSwitcherCancellationTokenSource?.Cancel();
		if (_trackSwitcherTask is not null)
		{
			await _trackSwitcherTask;
		}

		_taskSwitcherCancellationTokenSource?.Dispose();

		if (_audioContext is not null)
		{
			await _audioContext.CloseAsync();
		}

		if (_audioContext is not null)
		{
			await _audioContext.DisposeAsync();
		}

		if (_audioDestinationNode is not null)
		{
			await _audioDestinationNode.DisposeAsync();
		}

		if (_currentAudioBufferNode is not null)
		{
			await _currentAudioBufferNode.DisposeAsync();
		}

		if (_currentTrack is not null)
		{
			await ((IAsyncDisposable)_currentTrack).DisposeAsync();
		}
	}
}