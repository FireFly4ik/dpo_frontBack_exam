using TransLearner.Endpoints;

namespace TransLearner.Extensions;

public static class EndpointExtension
{
    public static WebApplication MapApplicationEndpoints(this WebApplication app)
    {
        app.MapHomeEndpoints();
        app.MapCollectionEndpoints();
        app.MapStudyEndpoints();
        app.MapStatisticsEndpoints();

        return app;
    }
}
