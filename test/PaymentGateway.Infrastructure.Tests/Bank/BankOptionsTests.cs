using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using PaymentGateway.Infrastructure.Bank;

namespace PaymentGateway.Infrastructure.Tests.Bank;

public sealed class BankOptionsTests
{
    [Fact]
    public void BindsBankSection()
    {
        var options = Resolve(new()
        {
            ["Bank:BaseUrl"] = "http://bank:8080",
            ["Bank:AttemptTimeout"] = "00:00:05",
            ["Bank:TotalTimeout"] = "00:00:20"
        });

        Assert.Equal("http://bank:8080", options.BaseUrl);
        Assert.Equal(TimeSpan.FromSeconds(5), options.AttemptTimeout);
        Assert.Equal(TimeSpan.FromSeconds(20), options.TotalTimeout);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a url")]
    public void RejectsMissingOrInvalidBaseUrl(string? baseUrl)
    {
        Assert.Throws<OptionsValidationException>(() => Resolve(new() { ["Bank:BaseUrl"] = baseUrl }));
    }

    [Fact]
    public void RejectsAttemptTimeoutLongerThanTotalTimeout()
    {
        Assert.Throws<OptionsValidationException>(() => Resolve(new()
        {
            ["Bank:BaseUrl"] = "http://bank:8080",
            ["Bank:AttemptTimeout"] = "00:01:00",
            ["Bank:TotalTimeout"] = "00:00:30"
        }));
    }

    private static BankOptions Resolve(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        using var provider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddLogging()
            .AddInfrastructure()
            .BuildServiceProvider();

        return provider.GetRequiredService<IOptions<BankOptions>>().Value;
    }
}
