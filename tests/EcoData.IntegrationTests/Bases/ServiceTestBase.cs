using Xunit;

namespace EcoData.IntegrationTests.Bases;

[Collection(EcoDataTestCollection.Name)]
public abstract class ServiceTestBase(EcoDataTestFixture fixture)
{
    protected IServiceProvider DomainServices => fixture.DomainServices;
}
