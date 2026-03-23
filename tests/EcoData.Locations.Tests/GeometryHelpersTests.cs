using EcoData.Locations.Helpers;
using FluentAssertions;
using Xunit;

namespace EcoData.Locations.Tests;

public sealed class GeometryHelpersTests
{
    [Fact]
    public void CreatePoint_WithValidCoordinates_ReturnsPoint()
    {
        // Arrange
        var latitude = 18.4655m;
        var longitude = -66.1057m;

        // Act
        var point = GeometryHelpers.CreatePoint(latitude, longitude);

        // Assert
        point.Should().NotBeNull();
        point!.X.Should().BeApproximately((double)longitude, 0.0001);
        point.Y.Should().BeApproximately((double)latitude, 0.0001);
        point.SRID.Should().Be(4326);
    }

    [Fact]
    public void CreatePoint_WithZeroCoordinates_ReturnsNull()
    {
        // Arrange & Act
        var point = GeometryHelpers.CreatePoint(0, 0);

        // Assert
        point.Should().BeNull();
    }

    [Fact]
    public void CreatePolygon_WithValidCoordinates_ReturnsPolygon()
    {
        // Arrange - A simple square
        var coordinates = new[]
        {
            new[] { 0.0, 0.0 },
            new[] { 1.0, 0.0 },
            new[] { 1.0, 1.0 },
            new[] { 0.0, 1.0 },
            new[] { 0.0, 0.0 } // Close the ring
        };

        // Act
        var polygon = GeometryHelpers.CreatePolygon(coordinates);

        // Assert
        polygon.Should().NotBeNull();
        polygon.IsValid.Should().BeTrue();
        polygon.NumPoints.Should().Be(5);
        polygon.SRID.Should().Be(4326);
    }

    [Fact]
    public void GeometryFactory_HasCorrectSrid()
    {
        // Assert
        GeometryHelpers.GeometryFactory.SRID.Should().Be(4326);
    }
}
