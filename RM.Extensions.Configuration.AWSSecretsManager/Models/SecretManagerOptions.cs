using Amazon.SecretsManager.Model;

namespace RM.Extensions.Configuration.AWSSecretsManager.Models
{
    public class SecretsManagerOptions
    {
        public Func<SecretListEntry, bool>? SecretFilter { get; set; }
        public Func<SecretListEntry, string, string>? KeyGenerator { get; set; }
        public TimeSpan PollingInterval { get; set; } = TimeSpan.FromHours(1);
    }
}
