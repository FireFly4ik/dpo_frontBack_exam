using TransLearner.Models;
using TransLearner.Services;

namespace TransLearner.Endpoints;

public static class StudyEndpoints
{
    public static void MapStudyEndpoints(this WebApplication app)
    {
        app.MapPost("/study/sessions", (IStudySessionService studySessionService) =>
            {
                var session = studySessionService.CreateSession();
                return Results.Created($"/study/sessions/{session.Id}", session);
            })
            .WithName("CreateStudySession")
            .WithTags("Study");

        app.MapGet("/study/sessions/{sessionId:guid}", (Guid sessionId, IStudySessionService studySessionService) =>
            {
                var session = studySessionService.GetSession(sessionId);
                return session is null
                    ? Results.NotFound(new { message = "Сессия не найдена" })
                    : Results.Json(session);
            })
            .WithName("GetStudySession")
            .WithTags("Study");

        app.MapGet("/study/sessions/{sessionId:guid}/wrong", (Guid sessionId, IStudySessionService studySessionService) =>
            {
                var session = studySessionService.GetSession(sessionId);
                return session is null
                    ? Results.NotFound(new { message = "Сессия не найдена" })
                    : Results.Json(session.WrongList);
            })
            .WithName("GetWrongAnswers")
            .WithTags("Study");

        app.MapPost("/study/sessions/{sessionId:guid}/answers", (Guid sessionId, AnswerRequest request, IStudySessionService studySessionService) =>
            {
                var result = studySessionService.SubmitAnswer(sessionId, request);
                return result is null
                    ? Results.NotFound(new { message = "Сессия, коллекция или слово не найдены" })
                    : Results.Json(result);
            })
            .WithName("SubmitAnswer")
            .WithTags("Study");
    }
}
