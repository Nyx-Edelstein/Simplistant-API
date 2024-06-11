using NHunspell;

namespace Simplistant_API.Domain.Stemming
{
    public class Stemmer : IStemmer, IDisposable
    {
        private Hunspell _Hunspell { get; } = new();

        public void Dispose()
        {
            _Hunspell.Dispose();
        }

        public List<string> Stem(string word)
        {
            return _Hunspell.Stem(word);
        }
    }
}
