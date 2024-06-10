using LiteDB;

namespace Simplistant_API.Repository
{
    public static class DatabaseSelector
    {
        public static LiteDatabase System { get; } = new LiteDatabase(@"..\System.ldb");
        public static LiteDatabase Users { get; } = new LiteDatabase(@"..\Users.ldb");
        public static LiteDatabase IndexKeys(ObjectId userId)
        {
            var path = @$"..\Data\{userId}\IndexKeys.ldb";
            Directory.CreateDirectory(path);
            return new LiteDatabase(path);
        }

        public static LiteDatabase IndexEntries(ObjectId userId)
        {
            var path = @$"..\Data\{userId}\IndexEntries.ldb";
            Directory.CreateDirectory(path);
            return new LiteDatabase(path);
        }

        public static LiteDatabase Notes(ObjectId userId)
        {
            var path = @$"..\Data\{userId}\Notes.ldb";
            Directory.CreateDirectory(path);
            return new LiteDatabase(path);
        }
    }
}
