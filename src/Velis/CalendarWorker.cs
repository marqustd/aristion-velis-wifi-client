namespace Velis;

public class CalendarWorker(CalendarReader calendarReader, VelisProxyClient velisClient, ILogger<CalendarWorker> logger)
	: BackgroundService
{
	private const int IntervalMinutes = 15;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			var now = DateTime.Now;
			var minutes = now.Minute;
			var remainder = minutes % IntervalMinutes;
			var initialDelay = TimeSpan.FromMinutes((IntervalMinutes - remainder) % IntervalMinutes);
			logger.LogInformation("Waiting for {InitialDelay} to match sharp interval", initialDelay);
			await Task.Delay(initialDelay, stoppingToken);

			try
			{
				var events = await calendarReader.GetEventsForDevice(stoppingToken);
				logger.LogInformation("Fetched {EventCount} events", events.Count);


				foreach (var calendarEvent in events)
				{
					logger.LogInformation("Processing event: {Summary} from {Start} to {End}",
						calendarEvent.Summary, calendarEvent.Start, calendarEvent.End);
				}
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error while processing calendar events");
			}

			await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
		}
	}
}

public record HeaterTemperatureEvent(string Summary, DateTime Start, DateTime End) : CalendarEvent(Summary, Start, End)
{
	public int? Temperature { get; }
}