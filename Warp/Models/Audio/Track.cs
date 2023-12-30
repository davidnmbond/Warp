using KristofferStrube.Blazor.WebAudio;

namespace Warp.Models.Audio;

public class Track(Uri uri)
{
	public Uri Uri { get; } = uri;

	public string Name => Uri.ToString().Split('/').Last();

	public AudioBuffer? AudioBuffer { get; private set; }

	public TimeSpan Duration { get; internal set; }

	public bool IsLoaded => AudioBuffer is not null;

	public async Task LoadAsync(HttpClient httpClient, AudioContext _audioContext)
	{
		var data = await httpClient.GetByteArrayAsync(Uri);
		await _audioContext.DecodeAudioDataAsync(
			data,
			async (audioBuffer) =>
			{
				AudioBuffer = audioBuffer;
				Duration = TimeSpan.FromSeconds(await audioBuffer.GetDurationAsync());
			}
		);
	}
}
