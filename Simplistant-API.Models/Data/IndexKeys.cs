using Simplistant_API.Repository;

namespace Simplistant_API.Models.Data
{
    [HasStringKey("Key", isUnique: true)]
    public class IndexKeys : DataItem
    {
        public string Key { get; set; }
    }
}
