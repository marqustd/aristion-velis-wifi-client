using Ical.Net;

namespace Velis;

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

	public async Task<IReadOnlyCollection<CalendarEvent>> GetEventsForDevice(
		CancellationToken cancellationToken = default)
	{
		var response = await httpClient.GetAsync(calendarUrl, cancellationToken);
		response.EnsureSuccessStatusCode();
		var content = await response.Content.ReadAsStringAsync(cancellationToken);
		logger.LogInformation("Calendar content: {Content}", content);

		var calendar = Calendar.Load(content);

		var events = calendar!.Events
			.Where(e => e.Start != null && e.Start.AsUtc.Date >= DateTime.UtcNow.Date)
			.Where(e => e.End != null && e.End.AsUtc.Date >= DateTime.UtcNow.Date)
			.Where(e => !string.IsNullOrWhiteSpace(e.Summary) &&
			            deviceAliases.Any(deviceAlias => e.Summary.StartsWith($"#{deviceAlias}")));

		return events.Select(e => new CalendarEvent(e.Summary!, e.Start!.AsUtc, e.End!.AsUtc)).ToArray();
	}
}