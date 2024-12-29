namespace ToggleX.Exceptions
{
    public class FeatureEvaluationException : Exception
    {
        public string FeatureName { get; }

        public FeatureEvaluationException(string featureName, string message, Exception innerException = null)
            : base(message, innerException)
        {
            FeatureName = featureName;
        }
    }

    public class ConditionEvaluationException : Exception
    {
        public string Condition { get; }
        public string ContextDetails { get; }

        public ConditionEvaluationException(string condition, string contextDetails, string message)
            : base(message)
        {
            Condition = condition;
            ContextDetails = contextDetails;
        }

        public ConditionEvaluationException(string condition, string contextDetails, string message, Exception innerException)
            : base(message, innerException)
        {
            Condition = condition;
            ContextDetails = contextDetails;
        }
    }
}
