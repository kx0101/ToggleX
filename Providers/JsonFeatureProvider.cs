using Newtonsoft.Json;
using ToggleX.Models;

namespace ToggleX.Providers
{
    public class JsonFeatureProvider : IFeatureProvider
    {
        private readonly string _filePath;

        public JsonFeatureProvider(string filePath)
        {
            _filePath = filePath;
        }

        public List<FeatureFlag> GetFeatureFlags()
        {
            var jsonContent = File.ReadAllText(_filePath);
            return JsonConvert.DeserializeObject<List<FeatureFlag>>(jsonContent);
        }
    }
}
