namespace Simplistant_API.Domain.Stemming
{
    public interface IStemmer
    {
        public List<string> Stem(string word);
    }
}
