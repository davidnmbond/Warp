﻿using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Warp;
using Warp.Game;
using Warp.Interfaces;
using Warp.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");


builder.Services
	.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
	.AddScoped<Renderer>()
	.AddScoped<IAudioService, WebAudioService>()
	.AddScoped<ImageLoader>()
	;

await builder.Build().RunAsync();
