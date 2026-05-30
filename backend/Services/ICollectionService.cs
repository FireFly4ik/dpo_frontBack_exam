using TransLearner.Models;

namespace TransLearner.Services;

public interface ICollectionService
{
    IReadOnlyList<CollectionInfo> GetCollections();
    TranslationCollection? GetCollection(string type, string name);
    TranslationCollection CreateUserCollection(CreateCollectionRequest request);
    bool DeleteUserCollection(string name);
    TranslationCollection? AddTranslation(string name, TranslationRequest request);
    bool DeleteTranslation(string name, string foreignWord);
}
