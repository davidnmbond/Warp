using KristofferStrube.Blazor.WebAudio;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Warp.Pages;

public partial class AudioPlayer : IAsyncDisposable
{
	[Inject]
	public required IJSRuntime JSRuntime { get; set; }

	[Inject]
	public required HttpClient HttpClient { get; set; }


	private bool playing;
	private bool currentTrackLoaded = false;
	private int currentTrack = 0;
	private double playTime;
	private double startTime;
	private double? pauseTime;
	private double offset;
	private double trackDuration;
	private int interactions;
	private List<string> tracks = [
		"sounds/birdchirp.wav",
		"sounds/birdchirp2.wav",
		"sounds/birdchirp3.wav",
		"sounds/guitar-mellow-beat-20221122-128596.mp3",
	];

	private AudioContext? _audioContext;
	private AudioDestinationNode? _audioDestinationNode;
	private AudioBufferSourceNode? _currentAudioBufferNode;
	private AudioBuffer? _currentAudioBuffer;

	protected override async Task OnInitializedAsync()
	{
		try
		{
			_audioContext = await AudioContext.CreateAsync(JSRuntime);
			_audioDestinationNode = await _audioContext.GetDestinationAsync();
			await EnsureCurrentTrackLoaded();
		}
		catch
		{
			Console.WriteLine("Couldn't initialize yet.");
		}
	}

	public async Task EnsureCurrentTrackLoaded()
	{
		if (currentTrackLoaded || _audioContext is null)
		{
			return;
		}

		byte[] trackData;
		try
		{
			trackData = await HttpClient.GetByteArrayAsync(tracks[currentTrack]);
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			return;
		}

		await _audioContext.DecodeAudioDataAsync(
			trackData,
			(audioBuffer) =>
			{
				_currentAudioBuffer = audioBuffer;
				return Task.CompletedTask;
			}
		);

		if (_currentAudioBuffer is null)
		{
			return;
		}

		trackDuration = await _currentAudioBuffer
			.GetDurationAsync();

		currentTrackLoaded = true;
	}

	public async Task PlayAsync()
	{
		if (playing || _audioContext is null || _audioDestinationNode is null)
		{
			return;
		}

		interactions++;
		await EnsureCurrentTrackLoaded();

		_currentAudioBufferNode = await _audioContext.CreateBufferSourceAsync();
		await _currentAudioBufferNode.SetBufferAsync(_currentAudioBuffer);
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
			if (playTime >= trackDuration)
			{
				await NextTrack();
			}

			await Task.Delay(100);
		}
	}

	public async Task PauseAsync()
	{
		if (!playing || _currentAudioBufferNode is null || _audioContext is null)
		{
			return;
		}

		interactions++;

		await _currentAudioBufferNode.DisconnectAsync();
		await _currentAudioBufferNode.StopAsync();

		var currentTime = await _audioContext.GetCurrentTimeAsync();
		pauseTime = await _audioContext.GetCurrentTimeAsync();
		if (offset + currentTime - startTime > trackDuration)
		{
			offset = 0;
		}
		else
		{
			offset += currentTime - startTime;
		}

		playing = false;
	}

	public Task PreviousTrack() => SwitchTrack(() => currentTrack = (currentTrack - 1 + tracks.Count) % tracks.Count);
	public Task NextTrack() => SwitchTrack(() => currentTrack = (currentTrack + 1) % tracks.Count);

	private async Task SwitchTrack(Action changeTrack)
	{
		var wasPlaying = playing;
		if (wasPlaying)
		{
			await PauseAsync();
		}

		changeTrack();
		currentTrackLoaded = false;
		await EnsureCurrentTrackLoaded();
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

		tracks = tracks
			.OrderBy(x => Random.Shared.Next())
			.ToList();

		currentTrackLoaded = false;
		currentTrack = 0;
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