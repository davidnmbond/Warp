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

	private bool playing;
	private bool currentTrackLoaded = false;
	private int trackIndex = 0;
	private double playTime;
	private double startTime;
	private double? pauseTime;
	private double offset;
	private int interactions;
	private List<Track> _tracks = [
		new(new("sounds/birdchirp.wav", UriKind.Relative)),
		new(new("sounds/birdchirp2.wav", UriKind.Relative)),
		new(new("sounds/birdchirp3.wav", UriKind.Relative)),
		new(new("sounds/guitar-mellow-beat-20221122-128596.mp3", UriKind.Relative))
	];

	private AudioContext? _audioContext;
	private AudioDestinationNode? _audioDestinationNode;
	private AudioBufferSourceNode? _currentAudioBufferNode;
	private Track? _currentTrack;

	protected override async Task OnInitializedAsync()
	{
		try
		{
			_audioContext = await AudioContext.CreateAsync(JSRuntime);
			_audioDestinationNode = await _audioContext.GetDestinationAsync();
		}
		catch
		{
			Console.WriteLine("Couldn't initialize yet.");
		}
	}

	public async Task EnsureCurrentTrackLoadedAsync()
	{
		if (currentTrackLoaded || _audioContext is null)
		{
			return;
		}

		_currentTrack = _tracks[trackIndex];

		try
		{
			await _currentTrack.LoadAsync(HttpClient, _audioContext);
			currentTrackLoaded = true;
		}
		catch (HttpRequestException e)
		{
			Console.WriteLine(e);
		}
	}

	public async Task PlayAsync()
	{
		if (playing || _audioContext is null || _audioDestinationNode is null || _currentTrack is null)
		{
			return;
		}

		interactions++;
		await EnsureCurrentTrackLoadedAsync();

		_currentAudioBufferNode = await _audioContext.CreateBufferSourceAsync();
		await _currentAudioBufferNode.SetBufferAsync(_currentTrack.AudioBuffer);
		await _currentAudioBufferNode.ConnectAsync(_audioDestinationNode);
		if (pauseTime is null)
		{
			await _currentAudioBufferNode.StartAsync();
		}
		else
		{
			await _currentAudioBufferNode.StartAsync(when: 0, offset: offset);
		}

		startTime = await _audioContext.GetCurrentTimeAsync();

		playing = true;
		var currentInteractions = interactions;
		while (currentInteractions == interactions)
		{
			playTime = await _audioContext.GetCurrentTimeAsync() - startTime + offset;
			StateHasChanged();
			if (playTime >= _currentTrack.Duration.TotalSeconds)
			{
				await NextTrack();
			}

			await Task.Delay(100);
		}
	}

	public async Task PauseAsync()
	{
		if (!playing || _currentAudioBufferNode is null || _audioContext is null || _currentTrack is null)
		{
			return;
		}

		interactions++;

		await _currentAudioBufferNode.DisconnectAsync();
		await _currentAudioBufferNode.StopAsync();

		var currentTime = await _audioContext.GetCurrentTimeAsync();
		pauseTime = await _audioContext.GetCurrentTimeAsync();
		if (offset + currentTime - startTime > _currentTrack.Duration.TotalSeconds)
		{
			offset = 0;
		}
		else
		{
			offset += currentTime - startTime;
		}

		playing = false;
	}

	public Task PreviousTrack() => SwitchTrack(() => trackIndex = (trackIndex - 1 + _tracks.Count) % _tracks.Count);

	public Task NextTrack() => SwitchTrack(() => trackIndex = (trackIndex + 1) % _tracks.Count);

	private async Task SwitchTrack(Action changeTrack)
	{
		var wasPlaying = playing;
		if (wasPlaying)
		{
			await PauseAsync();
		}

		changeTrack();
		currentTrackLoaded = false;
		await EnsureCurrentTrackLoadedAsync();
		offset = 0;
		playTime = 0;
		if (wasPlaying)
		{
			await PlayAsync();
		}
	}

	public async Task RandomizeTracks()
	{
		var wasPlaying = playing;
		if (wasPlaying)
		{
			await PauseAsync();
		}

		_tracks = _tracks
			.OrderBy(x => Random.Shared.Next())
			.ToList();

		currentTrackLoaded = false;
		trackIndex = 0;
		offset = 0;
		playTime = 0;
		if (wasPlaying)
		{
			await PlayAsync();
		}
	}

	public async ValueTask DisposeAsync()
	{
		await PauseAsync();
	}
}