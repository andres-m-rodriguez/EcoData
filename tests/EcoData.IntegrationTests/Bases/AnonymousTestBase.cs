using Xunit;

namespace EcoData.IntegrationTests.Bases;

[Collection(EcoDataTestCollection.Name)]
public abstract class AnonymousTestBase(EcoDataTestFixture fixture)
{
    protected IServiceProvider Services => fixture.Services;
}
