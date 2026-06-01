using TransLearner.Models;
using TransLearner.Services;

namespace TransLearner.Endpoints;

public static class CollectionEndpoints
{
    public static void MapCollectionEndpoints(this WebApplication app)
    {
        app.MapGet("/collections", (ICollectionService collectionService) =>
            {
                return Results.Json(collectionService.GetCollections());
            })
            .WithName("GetCollections")
            .WithTags("Collections");

        app.MapGet("/collections/{type}/{name}", (string type, string name, ICollectionService collectionService) =>
            {
                var collection = collectionService.GetCollection(type, name);
                return collection is null
                    ? Results.NotFound(new { message = "Коллекция не найдена" })
                    : Results.Json(collection);
            })
            .WithName("GetCollection")
            .WithTags("Collections");

        app.MapPost("/collections/user", (CreateCollectionRequest request, ICollectionService collectionService) =>
            {
                try
                {
                    var collection = collectionService.CreateUserCollection(request);
                    var encodedName = Uri.EscapeDataString(collection.Name);
                    return Results.Created($"/collections/user/{encodedName}", collection);
                }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
                {
                    return Results.BadRequest(new { message = exception.Message });
                }
            })
            .WithName("CreateUserCollection")
            .WithTags("Collections");

        app.MapDelete("/collections/user/{name}", (string name, ICollectionService collectionService) =>
            {
                return collectionService.DeleteUserCollection(name)
                    ? Results.NoContent()
                    : Results.NotFound(new { message = "Пользовательская коллекция не найдена" });
            })
            .WithName("DeleteUserCollection")
            .WithTags("Collections");

        app.MapPost("/collections/user/{name}/translations", (string name, TranslationRequest request, ICollectionService collectionService) =>
            {
                try
                {
                    var collection = collectionService.AddTranslation(name, request);
                    return collection is null
                        ? Results.NotFound(new { message = "Пользовательская коллекция не найдена" })
                        : Results.Json(collection);
                }
                catch (ArgumentException exception)
                {
                    return Results.BadRequest(new { message = exception.Message });
                }
            })
            .WithName("AddTranslation")
            .WithTags("Collections");

        app.MapDelete("/collections/user/{name}/translations", (string name, string foreignWord, ICollectionService collectionService) =>
            {
                return collectionService.DeleteTranslation(name, foreignWord)
                    ? Results.NoContent()
                    : Results.NotFound(new { message = "Перевод не найден" });
            })
            .WithName("DeleteTranslation")
            .WithTags("Collections");
    }
}
