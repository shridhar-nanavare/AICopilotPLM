namespace AiCopilot.Worker.Options;

public sealed class WorkerJobOptions
{
    public const string SectionName = "WorkerJobs";

    public bool RunDailyScanOnStartup { get; init; } = true;

    public int DailyScanHourUtc { get; init; } = 2;

    public int EventPollIntervalSeconds { get; init; } = 60;

    public int InitialEventLookbackMinutes { get; init; } = 5;
}
