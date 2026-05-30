using System.Text.Json;
using TransLearner.Models;

namespace TransLearner.Services;

public class FileStatisticsService : IStatisticsService
{
    private readonly string _statisticsPath;
    private readonly object _locker = new();

    public FileStatisticsService(IWebHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "Data");
        Directory.CreateDirectory(dataDirectory);
        _statisticsPath = Path.Combine(dataDirectory, "statistics.json");
    }

    public Statistics Get()
    {
        lock (_locker)
        {
            return Read();
        }
    }

    public Statistics AddAnswer(bool isCorrect)
    {
        lock (_locker)
        {
            var statistics = Read();
            if (isCorrect)
            {
                statistics.CorrectAnswers++;
            }
            else
            {
                statistics.WrongAnswers++;
            }

            Write(statistics);
            return statistics;
        }
    }

    public Statistics Reset()
    {
        lock (_locker)
        {
            var statistics = new Statistics();
            Write(statistics);
            return statistics;
        }
    }

    private Statistics Read()
    {
        if (!File.Exists(_statisticsPath))
        {
            return new Statistics();
        }

        var json = File.ReadAllText(_statisticsPath);
        return JsonSerializer.Deserialize<Statistics>(json) ?? new Statistics();
    }

    private void Write(Statistics statistics)
    {
        var json = JsonSerializer.Serialize(statistics, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_statisticsPath, json);
    }
}
