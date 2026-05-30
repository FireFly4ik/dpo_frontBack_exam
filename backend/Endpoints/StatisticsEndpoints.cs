using TransLearner.Services;

namespace TransLearner.Endpoints;

public static class StatisticsEndpoints
{
    public static void MapStatisticsEndpoints(this WebApplication app)
    {
        app.MapGet("/statistics", (IStatisticsService statisticsService) =>
            {
                return Results.Json(statisticsService.Get());
            })
            .WithName("GetStatistics")
            .WithTags("Statistics");

        app.MapDelete("/statistics", (IStatisticsService statisticsService) =>
            {
                return Results.Json(statisticsService.Reset());
            })
            .WithName("ResetStatistics")
            .WithTags("Statistics");
    }
}
