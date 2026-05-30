namespace TransLearner.Endpoints;

public static class HomeEndpoints
{
    public static void MapHomeEndpoints(this WebApplication app)
    {
        app.MapGet("/", () => Results.Json(new
            {
                app = "TransLearner API",
                description = "Сервис коллекций карточек для изучения иностранных слов",
                collections = "/collections",
                study = "/study/sessions",
                statistics = "/statistics"
            }))
            .WithName("GetApiInfo")
            .WithTags("Home");
    }
}
