namespace ToggleX.Models
{
    public class FeatureFlag
    {
        public string Name { get; set; }

        public bool IsEnabled { get; set; }

        public bool DefaultEnabled { get; set; }

        public List<string> DependsOn { get; set; } = new();

        public List<FeatureRule> Rules { get; set; } = new();
    }
}
