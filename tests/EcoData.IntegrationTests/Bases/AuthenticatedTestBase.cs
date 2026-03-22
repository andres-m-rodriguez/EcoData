using Aspire.Hosting;
using Xunit;

namespace EcoData.IntegrationTests.Bases;

[Collection(EcoDataTestCollection.Name)]
public abstract class AuthenticatedTestBase(EcoDataTestFixture fixture)
{
    protected IServiceProvider Services => fixture.Services;
    protected DistributedApplication App => fixture.App;
}
