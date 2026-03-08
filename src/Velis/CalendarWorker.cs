namespace Velis;

public class CalendarWorker(CalendarReader calendarReader, VelisProxyClient velisClient) : BackgroundService
{
	private bool isRunning;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (isRunning)
		{
			await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
		}
	}

	public override Task StartAsync(CancellationToken cancellationToken)
	{
		isRunning = true;
		return base.StartAsync(cancellationToken);
	}

	public override Task StopAsync(CancellationToken cancellationToken)
	{
		isRunning = false;
		return base.StopAsync(cancellationToken);
	}
}