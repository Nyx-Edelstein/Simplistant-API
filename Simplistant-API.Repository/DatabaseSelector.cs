using LiteDB;

namespace Simplistant_API.Repository
{
    public static class DatabaseSelector
    {
        public static LiteDatabase System { get; } = new LiteDatabase(@"..\System.ldb");
        public static LiteDatabase Users { get; } = new LiteDatabase(@"..\Users.ldb");
        public static LiteDatabase Data { get; } = new LiteDatabase(@"..\Data.ldb");
    }
}
