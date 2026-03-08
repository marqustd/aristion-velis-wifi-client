using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;

namespace Velis;

/// <summary>
///     Simple proxy for the Ariston/Velis REST endpoints.  A <see cref="HttpClient" />
///     configured with a <see cref="CookieContainer" /> is used so that the
///     authentication cookies returned by the login call are preserved for
///     subsequent requests exactly as the TypeScript implementation uses a
///     tough-cookie jar.
/// </summary>
public class VelisProxyClient(
	IHttpClientFactory httpClientFactory,
	IOptions<AristionOptions> options,
	ILogger<VelisProxyClient> logger)
{
	private const string LoginEndpoint = "/R2/Account/Login?returnUrl=%2FR2%2FHome";

	private readonly AristionOptions aristionOptions = options.Value;

	private readonly HttpClient client = httpClientFactory.CreateClient(AristionOptions.ClientName);

	private bool loggedIn;
	private string plantId = string.Empty;

	private string GetSensorsUrl
		=> $"/api/v2/velis/{aristionOptions.DeviceType}PlantData/{plantId}?appId=com.remotethermo.velis";

	private string SetTemperatureUrl =>
		$"/api/v2/velis/{aristionOptions.DeviceType}PlantData/{plantId}/temperature?appId=com.remotethermo.velis";


	public async Task InitializeLoginAsync(CancellationToken cancellationToken = default)
	{
		if (loggedIn)
		{
			return;
		}

		var auth = Convert.ToBase64String(
			Encoding.UTF8.GetBytes($"{aristionOptions.Username}:{aristionOptions.Password}"));

		var request = new HttpRequestMessage(HttpMethod.Post, LoginEndpoint)
		{
			Content = JsonContent.Create(new
			{
				email = aristionOptions.Username,
				password = aristionOptions.Password,
				rememberMe = false,
				language = "English_Us"
			})
		};

		request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);

		using var response =
			await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

		// The Ariston server often returns a 302/303 redirect on successful login; the
		// TypeScript implementation allowed any status <400.  HttpClient's
		// EnsureSuccessStatusCode treats 3xx as failure, so we only call it for
		// real error codes and treat redirects as "success" so we can read the
		// Location header.
		if ((int) response.StatusCode >= 400)
		{
			// Will throw an exception with details if something really went wrong
			response.EnsureSuccessStatusCode();
		}

		var redirectUrl = response.Headers.Location?.ToString() ?? string.Empty;
		if (!string.IsNullOrEmpty(redirectUrl))
		{
			plantId = GetPlantId(redirectUrl);
		}

		if (!string.IsNullOrEmpty(plantId))
		{
			plantId = aristionOptions.GatewayId;
		}

		loggedIn = true;
		logger.LogInformation("Logged in successfully, Plant ID: {PlantId}", plantId);
	}

	private static string GetPlantId(string urlStr)
	{
		if (string.IsNullOrEmpty(urlStr))
		{
			return string.Empty;
		}

		var parts = urlStr.Split('/');

		if (parts.Any(p => new[]
			    {
				    "PlantDashboard", "PlantManagement", "PlantPreference", "Error", "PlantGuest", "TimeProg"
			    }
			    .Contains(p)))
		{
			return parts.Length > 5 ? parts[5] : string.Empty;
		}

		if (parts.Any(p => p == "PlantData" || p == "UserData"))
		{
			return parts.Length > 5 ? parts[5].Split('?')[0] : string.Empty;
		}

		if (urlStr.Contains("/Menu/User/Index/"))
		{
			return parts.Length > 6 ? parts[6] : string.Empty;
		}

		if (urlStr.Contains("/R2/Plant/Index/"))
		{
			return parts.Length > 6 ? parts[6].Split('?')[0] : string.Empty;
		}

		return string.Empty;
	}

	public async Task<SenorsInfo> FetchSensorsAsync(CancellationToken cancellationToken = default)
	{
		if (!loggedIn)
		{
			await InitializeLoginAsync(cancellationToken);
		}

		HttpResponseMessage mainResponse;

		try
		{
			mainResponse = await client.GetAsync(GetSensorsUrl, cancellationToken);
		}
		catch (HttpRequestException e)
		{
			// In case of low‑level errors there won't be a status code but we
			// still want some diagnostic information.
			logger.LogError(e, "Error getting sensors");
			throw;
		}

		if (mainResponse.StatusCode == HttpStatusCode.MethodNotAllowed)
		{
			logger.LogWarning("GET returned 405, retrying with POST");
			mainResponse = await client.PostAsync(GetSensorsUrl, JsonContent.Create(new { }), cancellationToken);
		}

		// A 302 from this endpoint usually means the cookie jar is no longer
		// valid; try to re-authenticate once before giving up.
		if (mainResponse.StatusCode == HttpStatusCode.Found || mainResponse.StatusCode == HttpStatusCode.Redirect)
		{
			logger.LogWarning("fetchSensors returned redirect; reinitializing login and retrying");
			loggedIn = false;
			await InitializeLoginAsync(cancellationToken);
			mainResponse = await client.GetAsync(GetSensorsUrl, cancellationToken);
			if (mainResponse.StatusCode == HttpStatusCode.MethodNotAllowed)
			{
				mainResponse = await client.PostAsync(GetSensorsUrl, JsonContent.Create(new { }), cancellationToken);
			}
		}

		mainResponse.EnsureSuccessStatusCode();

		var sensors = await mainResponse.Content.ReadFromJsonAsync<GetSensorsResponse>(cancellationToken);

		if (sensors is null)
		{
			throw new InvalidOperationException("Can't read main sensor data");
		}

		return new SenorsInfo
		{
			Eco = sensors.Eco,
			Mode = sensors.Mode,
			RemainingTime = sensors.RemainingTime,
			RequiredTemperature = sensors.RequiredTemperature,
			AntiLegionellaInProgress = sensors.AntiLegionellaInProgress,
			AvailableShowers = sensors.AvailableShowers,
			IsHeating = sensors.IsHeating,
			On = sensors.On,
			CurrentTemperature = sensors.Temperature,
			ProcessingTemperature = sensors.ProcessingTemperature
		};
	}


	public async Task SetTemperatureAsync(int newTemp, CancellationToken cancellationToken = default)
	{
		if (!loggedIn)
		{
			await InitializeLoginAsync(cancellationToken);
		}

		var oldTemp = 47; // no cache available currently

		var payload = new SetTemperatureRequest(oldTemp, newTemp);
		var response = await client.PostAsJsonAsync(SetTemperatureUrl, payload, cancellationToken);
		response.EnsureSuccessStatusCode();
	}
}

public record SetTemperatureRequest(int Old, int New);

public enum Mode
{
	Manual = 0,
	Eco = 1,
	Scheduled = 5
}

public record SenorsInfo
{
	public bool On { get; set; }

	public Mode Mode { get; set; }

	public double CurrentTemperature { get; set; }

	public double RequiredTemperature { get; set; }
	public double ProcessingTemperature { get; set; }

	public bool AntiLegionellaInProgress { get; set; }

	public bool IsHeating { get; set; }

	public bool Eco { get; set; }

	public TimeSpan RemainingTime { get; set; }

	public int AvailableShowers { get; set; }
}

public record GetSensorsResponse
{
	[JsonPropertyName("gw")] public string Gateway { get; init; } = string.Empty;

	[JsonPropertyName("on")] public bool On { get; set; }

	[JsonPropertyName("mode")] public Mode Mode { get; set; }

	[JsonPropertyName("temp")] public double Temperature { get; set; }

	[JsonPropertyName("procReqTemp")] public double ProcessingTemperature { get; set; }

	[JsonPropertyName("reqTemp")] public double RequiredTemperature { get; set; }

	[JsonPropertyName("antiLeg")] public bool AntiLegionellaInProgress { get; set; }

	[JsonPropertyName("heatReq")] public bool IsHeating { get; set; }

	[JsonPropertyName("eco")] public bool Eco { get; set; }

	[JsonPropertyName("rmTm")] public TimeSpan RemainingTime { get; set; }

	[JsonPropertyName("avShw")] public int AvailableShowers { get; set; }
	[JsonPropertyName("pwrOpt")] public bool PowerOptimization { get; set; }
}