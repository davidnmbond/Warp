using KristofferStrube.Blazor.WebAudio;

namespace Warp.Models.Audio;

public class ActiveTrack(Track track, AudioBufferSourceNode audioBufferSourceNode)
{
	public Track Track { get; } = track;

	public AudioBufferSourceNode AudioBufferSourceNode { get; } = audioBufferSourceNode;
}
