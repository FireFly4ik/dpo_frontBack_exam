namespace TransLearner.Models;

public class Translation
{
    public string ForeignWord { get; set; } = string.Empty;
    public string RussianWord { get; set; } = string.Empty;
}

public class TranslationCollection
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<Translation> Translations { get; set; } = new();
}

public class CreateCollectionRequest
{
    public string Name { get; set; } = string.Empty;
    public List<Translation> Translations { get; set; } = new();
}

public class TranslationRequest
{
    public string ForeignWord { get; set; } = string.Empty;
    public string RussianWord { get; set; } = string.Empty;
}

public class AnswerRequest
{
    public string CollectionType { get; set; } = "common";
    public string CollectionName { get; set; } = string.Empty;
    public string ForeignWord { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}

public class AnswerResult
{
    public bool IsCorrect { get; set; }
    public string ExpectedAnswer { get; set; } = string.Empty;
    public int SessionCorrectAnswers { get; set; }
    public int SessionWrongAnswers { get; set; }
}

public class WrongAnswer
{
    public string CollectionType { get; set; } = string.Empty;
    public string CollectionName { get; set; } = string.Empty;
    public string ForeignWord { get; set; } = string.Empty;
    public string ExpectedAnswer { get; set; } = string.Empty;
    public string UserAnswer { get; set; } = string.Empty;
    public int MistakesCount { get; set; }
}

public class StudySession
{
    public Guid Id { get; set; }
    public int CorrectAnswers { get; set; }
    public int WrongAnswers { get; set; }
    public List<WrongAnswer> WrongList { get; set; } = new();
}

public class Statistics
{
    public int CorrectAnswers { get; set; }
    public int WrongAnswers { get; set; }
    public int TotalAnswers => CorrectAnswers + WrongAnswers;
    public double CorrectRatio => TotalAnswers == 0 ? 0 : Math.Round((double)CorrectAnswers / TotalAnswers, 2);
    public double WrongRatio => TotalAnswers == 0 ? 0 : Math.Round((double)WrongAnswers / TotalAnswers, 2);
}
