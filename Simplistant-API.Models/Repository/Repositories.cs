using LiteDB;

namespace Simplistant_API.Models.Repository
{
    public static class Repositories
    {
        private static readonly LiteDatabase SystemInstance = new(@"..\System.ldb");
        public static IRepository<T> System<T>() where T : DataItem
        {
            return new Repository<T>(SystemInstance);
        }

        private static readonly LiteDatabase UserInstance = new(@"..\Users.ldb");

        public static IRepository<T> Users<T>() where T : DataItem
        {
            return new Repository<T>(UserInstance);
        }

        //todo: not great to hold all this in memory, probably need some optimization (weakreference?)
        private static readonly Dictionary<string, LiteDatabase> DataInstances = [];
        private static readonly object _lock = new();

        public static IRepository<T> Data<T>(ObjectId userId) where T : DataItem
        {
            var path = @$"..\Data\{userId}\";
            var file = path + $"{typeof(T).Name}.ldb";
            LiteDatabase instance;
            lock (_lock)
            {
                if (!DataInstances.ContainsKey(file))
                {
                    Directory.CreateDirectory(path);
                    DataInstances.Add(file, new LiteDatabase(file));
                }
                instance = DataInstances[file];
            }
            return new Repository<T>(instance);
        }
    }
}
