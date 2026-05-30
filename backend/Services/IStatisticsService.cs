using TransLearner.Models;

namespace TransLearner.Services;

public interface IStatisticsService
{
    Statistics Get();
    Statistics AddAnswer(bool isCorrect);
    Statistics Reset();
}
