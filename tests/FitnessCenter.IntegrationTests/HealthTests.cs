using System.Net;

namespace FitnessCenter.IntegrationTests;

[TestClass]
public sealed class HealthTests
{
    [TestMethod]
    public async Task Api_is_healthy_when_the_database_is_reachable()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
    }
}
