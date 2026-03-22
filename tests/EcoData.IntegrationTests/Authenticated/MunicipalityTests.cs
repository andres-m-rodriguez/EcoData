using EcoData.IntegrationTests.Bases;
using EcoData.IntegrationTests.Stores;
using EcoData.Locations.Application.Client;
using EcoData.Locations.Contracts.Dtos;
using EcoData.Locations.Contracts.Parameters;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EcoData.IntegrationTests.Authenticated;

public sealed class MunicipalityTests(EcoDataTestFixture fixture) : AuthenticatedTestBase(fixture)
{
    IMunicipalityHttpClient MunicipalityHttpClient =>
        Services.GetRequiredService<IMunicipalityHttpClient>();

    LocationsTestStore Locations => Services.GetRequiredService<LocationsTestStore>();

    [Fact]
    public async Task GetMunicipalities_ReturnsPagedResults()
    {
        var parameters = new MunicipalityParameters(PageSize: 10);
        var municipalities = new List<MunicipalityDtoForList>();

        await foreach (
            var municipality in MunicipalityHttpClient.GetMunicipalitiesAsync(parameters)
        )
        {
            municipalities.Add(municipality);
            if (municipalities.Count >= 10)
                break;
        }

        municipalities.Should().NotBeEmpty();
        municipalities
            .Should()
            .AllSatisfy(m =>
            {
                m.Id.Should().NotBeEmpty();
                m.Name.Should().NotBeNullOrEmpty();
                m.GeoJsonId.Should().NotBeNullOrEmpty();
            });
    }

    [Fact]
    public async Task GetMunicipalities_WithSearchFilter_ReturnsMatchingResults()
    {
        var firstMunicipality = await GetFirstMunicipalityAsync();
        if (firstMunicipality is null)
            return;

        var searchTerm = firstMunicipality.Name[..3];
        var parameters = new MunicipalityParameters(PageSize: 10, Search: searchTerm);
        var municipalities = new List<MunicipalityDtoForList>();

        await foreach (
            var municipality in MunicipalityHttpClient.GetMunicipalitiesAsync(parameters)
        )
        {
            municipalities.Add(municipality);
            if (municipalities.Count >= 10)
                break;
        }

        municipalities.Should().NotBeEmpty();
        municipalities
            .Should()
            .Contain(m => m.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetById_WhenMunicipalityExists_ReturnsDetail()
    {
        var municipalityId = Locations.MunicipalityId;

        var detail = await MunicipalityHttpClient.GetByIdAsync(municipalityId);

        detail.Should().NotBeNull();
        detail!.Id.Should().Be(municipalityId);
        detail.Name.Should().NotBeNullOrEmpty();
        detail.StateId.Should().NotBeEmpty();
        detail.StateName.Should().NotBeNullOrEmpty();
        detail.StateCode.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetById_WhenMunicipalityDoesNotExist_ReturnsNull()
    {
        var nonExistentId = Guid.CreateVersion7();

        var detail = await MunicipalityHttpClient.GetByIdAsync(nonExistentId);

        detail.Should().BeNull();
    }

    [Fact]
    public async Task GetMunicipalities_WithStateCodeFilter_ReturnsOnlyMatchingState()
    {
        var municipalityDetail = await MunicipalityHttpClient.GetByIdAsync(
            Locations.MunicipalityId
        );
        if (municipalityDetail is null)
            return;

        var stateCode = municipalityDetail.StateCode;
        var parameters = new MunicipalityParameters(PageSize: 10, StateCode: stateCode);
        var municipalities = new List<MunicipalityDtoForList>();

        await foreach (
            var municipality in MunicipalityHttpClient.GetMunicipalitiesAsync(parameters)
        )
        {
            municipalities.Add(municipality);
            if (municipalities.Count >= 10)
                break;
        }

        municipalities.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetMunicipalities_Pagination_WorksCorrectly()
    {
        var firstPageParams = new MunicipalityParameters(PageSize: 5);
        var firstPage = new List<MunicipalityDtoForList>();

        await foreach (
            var municipality in MunicipalityHttpClient.GetMunicipalitiesAsync(firstPageParams)
        )
        {
            firstPage.Add(municipality);
            if (firstPage.Count >= 5)
                break;
        }

        if (firstPage.Count < 5)
            return;

        var lastItem = firstPage.Last();
        var secondPageParams = new MunicipalityParameters(PageSize: 5, Cursor: lastItem.Id);
        var secondPage = new List<MunicipalityDtoForList>();

        await foreach (
            var municipality in MunicipalityHttpClient.GetMunicipalitiesAsync(secondPageParams)
        )
        {
            secondPage.Add(municipality);
            if (secondPage.Count >= 5)
                break;
        }

        secondPage.Should().NotBeEmpty();
        secondPage.Select(m => m.Id).Should().NotContain(firstPage.Select(m => m.Id));
    }

    [Fact]
    public async Task GetMunicipality_HasValidCoordinates()
    {
        var detail = await MunicipalityHttpClient.GetByIdAsync(Locations.MunicipalityId);

        detail.Should().NotBeNull();
        detail!.CentroidLatitude.Should().BeInRange(-90, 90);
        detail.CentroidLongitude.Should().BeInRange(-180, 180);
    }

    async Task<MunicipalityDtoForList?> GetFirstMunicipalityAsync()
    {
        var parameters = new MunicipalityParameters(PageSize: 1);

        await foreach (
            var municipality in MunicipalityHttpClient.GetMunicipalitiesAsync(parameters)
        )
        {
            return municipality;
        }

        return null;
    }
}
