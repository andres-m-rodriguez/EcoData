using EcoData.Locations.Helpers;
using EcoData.Sensors.Database;
using EcoData.Sensors.Database.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace EcoData.Sensors.Api.Endpoints;

public static class GeoTestEndpoints
{
    public static IEndpointRouteBuilder MapGeoTestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/sensors/geo-test").WithTags("Geo Test");

        // Test 1: Read a sensor's Location (Point geometry)
        group
            .MapGet(
                "/read",
                async (IDbContextFactory<SensorsDbContext> contextFactory, CancellationToken ct) =>
                {
                    try
                    {
                        await using var context = await contextFactory.CreateDbContextAsync(ct);

                        var sensor = await context
                            .Sensors.Where(s => s.Location != null)
                            .Select(s => new
                            {
                                s.Id,
                                s.Name,
                                s.Latitude,
                                s.Longitude,
                                LocationX = s.Location!.X,
                                LocationY = s.Location!.Y,
                                LocationSrid = s.Location!.SRID,
                            })
                            .FirstOrDefaultAsync(ct);

                        if (sensor is null)
                        {
                            return Results.Ok(
                                new
                                {
                                    Success = true,
                                    Message = "No sensors with Location found, but query succeeded",
                                }
                            );
                        }

                        return Results.Ok(
                            new
                            {
                                Success = true,
                                Message = "Read Point geometry successful",
                                Data = sensor,
                            }
                        );
                    }
                    catch (Exception ex)
                    {
                        return Results.Ok(
                            new
                            {
                                Success = false,
                                Message = "Read failed",
                                Error = ex.Message,
                                InnerError = ex.InnerException?.Message,
                            }
                        );
                    }
                }
            )
            .WithName("GeoTestRead");

        // Test 2: Write a Point geometry (creates and deletes a test sensor)
        group
            .MapPost(
                "/write",
                async (IDbContextFactory<SensorsDbContext> contextFactory, CancellationToken ct) =>
                {
                    try
                    {
                        await using var context = await contextFactory.CreateDbContextAsync(ct);

                        // Create a test sensor with Point geometry
                        var testSensorId = Guid.CreateVersion7();
                        var now = DateTimeOffset.UtcNow;
                        var testPoint = GeometryHelpers.CreatePoint(19.4326m, -99.1332m); // Mexico City

                        var testSensor = new Sensor
                        {
                            Id = testSensorId,
                            OrganizationId = Guid.Empty, // Dummy
                            SourceId = null,
                            ExternalId = $"geo-test-{testSensorId}",
                            Name = "Geo Test Sensor",
                            Latitude = 19.4326m,
                            Longitude = -99.1332m,
                            Location = testPoint,
                            MunicipalityId = Guid.Empty, // Dummy
                            IsActive = false,
                            ReportingMode = ReportingMode.Push,
                            SensorTypeId = null,
                            CreatedAt = now,
                            UpdatedAt = now,
                        };

                        context.Sensors.Add(testSensor);
                        await context.SaveChangesAsync(ct);

                        // Clean up - delete the test sensor
                        context.Sensors.Remove(testSensor);
                        await context.SaveChangesAsync(ct);

                        return Results.Ok(
                            new
                            {
                                Success = true,
                                Message = "Write Point geometry successful",
                                TestSensorId = testSensorId,
                                PointX = testPoint?.X,
                                PointY = testPoint?.Y,
                                PointSrid = testPoint?.SRID,
                            }
                        );
                    }
                    catch (Exception ex)
                    {
                        return Results.Ok(
                            new
                            {
                                Success = false,
                                Message = "Write failed",
                                Error = ex.Message,
                                InnerError = ex.InnerException?.Message,
                                StackTrace = ex.StackTrace,
                            }
                        );
                    }
                }
            )
            .WithName("GeoTestWrite");

        return app;
    }
}
