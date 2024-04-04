using LiteDB;

namespace Simplistant_API.Repository
{
    public static class RepositoryFactory
    {
        public static IRepository<T> Create<T>(LiteDatabase databaseInstance) where T : DataItem
        {
            return new Repository<T>(databaseInstance);
        }
    }
}
