using Amazon;
using Amazon.Runtime;
using Amazon.SecretsManager;
using Microsoft.Extensions.Configuration;
using System;

namespace RM.Extensions.Configuration.AWSSecretsManager.Models
{
    public class AmazonSecretsManagerConfigurationSource : IConfigurationSource
    {
        private readonly RegionEndpoint _region;
        private readonly AWSCredentials _credentials;
        private readonly Action<SecretsManagerOptions> _configure;

        public AmazonSecretsManagerConfigurationSource(AWSCredentials credentials, RegionEndpoint region, Action<SecretsManagerOptions> configure)
        {
            _region = region;
            _configure = configure;
            _credentials= credentials;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            var options = new SecretsManagerOptions();
            
            _configure?.Invoke(options);

            AmazonSecretsManagerClient client;
            if (_region == null)
            {
                client = _credentials == null
                    ? new AmazonSecretsManagerClient()
                    : new AmazonSecretsManagerClient(_credentials);
            }
            else
            {
                client = _credentials == null
                    ? new AmazonSecretsManagerClient(_region)
                    : new AmazonSecretsManagerClient(_credentials, _region);
            }               

            return new AmazonSecretsManagerConfigurationProvider(client, options);
        }
    }
}
