using System.Linq.Dynamic.Core;
using System.Text;
using ToggleX.Context;
using ToggleX.Exceptions;
using ToggleX.Providers;

namespace ToggleX
{
    public class FeatureManager
    {
        private readonly IFeatureProvider _featureProvider;

        public FeatureManager(IFeatureProvider featureProvider)
        {
            _featureProvider = featureProvider;
        }

        public bool IsFeatureEnabled(string featureName, FeatureContext context)
        {
            try
            {
                var feature = _featureProvider.GetFeatureFlags().FirstOrDefault(f => f.Name == featureName);

                if (feature == null)
                {
                    throw new FeatureEvaluationException(featureName, $"Feature '{featureName}' not found");
                }

                var missingDependencies = GetMissingDependencies(feature.DependsOn);
                if (missingDependencies.Any())
                {
                    throw new FeatureEvaluationException(featureName, $"Dependencies for feature '{featureName}' are not satisfied. Missing dependencies: {string.Join(", ", missingDependencies)}");
                }

                if (!feature.IsEnabled)
                {
                    throw new FeatureEvaluationException(featureName, $"'{featureName}' is disabled");
                }

                if (feature.Rules != null && context != null)
                {
                    foreach (var rule in feature.Rules)
                    {
                        if (EvaluateCondition(rule.Condition, context))
                        {
                            return rule.IsEnabled;
                        }
                    }
                }

                return feature.IsEnabled;
            }
            catch (Exception ex)
            {
                throw new FeatureEvaluationException(featureName, $"Error during feature evaluation for '{featureName}': {ex.Message}", ex);
            }
        }

        private bool EvaluateCondition(string condition, FeatureContext context)
        {
            try
            {
                condition = condition.Replace("'", "\"");

                var contextDetails = GetContextDetails(context);

                var result = DynamicExpressionParser
                    .ParseLambda<FeatureContext, bool>(null, true, condition)
                    .Compile()
                    .Invoke(context);

                if (!result)
                {
                    throw new ConditionEvaluationException(condition, contextDetails, $"\n\tCondition failed: {condition}, \n\tContext Details: {contextDetails}");
                }

                return result;
            }
            catch (Exception ex)
            {
                var contextDetails = context != null ? GetContextDetails(context) : "No context details available (context is null)";
                throw new ConditionEvaluationException(condition, contextDetails, ex.Message, ex);
            }
        }

        private string GetContextDetails(FeatureContext context)
        {
            var contextDetails = new StringBuilder();

            if (context != null)
            {
                foreach (var prop in context.GetType().GetProperties())
                {
                    var value = prop.GetValue(context);

                    if (value is string)
                    {
                        contextDetails.Append($"{prop.Name}: \"{value}\", ");
                        continue;
                    }

                    contextDetails.Append($"{prop.Name}: {value}, ");
                }
            }

            return contextDetails.ToString();
        }

        private List<string> GetMissingDependencies(List<string> dependencies)
        {
            var missingDependencies = new List<string>();

            foreach (var dependency in dependencies)
            {
                var dependencyFeature = _featureProvider.GetFeatureFlags().FirstOrDefault(f => f.Name == dependency);
                if (dependencyFeature == null || !dependencyFeature.IsEnabled)
                {
                    missingDependencies.Add(dependency);
                }
            }

            return missingDependencies;
        }
    }
}

