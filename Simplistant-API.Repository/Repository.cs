using System.Linq.Expressions;
using System.Reflection;
using LiteDB;

namespace Simplistant_API.Repository
{
    internal class Repository<T> : IRepository<T>
        where T : DataItem
    {
        private LiteDatabase _Instance { get; }

        internal Repository(LiteDatabase _instance)
        {
            _Instance = _instance;

            //Ensure indices
            //Done via metadata/attributes because it's the cleanest solution
            var typeMetadata = Metadata.Get<T>();
            if (typeMetadata.HasGuidKey)
            {
                _Instance.GetCollection<T>().EnsureIndex(typeMetadata.GuidKeyField, typeMetadata.GuidKeyIsUnique);
            }
            if (typeMetadata.HasStringKey)
            {
                _Instance.GetCollection<T>().EnsureIndex(typeMetadata.StringKeyField, typeMetadata.StringKeyIsUnique);
            }
        }

        public List<T> GetWhere(Expression<Func<T, bool>> filter)
        {
            return _Instance.GetCollection<T>()
                .Find(filter)
                .ToList();
        }

        public void RemoveWhere(Expression<Func<T, bool>> filter)
        {
            _Instance.GetCollection<T>()
                .DeleteMany(filter);
        }

        public bool Upsert(T item)
        {
            if (item.Id == null)
            {
                item.Id = ObjectId.NewObjectId();
            }

            _Instance.GetCollection<T>()
                .Upsert(item);

            //If it fails an exception will be thrown
            //User will see an error page and it will be logged upstream
            return true;
        }
    }

    internal class DataItemMetadata
    {
        public bool HasGuidKey { get; set; } = false;
        public string GuidKeyField { get; set; } = "";
        public bool GuidKeyIsUnique { get; set; } = false;
        public bool HasStringKey { get; set; } = false;
        public string StringKeyField { get; set; } = "";
        public bool StringKeyIsUnique { get; set; } = false;
    }

    internal static class Metadata
    {
        internal static DataItemMetadata Get<T>()
        {
            var metaData = new DataItemMetadata();

            var type = typeof(T);

            var GuidKeyAttribute = type.GetCustomAttribute<HasGuidKeyAttribute>();
            if (GuidKeyAttribute != null)
            {
                metaData.HasGuidKey = true;
                metaData.GuidKeyField = GuidKeyAttribute.KeyField;
                metaData.GuidKeyIsUnique = GuidKeyAttribute.IsUnique;
            }

            var StringKeyAttribute = type.GetCustomAttribute<HasStringKeyAttribute>();
            if (StringKeyAttribute != null)
            {
                metaData.HasStringKey = true;
                metaData.StringKeyField = StringKeyAttribute.KeyField;
                metaData.StringKeyIsUnique = StringKeyAttribute.IsUnique;
            }

            return metaData;
        }
    }
}
