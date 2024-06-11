using LiteDB;

namespace Simplistant_API.Models
{
    public abstract class DataItem
    {
        [BsonId]
        public ObjectId? Id { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class HasGuidKeyAttribute : Attribute
    {
        public readonly string KeyField;
        public readonly bool IsUnique;

        public HasGuidKeyAttribute(string keyField, bool isUnique = false)
        {
            KeyField = keyField;
            IsUnique = isUnique;
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class HasStringKeyAttribute : Attribute
    {
        public readonly string KeyField;
        public readonly bool IsUnique;

        public HasStringKeyAttribute(string keyField, bool isUnique = false)
        {
            KeyField = keyField;
            IsUnique = isUnique;
        }
    }
}
