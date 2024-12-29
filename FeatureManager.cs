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

        public bool IsFeatureEnabled(string featureName, IFeatureContext context)
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
                        if (EvaluateCondition(rule.IsEnabled, rule.Condition, context))
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

        private bool EvaluateCondition(bool isEnabled, string condition, IFeatureContext context)
        {
            condition = condition.Replace("'", "\"");

            var contextDetails = GetContextDetails(context);

            if (!isEnabled)
            {
                throw new ConditionEvaluationException($"\n\trule: {condition} for this feature is disabled");
            }

            try
            {
                var result = DynamicExpressionParser
                    .ParseLambda(context.GetType(), typeof(bool), condition)
                    .Compile()
                    .DynamicInvoke(context);

                if (result is bool resultBool && !resultBool)
                {
                    throw new ConditionEvaluationException(condition, contextDetails, $"\n\tCondition failed: {condition}, \n\tContext Details: {contextDetails}");
                }

                return true;
            }
            catch (Exception ex)
            {
                contextDetails = context != null ? GetContextDetails(context) : "No context details available (context is null)";
                throw new ConditionEvaluationException(condition, contextDetails, ex.Message, ex);
            }
        }

        private string GetContextDetails(IFeatureContext context)
        {
            var contextDetails = new StringBuilder();

            if (context != null)
            {
                var properties = context.GetType().GetProperties();

                foreach (var prop in properties)
                {
                    var value = prop.GetValue(context);

                    if (value is string)
                    {
                        contextDetails.Append($"{prop.Name}: \"{value}\", ");
                    }
                    else
                    {
                        contextDetails.Append($"{prop.Name}: {value}, ");
                    }
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

