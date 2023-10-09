using KristofferStrube.Blazor.WebAudio;
using Microsoft.AspNetCore.Components.Web;

namespace Warp.Pages;

public partial class Keyboard
{
	AudioContext context = default!;
	OscillatorNode? oscillator;
	GainNode? gainNode;
	float gain = 0.05f;
	int? currentOctave;
	int? currentPitch;
	List<(string Name, int Octave, int Pitch, string Color)> keys = new();
	int[] whiteKeyIndices = [];
	int[] blackKeyIndices = [];
	readonly SemaphoreSlim semaphoreSlim = new(1);

	protected override async Task OnInitializedAsync()
	{
		context = await AudioContext.CreateAsync(JSRuntime);

		var letters = new List<string>() { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
		keys = Enumerable
			.Range(3, 3)
			.SelectMany(octave =>
				Enumerable
				.Range(1, 12)
				.Select(pitch => (letters[pitch - 1], octave, pitch, letters[pitch - 1].Contains('#') ? "black" : "white"))
			)
			.ToList();

		whiteKeyIndices = keys
			.Where(key => key.Color == "white")
			.Select(key => keys.IndexOf(key))
			.ToArray();
		blackKeyIndices = keys
			.Where(key => key.Color == "black")
			.Select(key => keys.IndexOf(key))
			.ToArray();
	}

	public async Task PointerMove(int octave, int pitch, MouseEventArgs e)
	{
		if ((e.Buttons & 1) == 1)
		{
			await StartSound(octave, pitch);
		}
		else
		{
			await StopSound();
		}
	}

	public async Task StartSound(int octave, int pitch)
	{
		await semaphoreSlim
			.WaitAsync();

		if (currentOctave != octave || currentPitch != pitch)
		{
			await StopSound();
			currentOctave = octave;
			currentPitch = pitch;

			AudioDestinationNode destination = await context.GetDestinationAsync();

			gainNode = await GainNode.CreateAsync(JSRuntime, context, new() { Gain = gain });
			await gainNode.ConnectAsync(destination);

			OscillatorOptions oscillatorOptions = new()
			{
				Type = OscillatorType.Triangle,
				Frequency = (float)Frequency(octave, pitch)
			};
			oscillator = await OscillatorNode.CreateAsync(JSRuntime, context, oscillatorOptions);

			await oscillator.ConnectAsync(gainNode);
			await oscillator.StartAsync();
		}

		semaphoreSlim.Release();
	}

	public async Task StopSound()
	{
		if (oscillator is null || gainNode is null) return;
		var currentTime = await context
			.GetCurrentTimeAsync();
		var audioParam = await gainNode
			.GetGainAsync();
		await audioParam
			.LinearRampToValueAtTimeAsync(0, currentTime + 0.3);
		oscillator = null;
		gainNode = null;
		currentOctave = null;
		currentPitch = null;
	}

	private static double Frequency(int octave, int pitch)
	{
		var noteIndex = octave * 12 + pitch;
		var a = Math.Pow(2, 1.0 / 12);
		var A4 = 440;
		var A4Index = 4 * 12 + 10;
		var halfStepDifference = noteIndex - A4Index;
		return A4 * Math.Pow(a, halfStepDifference);
	}
}
