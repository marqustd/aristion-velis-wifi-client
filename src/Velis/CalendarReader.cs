using Ical.Net;
using Microsoft.Extensions.Options;

namespace Velis;

public sealed class CalendarReader
{
	private readonly string[] deviceAliases;


	private readonly HttpClient httpClient;
	private readonly ILogger<CalendarReader> logger;
	private readonly CalendarOptions options;

	public CalendarReader(IHttpClientFactory httpClientFactory,
		IOptions<CalendarOptions> calendarOptions,
		ILogger<CalendarReader> logger)
	{
		options = calendarOptions.Value;
		this.logger = logger;
		httpClient = httpClientFactory.CreateClient();
		deviceAliases = options.Aliases.Split(';').Select(a => a.Trim()).ToArray();
	}

	public async Task<IReadOnlyCollection<CalendarEvent>> GetEventsForDevice(
		CancellationToken cancellationToken = default)
	{
		var response = await httpClient.GetAsync(options.Url, cancellationToken);
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