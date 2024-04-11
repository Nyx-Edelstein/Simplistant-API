using Simplistant_API.Repository;

namespace Simplistant_API.Models.System
{
    [HasStringKey("Key", isUnique: true)]
    public class ConfigItem : DataItem
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}