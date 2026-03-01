using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using velis;

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

var clientName = builder.Configuration["Ariston:Client"];
ArgumentException.ThrowIfNullOrWhiteSpace(clientName);

builder.Services.AddHttpClient(clientName, client =>
{
	var baseAddress = builder.Configuration["Ariston:BaseUrl"];
	ArgumentException.ThrowIfNullOrWhiteSpace(baseAddress);
	client.BaseAddress = new Uri(baseAddress);
	client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
	CookieContainer = new CookieContainer(), AllowAutoRedirect = false // we want the location header
});
builder.Services.AddSingleton<VelisProxyClient>();
builder.Services.AddScoped<CalendarReader>();

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
			calendarReader.GetEventsAsync(ct))
	.WithName("Calendar");

app.MapPost("/api/temperature",
		async ([FromServices] VelisProxyClient proxyClient, CancellationToken ct) =>
		await proxyClient.SetTemperatureAsync(50, ct))
	.WithName("SetTemperature");

app.Run();