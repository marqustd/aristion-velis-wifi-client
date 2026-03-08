using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Velis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddLogging();
builder.Services.AddMemoryCache();

builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowAll", policy =>
	{
		policy.AllowAnyOrigin()
			.AllowAnyMethod()
			.AllowAnyHeader();
	});
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
	options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
	options.SerializerOptions.WriteIndented = true;
	options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
	options.SerializerOptions.PropertyNameCaseInsensitive = true;
	options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});


builder.Services.Configure<AristionOptions>(
	builder.Configuration.GetSection(
		"Ariston"));

builder.Services.Configure<CalendarOptions>(
	builder.Configuration.GetSection(
		"Calendar"));


builder.Services.AddHttpClient(AristionOptions.ClientName, (provider, client) =>
{
	var options = provider.GetRequiredService<AristionOptions>();
	client.BaseAddress = new Uri(options.BaseUrl);
	client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
	CookieContainer = new CookieContainer(), AllowAutoRedirect = false // we want the location header
});

builder.Services.AddSingleton<VelisProxyClient>();
builder.Services.AddScoped<CalendarReader>();
builder.Services.AddSingleton<CalendarWorker>();
builder.Services.AddHostedService<CalendarWorker>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.MapGet("/api/fetch-sensors",
		([FromServices] VelisProxyClient proxyClient, CancellationToken ct) =>
			proxyClient.FetchSensorsAsync(ct))
	.WithName("FetchSensors");

app.MapGet("/api/calendar",
		([FromServices] CalendarReader calendarReader, CancellationToken ct) =>
			calendarReader.GetEventsForDevice(ct))
	.WithName("Calendar");

app.MapPost("/api/temperature",
		async ([FromServices] VelisProxyClient proxyClient, CancellationToken ct) =>
		await proxyClient.SetTemperatureAsync(50, ct))
	.WithName("SetTemperature");

app.Run();