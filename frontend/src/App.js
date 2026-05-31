import { useEffect, useMemo, useState } from 'react';
import './App.css';

const API_URL = process.env.REACT_APP_API_URL || 'http://localhost:5171';

const emptyStatistics = {
  correctAnswers: 0,
  wrongAnswers: 0,
  totalAnswers: 0,
  correctRatio: 0,
  wrongRatio: 0,
};

function shuffleWords(words) {
  const shuffled = [...words];

  for (let index = shuffled.length - 1; index > 0; index--) {
    const randomIndex = Math.floor(Math.random() * (index + 1));
    [shuffled[index], shuffled[randomIndex]] = [shuffled[randomIndex], shuffled[index]];
  }

  return shuffled;
}

async function request(path, options = {}) {
  const response = await fetch(`${API_URL}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...options.headers,
    },
    ...options,
  });

  if (response.status === 204) {
    return null;
  }

  const payload = await response.json().catch(() => null);

  if (!response.ok) {
    throw new Error(payload?.message || 'Не удалось выполнить запрос к API');
  }

  return payload;
}

function App() {
  const [collections, setCollections] = useState([]);
  const [selectedCollection, setSelectedCollection] = useState(null);
  const [collectionDetails, setCollectionDetails] = useState(null);
  const [session, setSession] = useState(null);
  const [statistics, setStatistics] = useState(emptyStatistics);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [answer, setAnswer] = useState('');
  const [feedback, setFeedback] = useState(null);
  const [wrongMode, setWrongMode] = useState(false);
  const [wrongWords, setWrongWords] = useState([]);
  const [isCollectionModalOpen, setCollectionModalOpen] = useState(false);
  const [isWordModalOpen, setWordModalOpen] = useState(false);
  const [newCollection, setNewCollection] = useState({ name: '', foreignWord: '', russianWord: '' });
  const [newWord, setNewWord] = useState({ foreignWord: '', russianWord: '' });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const currentWords = wrongMode ? wrongWords : collectionDetails?.translations || [];
  const currentWord = currentWords.length > 0 ? currentWords[currentIndex % currentWords.length] : null;
  const canEditSelectedCollection = selectedCollection?.type === 'user';

  const sessionStats = useMemo(() => {
    return {
      correct: session?.correctAnswers ?? 0,
      wrong: session?.wrongAnswers ?? 0,
    };
  }, [session]);

  useEffect(() => {
    async function init() {
      setLoading(true);
      try {
        const [collectionsData, sessionData, statisticsData] = await Promise.all([
          request('/collections'),
          request('/study/sessions', { method: 'POST' }),
          request('/statistics'),
        ]);

        setCollections(collectionsData);
        setSession(sessionData);
        setStatistics(statisticsData);

        if (collectionsData.length > 0) {
          setSelectedCollection(collectionsData[0]);
        }
      } catch (requestError) {
        setError(requestError.message);
      } finally {
        setLoading(false);
      }
    }

    init();
  }, []);

  useEffect(() => {
    if (!selectedCollection) {
      setCollectionDetails(null);
      return;
    }

    async function loadSelectedCollection() {
      try {
        setError('');
        const details = await request(`/collections/${selectedCollection.type}/${encodeURIComponent(selectedCollection.name)}`);
        setCollectionDetails({
          ...details,
          translations: shuffleWords(details.translations || []),
        });
        setCurrentIndex(0);
        setAnswer('');
        setFeedback(null);
        setWrongMode(false);
      } catch (requestError) {
        setError(requestError.message);
      }
    }

    loadSelectedCollection();
  }, [selectedCollection]);

  async function loadCollections(preferredCollection = selectedCollection) {
    const data = await request('/collections');
    setCollections(data);

    if (data.length === 0) {
      setSelectedCollection(null);
      return;
    }

    const nextSelected = data.find((collection) =>
      preferredCollection &&
      collection.name === preferredCollection.name &&
      collection.type === preferredCollection.type
    ) || data[0];

    setSelectedCollection(nextSelected);
  }

  async function refreshStatistics() {
    const data = await request('/statistics');
    setStatistics(data);
  }

  async function submitAnswer(event) {
    event.preventDefault();

    if (!session?.id || !currentWord || !answer.trim()) {
      return;
    }

    const source = wrongMode
      ? {
          collectionType: currentWord.collectionType,
          collectionName: currentWord.collectionName,
          foreignWord: currentWord.foreignWord,
        }
      : {
          collectionType: selectedCollection.type,
          collectionName: selectedCollection.name,
          foreignWord: currentWord.foreignWord,
        };

    try {
      setError('');
      const result = await request(`/study/sessions/${session.id}/answers`, {
        method: 'POST',
        body: JSON.stringify({
          ...source,
          answer,
        }),
      });

      setFeedback(result);
      setSession((previous) => ({
        ...previous,
        correctAnswers: result.sessionCorrectAnswers,
        wrongAnswers: result.sessionWrongAnswers,
      }));
      setAnswer('');
      await refreshStatistics();

      if (wrongMode) {
        const updatedWrongWords = await request(`/study/sessions/${session.id}/wrong`);
        setWrongWords(shuffleWords(updatedWrongWords));
        setCurrentIndex((index) => getNextIndex(index, updatedWrongWords.length));
        return;
      }

      setCurrentIndex((index) => getNextIndex(index, currentWords.length));
    } catch (requestError) {
      setError(requestError.message);
    }
  }

  async function repeatWrongWords() {
    if (!session?.id) {
      return;
    }

    try {
      setError('');
      const data = await request(`/study/sessions/${session.id}/wrong`);
      setWrongWords(shuffleWords(data));
      setWrongMode(true);
      setCurrentIndex(0);
      setFeedback(null);
      setAnswer('');
    } catch (requestError) {
      setError(requestError.message);
    }
  }

  async function resetStatistics() {
    try {
      setError('');
      const data = await request('/statistics', { method: 'DELETE' });
      setStatistics(data);
    } catch (requestError) {
      setError(requestError.message);
    }
  }

  async function createCollection(event) {
    event.preventDefault();

    const collectionName = newCollection.name.trim();
    const translations = newCollection.foreignWord.trim() && newCollection.russianWord.trim()
      ? [{
          foreignWord: newCollection.foreignWord.trim(),
          russianWord: newCollection.russianWord.trim(),
        }]
      : [];

    try {
      setError('');
      const created = await request('/collections/user', {
        method: 'POST',
        body: JSON.stringify({
          name: collectionName,
          translations,
        }),
      });

      await loadCollections({
        name: created?.name || collectionName,
        type: created?.type || 'user',
      });
      setCollectionModalOpen(false);
      setNewCollection({ name: '', foreignWord: '', russianWord: '' });
    } catch (requestError) {
      setError(requestError.message);
    }
  }

  async function deleteCollection() {
    if (!canEditSelectedCollection) {
      return;
    }

    try {
      setError('');
      await request(`/collections/user/${encodeURIComponent(selectedCollection.name)}`, { method: 'DELETE' });
      await loadCollections(null);
    } catch (requestError) {
      setError(requestError.message);
    }
  }

  async function addWord(event) {
    event.preventDefault();

    if (!canEditSelectedCollection) {
      return;
    }

    try {
      setError('');
      const updatedCollection = await request(`/collections/user/${encodeURIComponent(selectedCollection.name)}/translations`, {
        method: 'POST',
        body: JSON.stringify({
          foreignWord: newWord.foreignWord.trim(),
          russianWord: newWord.russianWord.trim(),
        }),
      });

      setCollectionDetails({
        ...updatedCollection,
        translations: shuffleWords(updatedCollection.translations || []),
      });
      setWordModalOpen(false);
      setNewWord({ foreignWord: '', russianWord: '' });
      await loadCollections(selectedCollection);
    } catch (requestError) {
      setError(requestError.message);
    }
  }

  async function deleteWord() {
    if (!canEditSelectedCollection || !currentWord) {
      return;
    }

    try {
      setError('');
      await request(
        `/collections/user/${encodeURIComponent(selectedCollection.name)}/translations?foreignWord=${encodeURIComponent(currentWord.foreignWord)}`,
        { method: 'DELETE' }
      );
      const updatedCollection = await request(`/collections/user/${encodeURIComponent(selectedCollection.name)}`);
      setCollectionDetails({
        ...updatedCollection,
        translations: shuffleWords(updatedCollection.translations || []),
      });
      setCurrentIndex((index) => getNextIndex(index, updatedCollection.translations.length));
      await loadCollections(selectedCollection);
    } catch (requestError) {
      setError(requestError.message);
    }
  }

  return (
    <main className="app-shell">
      <aside className="sidebar" aria-label="Навигация по коллекциям">
        <div className="brand">
          <div>
            <h1>TransLearner</h1>
            <p>Карточки для изучения языков</p>
          </div>
        </div>

        <div className="sidebar-actions">
          <button className="button button-primary" onClick={() => setCollectionModalOpen(true)}>
            + Коллекция
          </button>
          <button
            className="button button-danger"
            onClick={deleteCollection}
            disabled={!canEditSelectedCollection}
          >
            Удалить
          </button>
        </div>

        <button className="button button-secondary wide" onClick={repeatWrongWords} disabled={!session?.id}>
          Повторить неправильные слова
        </button>

        <div className="collections-list">
          {collections.map((collection) => {
            const isActive = selectedCollection?.name === collection.name && selectedCollection?.type === collection.type;

            return (
              <button
                className={`collection-card ${isActive ? 'active' : ''}`}
                key={`${collection.type}-${collection.name}`}
                onClick={() => setSelectedCollection(collection)}
              >
                <span className="collection-name">{collection.name}</span>
                <span className={`tag ${collection.type === 'user' ? 'tag-user' : 'tag-common'}`}>
                  {collection.type === 'user' ? 'Пользовательская' : 'Предустановленная'}
                </span>
                <span className="word-count">{collection.wordsCount} слов</span>
              </button>
            );
          })}
        </div>
      </aside>

      <section className="workspace">
        <header className="topbar">
          <div>
            <p className="eyebrow">{wrongMode ? 'Режим повторения' : 'Текущая коллекция'}</p>
            <h2>{wrongMode ? 'Неправильные слова' : selectedCollection?.name || 'Коллекция не выбрана'}</h2>
          </div>

          <div className="stats-group">
            <div className="stats-card session-panel" aria-label="Статистика сессии">
              <div className="stats-metrics two-columns">
                <div>
                  <span>Правильно</span>
                  <strong>{sessionStats.correct}</strong>
                </div>
                <div>
                  <span>Неправильно</span>
                  <strong>{sessionStats.wrong}</strong>
                </div>
              </div>
              <p>Статистика сессии</p>
            </div>

            <div className="stats-card stats-panel" aria-label="Общая статистика">
              <div className="stats-metrics">
                <div>
                  <span>Правильно</span>
                  <strong>{statistics.correctAnswers}</strong>
                </div>
                <div>
                  <span>Неправильно</span>
                  <strong>{statistics.wrongAnswers}</strong>
                </div>
                <div>
                  <span>Точность</span>
                  <strong>{Math.round((statistics.correctRatio || 0) * 100)}%</strong>
                </div>
                <button className="button button-ghost" onClick={resetStatistics}>Обнулить</button>
              </div>
              <p>Общая статистика</p>
            </div>
          </div>
        </header>

        {error && <div className="notice notice-error">{error}</div>}
        {loading && <div className="notice">Загрузка приложения...</div>}

        <div className="study-card">
          {currentWord ? (
            <>
              <div className="word-area">
                <span className="word-label">Переведите слово</span>
                <div className="foreign-word">{currentWord.foreignWord}</div>
              </div>

              <form className="answer-form" onSubmit={submitAnswer}>
                <input
                  value={answer}
                  onChange={(event) => setAnswer(event.target.value)}
                  placeholder="Введите перевод на русском"
                  aria-label="Введите перевод"
                />
                <button className="button button-primary" type="submit">Проверить</button>
              </form>

              {feedback && (
                <div className={`notice ${feedback.isCorrect ? 'notice-success' : 'notice-warning'}`}>
                  {feedback.isCorrect ? 'Верно.' : 'Неверно.'} Правильный ответ: {feedback.expectedAnswer}
                </div>
              )}
            </>
          ) : (
            <div className="empty-state">
              <h3>{wrongMode ? 'Ошибок для повторения нет' : 'В коллекции пока нет слов'}</h3>
              <p>{canEditSelectedCollection ? 'Добавьте первое слово через кнопку ниже.' : 'Выберите другую коллекцию или создайте свою.'}</p>
            </div>
          )}
        </div>

        <footer className="bottom-actions">
          <button
            className="button button-secondary button-small"
            onClick={() => setWordModalOpen(true)}
            disabled={!canEditSelectedCollection}
          >
            Добавить слово в коллекцию
          </button>
          <button
            className="button button-danger button-small"
            onClick={deleteWord}
            disabled={!canEditSelectedCollection || !currentWord}
          >
            Удалить текущее слово
          </button>
        </footer>
      </section>

      {isCollectionModalOpen && (
        <Modal title="Новая коллекция" onClose={() => setCollectionModalOpen(false)}>
          <form className="modal-form" onSubmit={createCollection}>
            <label>
              Название коллекции
              <input
                value={newCollection.name}
                onChange={(event) => setNewCollection({ ...newCollection, name: event.target.value })}
                placeholder="spanish-a1"
                required
              />
            </label>
            <label>
              Первое иностранное слово
              <input
                value={newCollection.foreignWord}
                onChange={(event) => setNewCollection({ ...newCollection, foreignWord: event.target.value })}
                placeholder="hola"
              />
            </label>
            <label>
              Перевод
              <input
                value={newCollection.russianWord}
                onChange={(event) => setNewCollection({ ...newCollection, russianWord: event.target.value })}
                placeholder="привет"
              />
            </label>
            <div className="modal-actions">
              <button className="button button-ghost" type="button" onClick={() => setCollectionModalOpen(false)}>
                Отмена
              </button>
              <button className="button button-primary" type="submit">Создать</button>
            </div>
          </form>
        </Modal>
      )}

      {isWordModalOpen && (
        <Modal title="Добавить слово" onClose={() => setWordModalOpen(false)}>
          <form className="modal-form" onSubmit={addWord}>
            <label>
              Иностранное слово
              <input
                value={newWord.foreignWord}
                onChange={(event) => setNewWord({ ...newWord, foreignWord: event.target.value })}
                placeholder="ability"
                required
              />
            </label>
            <label>
              Перевод
              <input
                value={newWord.russianWord}
                onChange={(event) => setNewWord({ ...newWord, russianWord: event.target.value })}
                placeholder="способность"
                required
              />
            </label>
            <div className="modal-actions">
              <button className="button button-ghost" type="button" onClick={() => setWordModalOpen(false)}>
                Отмена
              </button>
              <button className="button button-primary" type="submit">Сохранить</button>
            </div>
          </form>
        </Modal>
      )}
    </main>
  );
}

function Modal({ title, children, onClose }) {
  return (
    <div className="modal-backdrop" role="presentation" onMouseDown={onClose}>
      <section className="modal" role="dialog" aria-modal="true" aria-label={title} onMouseDown={(event) => event.stopPropagation()}>
        <div className="modal-header">
          <h2>{title}</h2>
          <button className="icon-button" type="button" onClick={onClose} aria-label="Закрыть">x</button>
        </div>
        {children}
      </section>
    </div>
  );
}

function getNextIndex(currentIndex, length) {
  if (length <= 1) {
    return 0;
  }

  return (currentIndex + 1) % length;
}

export default App;
