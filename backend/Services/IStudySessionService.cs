using TransLearner.Models;

namespace TransLearner.Services;

public interface IStudySessionService
{
    StudySession CreateSession();
    StudySession? GetSession(Guid sessionId);
    AnswerResult? SubmitAnswer(Guid sessionId, AnswerRequest request);
}
