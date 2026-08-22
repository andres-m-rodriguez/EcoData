using EcoData.Common.Authorization;
using EcoData.Sensors.Contracts;

namespace EcoData.Sensors.Application.Client;

// The type picks the scope. Call sites reference these fields; nobody types a key twice.
public static class SensorPermissions
{
    public static readonly OrgPermission ReadSensor = new(Permissions.Sensor.Read);

    public static readonly OrgPermission CreateSensor = new(Permissions.Sensor.Create);

    public static readonly OrgPermission UpdateSensor = new(Permissions.Sensor.Update);

    public static readonly OrgPermission DeleteSensor = new(Permissions.Sensor.Delete);
}
