using Amazon;
using Amazon.SecretsManager;
using Microsoft.Extensions.Configuration;

namespace RM.Extensions.Configuration.AWSSecretsManager.Models
{
    public class AmazonSecretsManagerConfigurationSource : IConfigurationSource
    {
        private readonly RegionEndpoint _region;
        private readonly Action<SecretsManagerOptions> _configure;

        public AmazonSecretsManagerConfigurationSource(RegionEndpoint region, Action<SecretsManagerOptions> configure)
        {
            _region = region;
            _configure = configure;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            var options = new SecretsManagerOptions();
            _configure(options);

            var client = new AmazonSecretsManagerClient(_region);

            return new AmazonSecretsManagerConfigurationProvider(client, options);
        }
    }
}
