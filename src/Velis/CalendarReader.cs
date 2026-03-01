using Ical.Net;

namespace velis;

public sealed class CalendarReader
{
	private readonly string calendarUrl;
	private readonly string[] deviceAliases;


	private readonly HttpClient httpClient;
	private readonly ILogger<CalendarReader> logger;

	public CalendarReader(IHttpClientFactory httpClientFactory,
		IConfiguration configuration,
		ILogger<CalendarReader> logger)
	{
		this.logger = logger;
		calendarUrl = configuration["Calendar:Url"] ?? throw new ArgumentException("CalendarUrl");
		var deviceAliasesString = configuration["Calendar:Aliases"] ?? throw new ArgumentException("CalendarUrl");
		httpClient = httpClientFactory.CreateClient();
		deviceAliases = deviceAliasesString.Split(';').Select(a => a.Trim()).ToArray();
	}

	public async Task<IReadOnlyCollection<string>> GetEventsAsync(CancellationToken cancellationToken = default)
	{
		var response = await httpClient.GetAsync(calendarUrl, cancellationToken);
		response.EnsureSuccessStatusCode();
		var content = await response.Content.ReadAsStringAsync(cancellationToken);
		logger.LogInformation("Calendar content: {Content}", content);

		var calendar = Calendar.Load(content);

		var events = calendar!.Events
			.Where(e => e.Start != null && e.Start.AsUtc > DateTime.UtcNow)
			.Where(e => e.End != null && e.End.AsUtc > DateTime.UtcNow)
			.Where(e => !string.IsNullOrWhiteSpace(e.Summary) &&
			            deviceAliases.Any(deviceAlias => e.Summary.StartsWith($"#{deviceAlias}")));

		var x = 1;
		return events.Select(e => e.Summary!).ToList();
	}
}