using Amazon;
using Amazon.Runtime;
using RM.Extensions.Configuration.AWSSecretsManager.Models;
using System;

namespace Microsoft.Extensions.Configuration
{
    public static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddSecretsManager(this IConfigurationBuilder builder, AWSCredentials credentials = null, RegionEndpoint region = null, Action<SecretsManagerOptions> configurator = null)
        {
            return builder.Add(new AmazonSecretsManagerConfigurationSource(credentials, region, configurator));
        }
    }
}
