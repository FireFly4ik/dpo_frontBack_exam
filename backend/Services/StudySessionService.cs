using TransLearner.Models;

namespace TransLearner.Services;

public class StudySessionService : IStudySessionService
{
    private readonly ICollectionService _collectionService;
    private readonly IStatisticsService _statisticsService;
    private readonly Dictionary<Guid, StudySession> _sessions = new();
    private readonly object _locker = new();

    public StudySessionService(ICollectionService collectionService, IStatisticsService statisticsService)
    {
        _collectionService = collectionService;
        _statisticsService = statisticsService;
    }

    public StudySession CreateSession()
    {
        var session = new StudySession { Id = Guid.NewGuid() };

        lock (_locker)
        {
            _sessions[session.Id] = session;
        }

        return session;
    }

    public StudySession? GetSession(Guid sessionId)
    {
        lock (_locker)
        {
            return _sessions.TryGetValue(sessionId, out var session) ? session : null;
        }
    }

    public AnswerResult? SubmitAnswer(Guid sessionId, AnswerRequest request)
    {
        lock (_locker)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
            {
                return null;
            }

            var collection = _collectionService.GetCollection(request.CollectionType, request.CollectionName);
            var translation = collection?.Translations.FirstOrDefault(t =>
                string.Equals(t.ForeignWord, request.ForeignWord.Trim(), StringComparison.OrdinalIgnoreCase));

            if (translation is null)
            {
                return null;
            }

            var normalizedAnswer = NormalizeAnswer(request.Answer);
            var expectedAnswer = NormalizeAnswer(translation.RussianWord);
            var isCorrect = string.Equals(normalizedAnswer, expectedAnswer, StringComparison.OrdinalIgnoreCase);

            if (isCorrect)
            {
                session.CorrectAnswers++;
                session.WrongList.RemoveAll(wrong =>
                    string.Equals(wrong.CollectionType, request.CollectionType, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(wrong.CollectionName, request.CollectionName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(wrong.ForeignWord, translation.ForeignWord, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                session.WrongAnswers++;
                AddWrongAnswer(session, request, translation);
            }

            _statisticsService.AddAnswer(isCorrect);

            return new AnswerResult
            {
                IsCorrect = isCorrect,
                ExpectedAnswer = translation.RussianWord,
                SessionCorrectAnswers = session.CorrectAnswers,
                SessionWrongAnswers = session.WrongAnswers
            };
        }
    }

    private static void AddWrongAnswer(StudySession session, AnswerRequest request, Translation translation)
    {
        var wrong = session.WrongList.FirstOrDefault(item =>
            string.Equals(item.CollectionType, request.CollectionType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.CollectionName, request.CollectionName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.ForeignWord, translation.ForeignWord, StringComparison.OrdinalIgnoreCase));

        if (wrong is null)
        {
            session.WrongList.Add(new WrongAnswer
            {
                CollectionType = request.CollectionType,
                CollectionName = request.CollectionName,
                ForeignWord = translation.ForeignWord,
                ExpectedAnswer = translation.RussianWord,
                UserAnswer = request.Answer.Trim(),
                MistakesCount = 1
            });

            return;
        }

        wrong.UserAnswer = request.Answer.Trim();
        wrong.MistakesCount++;
    }

    private static string NormalizeAnswer(string value)
    {
        return string.Join(' ', value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
