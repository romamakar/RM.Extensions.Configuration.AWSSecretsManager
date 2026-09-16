using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;
using Moq;
using RM.Extensions.Configuration.AWSSecretsManager.Models;

namespace UnitTests;

public class AmazonSecretsManagerConfigurationProviderTests
{
    [Fact]
    public void Load_LoadsPlainTextAndFlattenedJsonSecretsAcrossPages()
    {
        var client = new Mock<IAmazonSecretsManager>(MockBehavior.Strict);
        var firstPage = new ListSecretsResponse
        {
            NextToken = "page-2",
            SecretList = new List<SecretListEntry>
            {
                new() { Name = "app/plain" }
            }
        };
        var secondPage = new ListSecretsResponse
        {
            SecretList = new List<SecretListEntry>
            {
                new() { Name = "app/json" }
            }
        };

        client
            .SetupSequence(x => x.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstPage)
            .ReturnsAsync(secondPage);

        client
            .Setup(x => x.GetSecretValueAsync(
                It.Is<GetSecretValueRequest>(request => request.SecretId == "app/plain"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetSecretValueResponse { SecretString = "plain-value" });

        client
            .Setup(x => x.GetSecretValueAsync(
                It.Is<GetSecretValueRequest>(request => request.SecretId == "app/json"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetSecretValueResponse
            {
                SecretString = "{\"ConnectionStrings\":{\"Default\":\"Server=db;Database=app;\"},\"Flags\":[true,false],\"Count\":3,\"Optional\":null}"
            });

        var provider = new AmazonSecretsManagerConfigurationProvider(
            client.Object,
            new SecretsManagerOptions { PollingInterval = TimeSpan.Zero });

        provider.Load();

        Assert.True(provider.TryGet("app/plain", out var plainValue));
        Assert.Equal("plain-value", plainValue);

        Assert.True(provider.TryGet("app/json:ConnectionStrings:Default", out var connectionString));
        Assert.Equal("Server=db;Database=app;", connectionString);

        Assert.True(provider.TryGet("app/json:Flags:0", out var firstFlag));
        Assert.Equal("True", firstFlag);

        Assert.True(provider.TryGet("app/json:Flags:1", out var secondFlag));
        Assert.Equal("False", secondFlag);

        Assert.True(provider.TryGet("app/json:Count", out var count));
        Assert.Equal("3", count);

        Assert.True(provider.TryGet("app/json:Optional", out var optional));
        Assert.Equal(string.Empty, optional);

        client.Verify(x => x.ListSecretsAsync(
            It.Is<ListSecretsRequest>(request => request.NextToken == null),
            It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(x => x.ListSecretsAsync(
            It.Is<ListSecretsRequest>(request => request.NextToken == "page-2"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Load_AppliesSecretFilterAndKeyGenerator()
    {
        var client = new Mock<IAmazonSecretsManager>(MockBehavior.Strict);
        var includedSecret = new SecretListEntry { Name = "prod/service" };
        var excludedSecret = new SecretListEntry { Name = "dev/service" };

        client
            .Setup(x => x.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListSecretsResponse
            {
                SecretList = new List<SecretListEntry> { includedSecret, excludedSecret }
            });

        client
            .Setup(x => x.GetSecretValueAsync(
                It.Is<GetSecretValueRequest>(request => request.SecretId == includedSecret.Name),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetSecretValueResponse { SecretString = "secret-value" });

        var provider = new AmazonSecretsManagerConfigurationProvider(
            client.Object,
            new SecretsManagerOptions
            {
                PollingInterval = TimeSpan.Zero,
                SecretFilter = secret => secret.Name.StartsWith("prod/", StringComparison.Ordinal),
                KeyGenerator = (_, key) => $"custom:{key.Replace('/', ':')}"
            });

        provider.Load();

        Assert.True(provider.TryGet("custom:prod:service", out var includedValue));
        Assert.Equal("secret-value", includedValue);
        Assert.False(provider.TryGet("custom:dev:service", out _));

        client.Verify(x => x.GetSecretValueAsync(
            It.Is<GetSecretValueRequest>(request => request.SecretId == includedSecret.Name),
            It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(x => x.GetSecretValueAsync(
            It.Is<GetSecretValueRequest>(request => request.SecretId == excludedSecret.Name),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void AddSecretsManager_AddsAmazonSecretsManagerConfigurationSource()
    {
        var builder = new ConfigurationBuilder();

        var result = builder.AddSecretsManager(configurator: options => options.PollingInterval = TimeSpan.Zero);

        Assert.Same(builder, result);
        var source = Assert.Single(builder.Sources);
        Assert.IsType<AmazonSecretsManagerConfigurationSource>(source);
    }
}
