using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace RM.Extensions.Configuration.AWSSecretsManager.Models
{
    public class AmazonSecretsManagerConfigurationProvider : ConfigurationProvider, IDisposable
    {
        private readonly IAmazonSecretsManager _client;
        private readonly SecretsManagerOptions _options;
        private Timer? _timer;

        public AmazonSecretsManagerConfigurationProvider(IAmazonSecretsManager client, SecretsManagerOptions options)
        {
            _client = client;
            _options = options;
        }

        public override void Load()
        {
            LoadSecrets();
            if (_options.PollingInterval > TimeSpan.Zero)
            {
                _timer ??= new Timer(_ => ReloadSecrets(), null,
                    _options.PollingInterval,
                    _options.PollingInterval);
            }
        }

        private void ReloadSecrets()
        {
            try
            {
                LoadSecrets();
                OnReload();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SecretsManager] Error reloading secrets: {ex.Message}");
            }
        }

        private void LoadSecrets()
        {
            var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            string? nextToken = null;
            do
            {
                var request = new ListSecretsRequest
                {
                    NextToken = nextToken
                };
                var listResponse = Task.Run(() => _client.ListSecretsAsync(request)).GetAwaiter().GetResult();

                foreach (var secret in listResponse.SecretList)
                {
                    if (_options.SecretFilter != null && !_options.SecretFilter(secret))
                        continue;

                    var getResponse = Task.Run(() => _client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secret.Name })).GetAwaiter().GetResult();

                    var keyPrefix = _options.KeyGenerator != null ? _options.KeyGenerator(secret, secret.Name) : secret.Name;


                    if (!string.IsNullOrEmpty(getResponse.SecretString))
                    {
                        if (IsJson(getResponse.SecretString))
                        {
                            var doc = JsonDocument.Parse(getResponse.SecretString);
                            Flatten(doc.RootElement, keyPrefix, data);
                        }
                        else
                        {
                            data[keyPrefix] = getResponse.SecretString;
                        }
                    }
                }
                nextToken = listResponse.NextToken;
            } while (!string.IsNullOrEmpty(nextToken));

            Data = data;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private static bool IsJson(string input)
        {
            input = input.Trim();
            return (input.StartsWith("{") && input.EndsWith("}")) ||
                   (input.StartsWith("[") && input.EndsWith("]"));
        }


        private static void Flatten(JsonElement element, string prefix, IDictionary<string, string?> data)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var prop in element.EnumerateObject())
                    {
                        var key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}:{prop.Name}";
                        Flatten(prop.Value, key, data);
                    }
                    break;

                case JsonValueKind.Array:
                    int i = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        Flatten(item, $"{prefix}:{i}", data);
                        i++;
                    }
                    data[prefix] = element.GetString()!;
                    break;

                case JsonValueKind.String:
                    data[prefix] = element.GetString()!;
                    break;

                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                    data[prefix] = element.ToString()!;
                    break;

                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    data[prefix] = string.Empty;
                    break;
            }
        }
    }
}
