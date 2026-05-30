using TransLearner.Services;

namespace TransLearner.Extensions;

public static class ServicesExtension
{
    public static IServiceCollection ServicesDI(this IServiceCollection services)
    {
        services.AddSingleton<ICollectionService, FileCollectionService>();
        services.AddSingleton<IStatisticsService, FileStatisticsService>();
        services.AddSingleton<IStudySessionService, StudySessionService>();

        return services;
    }
}
