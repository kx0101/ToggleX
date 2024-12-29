# ToggleX - Feature Flag Library

**ToggleX** is a flexible and powerful feature flag management library for .NET applications. It allows developers to ***enable*** or ***disable*** features in their application dynamically based on certain conditions, ***without deploying new code***. Which makes it useful for controlling beta features, conducting A/B testing, rolling out features incrementally, or temporarily disabling certain functionalities in production environments.

### Key Features
- **Dynamic Feature Management**: Toggle features on or off based on conditions 
- **Customizable Context**: Users can define their own **feature context classes** to determine feature eligibility
- **Μultiple Feature Sources**: Supports JSON-based configuration for feature flag management
- **Integration with ASP.NET Core**: Easily integrates with ASP.NET Core applications via ***Dependency Injection*** and ***Action Filters***

### How It Works

The core functionality of the ToggleX library revolves around managing feature flags based on conditions defined within feature configuration files (such as JSON files). The library allows you to:

- **Define Features**: Features are enabled or disabled based on specific conditions
- **Use Context**: Conditions for enabling/disabling features can depend on user-specific or application-specific context (e.g., user role, user location, or other custom data)
- **Evaluate Conditions**: Conditions can be evaluated dynamically using expressions (e.g., evaluating whether a user has access to a certain feature)

### Architecture

The core components of the library are:

- `FeatureManager`: The main component that checks whether a feature is enabled or not based on provided 
- `FeatureProvider`: Provides the feature flag configuration. This can be a JSON provider, or other custom providers
- `FeatureContext`: A base context class that can be implemented by the user to represent various data points needed to evaluate conditions for a feature flag

# Setup and Configuration
### 1. Install the Library

You can install the ToggleX library via NuGet Package Manager or .NET CLI.
`dotnet add package ToggleX`

### 2. Feature Configuration (using JSON)

- `Name`: The name of the feature flag
- `IsEnabled`: A boolean value indicating if the feature is enabled or disabled
- `DependsOn`: A list of other feature flags that this feature depends on. The feature will only be enabled if all the dependencies are ***enabled***.
- `Rules`: A list of conditions that further define when the feature flag should be enabled, based on the context provided. Each rule has:
    - `Condition`: A condition expression that evaluates whether the feature should be enabled (e.g. `UserRole == 'Admin'`)
    - `IsEnabled`: A boolean value indicating whether the feature should be enabled

```json
[
    {
        "Name": "AdvancedSearch",
        "IsEnabled": true,
        "DependsOn": ["SearchEngine"],
        "Rules": [
            {
                "Condition": "UserRole == 'Admin' && UserId == 42 && UserLocation == 'GR'",
                "IsEnabled": true
            }
        ]
    },
    {
        "Name": "SearchEngine",
        "IsEnabled": true,
        "DependsOn": []
    }
]
```

### 3. Registering Feature Provider and Manager

In your **Program.cs** or **Startup.cs**, register the `FeatureManager` (in this case using `JsonFeatureProvider`):

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new FeatureManager(new JsonFeatureProvider("features.json"));
    }
}
```

# Web Application Example

Imagine you have the same JSON configuration as above in our example. After registering the `FeatureManager`, you might want to create a custom context class:

```csharp
public class CustomFeatureContext : IFeatureContext
{
    public CustomFeatureContext(string userRole, int userId, string userLocation)
    {
        UserRole = userRole;
        UserId = userId;
        UserLocation = userLocation;
    }

    public string UserRole { get; set; }
    public int UserId { get; set; }
    public string UserLocation { get; set; }
}
```

Make sure to also register it in your services:

```csharp
builder.Services.AddScoped<CustomFeatureContext>(provider => new CustomFeatureContext("Admin", 42, "GR"));
```

Then, you might want to create your own custom attribute:

```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class FeatureFlagAttribute : Attribute, IActionFilter
{
    private readonly string _featureName;
    private readonly Type _featureContextType;

    public FeatureFlagAttribute(string featureName, Type featureContextType)
    {
        _featureName = featureName;
        _featureContextType = featureContextType;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var featureManager = context.HttpContext.RequestServices.GetService<FeatureManager>();

        try
        {
            IFeatureContext featureContext = (IFeatureContext)context.HttpContext.RequestServices.GetRequiredService(_featureContextType);

            var isFeatureEnabled = featureManager.IsFeatureEnabled(_featureName, featureContext);

            if (!isFeatureEnabled)
            {
                context.Result = new ContentResult
                {
                    Content = "Feature is disabled..\",
                    StatusCode = 403
                };
            }
        }
        catch (FeatureEvaluationException ex)
        {
            var requestDetails = GetRequestDetails(context.HttpContext.Request);
            Log.Error($"on request: {requestDetails} \n{ex.Message}");

            context.Result = new ContentResult
            {
                Content = "Feature is disabled.\n",
                StatusCode = 403
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"error occurred: {ex.Message}");
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
```

Which then can be used in **ANY** Class or Method:

```csharp
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    [FeatureFlag("AdvancedSearch", typeof(CustomFeatureContext))]
    public IActionResult Index()
    {
        return View();
    }
}
```

# Error Handling in ToggleX

The **ToggleX** library includes detailed error handling for various situations where feature evaluation might fail. Below are examples of common error scenarios, as well as how they will be represented in the application:

### 1. Conditions Do Not Match with the Context
This error occurs when the conditions defined for a feature flag do not match the current context. For example, if a feature flag is enabled only for users with a specific role or location, but the current context doesn't match, the evaluation will fail

![Conditions Do Not Match with the Context](https://raw.githubusercontent.com/kx0101/ToggleX/main/Images/condition-error.png)

### 2. Feature is Disabled
This error occurs when a feature flag is explicitly disabled in the configuration but the application attempts to evaluate it as if it's enabled

![Feature is Disabled](https://raw.githubusercontent.com/kx0101/ToggleX/main/Images/disabled-feature.png)

### 3. Dependencies for Feature Are Not Satisfied
This error occurs when a feature has dependencies (e.g., other features that must be enabled for this one to work) and those dependencies are not satisfied. For example, if the `AdvancedSearch` feature requires a `SearchEngine` feature to be enabled, but `SearchEngine` is disabled, the evaluation **will fail**

![Missing Dependencies](https://raw.githubusercontent.com/kx0101/ToggleX/main/Images/dependency-error.png)

### 4. Rule for This Feature is Disabled
This error occurs when a feature rule is explicitly disabled in the configuration. For example, a feature flag may be designed to be dynamically enabled/disabled based on business logic, and the rule itself could be disabled under certain circumstances.

![Rule Disabled](https://raw.githubusercontent.com/kx0101/ToggleX/main/Images/rule-disabled.png)
