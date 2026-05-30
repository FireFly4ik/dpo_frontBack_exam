using TransLearner.Models;

namespace TransLearner.Services;

public class FileCollectionService : ICollectionService
{
    private readonly string _commonDirectory;
    private readonly string _userDirectory;
    private readonly object _locker = new();

    public FileCollectionService(IWebHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "Data");
        _commonDirectory = Path.Combine(dataDirectory, "common");
        _userDirectory = Path.Combine(dataDirectory, "user");

        Directory.CreateDirectory(_commonDirectory);
        Directory.CreateDirectory(_userDirectory);
    }

    public IReadOnlyList<CollectionInfo> GetCollections()
    {
        lock (_locker)
        {
            var common = ReadCollectionInfos(_commonDirectory, "common");
            var user = ReadCollectionInfos(_userDirectory, "user");

            return common.Concat(user).OrderBy(c => c.Type).ThenBy(c => c.Name).ToList();
        }
    }

    public TranslationCollection? GetCollection(string type, string name)
    {
        lock (_locker)
        {
            var path = GetCollectionPath(type, name);
            if (path is null || !File.Exists(path))
            {
                return null;
            }

            return new TranslationCollection
            {
                Name = Path.GetFileNameWithoutExtension(path),
                Type = type,
                Translations = ReadTranslations(path)
            };
        }
    }

    public TranslationCollection CreateUserCollection(CreateCollectionRequest request)
    {
        var name = NormalizeName(request.Name);
        var path = Path.Combine(_userDirectory, $"{name}.txt");

        lock (_locker)
        {
            if (File.Exists(path))
            {
                throw new InvalidOperationException($"Коллекция '{name}' уже существует");
            }

            WriteTranslations(path, NormalizeTranslations(request.Translations));
            return GetCollection("user", name)!;
        }
    }

    public bool DeleteUserCollection(string name)
    {
        var path = Path.Combine(_userDirectory, $"{NormalizeName(name)}.txt");

        lock (_locker)
        {
            if (!File.Exists(path))
            {
                return false;
            }

            File.Delete(path);
            return true;
        }
    }

    public TranslationCollection? AddTranslation(string name, TranslationRequest request)
    {
        var collectionName = NormalizeName(name);
        var path = Path.Combine(_userDirectory, $"{collectionName}.txt");

        lock (_locker)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var translations = ReadTranslations(path);
            var translation = NormalizeTranslation(request);
            var existing = translations.FirstOrDefault(t =>
                string.Equals(t.ForeignWord, translation.ForeignWord, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                translations.Add(translation);
            }
            else
            {
                existing.RussianWord = translation.RussianWord;
            }

            WriteTranslations(path, translations);
            return GetCollection("user", collectionName);
        }
    }

    public bool DeleteTranslation(string name, string foreignWord)
    {
        var collectionName = NormalizeName(name);
        var path = Path.Combine(_userDirectory, $"{collectionName}.txt");

        lock (_locker)
        {
            if (!File.Exists(path))
            {
                return false;
            }

            var translations = ReadTranslations(path);
            var removed = translations.RemoveAll(t =>
                string.Equals(t.ForeignWord, foreignWord.Trim(), StringComparison.OrdinalIgnoreCase));

            if (removed == 0)
            {
                return false;
            }

            WriteTranslations(path, translations);
            return true;
        }
    }

    private IEnumerable<CollectionInfo> ReadCollectionInfos(string directory, string type)
    {
        return Directory
            .GetFiles(directory, "*.txt")
            .Select(file => new CollectionInfo
            {
                Name = Path.GetFileNameWithoutExtension(file),
                Type = type,
                WordsCount = ReadTranslations(file).Count
            });
    }

    private string? GetCollectionPath(string type, string name)
    {
        var normalizedName = NormalizeName(name);

        return type.ToLowerInvariant() switch
        {
            "common" => Path.Combine(_commonDirectory, $"{normalizedName}.txt"),
            "user" => Path.Combine(_userDirectory, $"{normalizedName}.txt"),
            _ => null
        };
    }

    private static List<Translation> ReadTranslations(string path)
    {
        return File
            .ReadAllLines(path)
            .Select(ParseTranslationLine)
            .Where(translation => translation is not null)
            .Select(translation => translation!)
            .ToList();
    }

    private static Translation? ParseTranslationLine(string line)
    {
        var parts = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            return null;
        }

        return new Translation
        {
            ForeignWord = parts[0],
            RussianWord = parts[1]
        };
    }

    private static void WriteTranslations(string path, IEnumerable<Translation> translations)
    {
        var lines = NormalizeTranslations(translations)
            .OrderBy(t => t.ForeignWord)
            .Select(t => $"{t.ForeignWord} {t.RussianWord}");

        File.WriteAllLines(path, lines);
    }

    private static List<Translation> NormalizeTranslations(IEnumerable<Translation> translations)
    {
        return translations
            .Select(translation => NormalizeTranslation(new TranslationRequest
            {
                ForeignWord = translation.ForeignWord,
                RussianWord = translation.RussianWord
            }))
            .GroupBy(translation => translation.ForeignWord, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .ToList();
    }

    private static Translation NormalizeTranslation(TranslationRequest request)
    {
        var foreignWord = request.ForeignWord.Trim();
        var russianWord = request.RussianWord.Trim();

        if (string.IsNullOrWhiteSpace(foreignWord) || string.IsNullOrWhiteSpace(russianWord))
        {
            throw new ArgumentException("Слово и перевод обязательны");
        }

        if (foreignWord.Contains(' ') || foreignWord.Contains('\n') || russianWord.Contains('\n'))
        {
            throw new ArgumentException("Иностранное слово должно быть одним токеном, перевод не должен содержать переносов строк");
        }

        return new Translation
        {
            ForeignWord = foreignWord,
            RussianWord = russianWord
        };
    }

    private static string NormalizeName(string name)
    {
        var normalizedName = name.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new ArgumentException("Название коллекции обязательно");
        }

        if (normalizedName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("Название коллекции содержит недопустимые символы");
        }

        return normalizedName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
            ? Path.GetFileNameWithoutExtension(normalizedName)
            : normalizedName;
    }
}
