using Amazon;
using Amazon.Runtime;
using Amazon.SecretsManager;
using Microsoft.Extensions.Configuration;
using RM.Extensions.Configuration.AWSSecretsManager.Models;
using System.Reflection;

namespace UnitTests;

public class AmazonSecretsManagerConfigurationSourceTests
{
    [Fact]
    public void Build_ReturnsProviderWithConfiguredOptions()
    {
        var source = new AmazonSecretsManagerConfigurationSource(
            credentials: null,
            region: null,
            configure: options =>
            {
                options.PollingInterval = TimeSpan.FromMinutes(5);
                options.KeyGenerator = (_, key) => $"prefix:{key}";
            });

        var provider = source.Build(new ConfigurationBuilder());

        var secretsProvider = Assert.IsType<AmazonSecretsManagerConfigurationProvider>(provider);
        var options = GetPrivateField<SecretsManagerOptions>(secretsProvider, "_options");

        Assert.Equal(TimeSpan.FromMinutes(5), options?.PollingInterval);
        Assert.NotNull(options?.KeyGenerator);
        Assert.Equal("prefix:sample", options?.KeyGenerator(null, "sample"));
    }

    [Fact]
    public void Build_WithoutConfigurator_UsesDefaultOptions()
    {
        var source = new AmazonSecretsManagerConfigurationSource(
            credentials: null,
            region: null,
            configure: null);

        var provider = source.Build(new ConfigurationBuilder());

        var secretsProvider = Assert.IsType<AmazonSecretsManagerConfigurationProvider>(provider);
        var options = GetPrivateField<SecretsManagerOptions>(secretsProvider, "_options");

        Assert.Equal(TimeSpan.FromHours(1), options?.PollingInterval);
        Assert.Null(options?.SecretFilter);
        Assert.Null(options?.KeyGenerator);
    }

    [Fact]
    public void Build_WithRegion_CreatesClientUsingThatRegion()
    {
        var source = new AmazonSecretsManagerConfigurationSource(
            credentials: new AnonymousAWSCredentials(),
            region: RegionEndpoint.EUCentral1,
            configure: null);

        var provider = source.Build(new ConfigurationBuilder());

        var secretsProvider = Assert.IsType<AmazonSecretsManagerConfigurationProvider>(provider);
        var client = GetPrivateField<IAmazonSecretsManager>(secretsProvider, "_client");
        var amazonClient = Assert.IsType<AmazonSecretsManagerClient>(client);

        Assert.Equal(RegionEndpoint.EUCentral1.SystemName, amazonClient.Config.RegionEndpoint.SystemName);
    }

    private static T? GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(field);

        return (T?)field.GetValue(instance);
    }
}
