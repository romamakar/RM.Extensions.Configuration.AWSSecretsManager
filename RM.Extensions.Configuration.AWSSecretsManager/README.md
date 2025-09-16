# Extensions.Configuration.AWSSecretsManager

This project provides an extension for integrating AWS Secrets Manager with the .NET configuration system. It allows you to load secrets from AWS Secrets Manager and use them as configuration values in your application.
Usage:

1. Install the NuGet package:
   ```
   dotnet add package RM.Extensions.Configuration.AWSSecretsManager
   ```
2. Configure AWS Secrets Manager in your application:

3. Add this extension method to your configuration builder:
   ```csharp
   var builder = new ConfigurationBuilder().AddSecretsManager();
   var configuration = builder.Build();
   ```
4. Customize the configuration options as needed:
   ```csharp
   var builder = new ConfigurationBuilder().AddSecretsManager(options =>
   {
	   options.SecretFilter = entry => entry.Name.StartsWith($"prod/");
       options.KeyGenerator = (_, s) =>
                    {
                        var key = s.Replace($"prod/", "");
                        var keyUpper = char.ToUpper(key[0]) + key.Substring(1);
                        return keyUpper;
                    };
      options.PollingInterval = TimeSpan.FromHours(1);
   });
   var configuration = builder.Build();
   ```
5. You can use credentials from the default AWS SDK credential chain or provide custom credentials:
   ```csharp
   var builder = new ConfigurationBuilder().AddSecretsManager(new BasicAWSCredentials("your-access-key", "your-secret-key"), RegionEndpoint.USEast1;);
   var configuration = builder.Build();
   ```

6. Use this extension method on builder.Configuration:
   ```csharp
        builder.Configuration.AddSecretsManager();
   ```