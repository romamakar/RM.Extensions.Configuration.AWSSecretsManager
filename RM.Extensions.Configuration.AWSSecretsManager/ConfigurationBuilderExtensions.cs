using Amazon;
using RM.Extensions.Configuration.AWSSecretsManager.Models;
using Microsoft.Extensions.Configuration;

namespace RM.Extensions.Configuration.AWSSecretsManager
{
    public static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddSecretsManager(this IConfigurationBuilder builder, RegionEndpoint region, Action<SecretsManagerOptions> configurator)
        {
            return builder.Add(new AmazonSecretsManagerConfigurationSource(region, configurator));
        }
    }
}
