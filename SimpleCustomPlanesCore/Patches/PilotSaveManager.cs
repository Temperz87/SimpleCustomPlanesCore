using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using UnityEngine;

[HarmonyPatch(typeof(PilotSaveManager), "EnsureVehicleCollections")]
public class Ensure_CustomVehicleCollections // C stop stealing my code
{
    public static bool Prefix()
    {
        Dictionary<string, PlayerVehicle> vehicles = Traverse.Create(typeof(PilotSaveManager)).Field("vehicles").GetValue() as Dictionary<string, PlayerVehicle>;
        if (vehicles == null)
        {
            Debug.Log("SCP: ensuring vehicle collections.");
            List<PlayerVehicle> vehicleList = new List<PlayerVehicle>();
            vehicles = new Dictionary<string, PlayerVehicle>();
            PlayerVehicleList playerVehicleList = (PlayerVehicleList)Resources.Load("PlayerVehicles");

            foreach (PlayerVehicle vehicle in playerVehicleList.playerVehicles)
            {
                vehicles.Add(vehicle.vehicleName, vehicle);
                vehicleList.Add(vehicle);
            }

            foreach (PlaneInformation info in PlaneInformation.planes)
            {
                if (!vehicles.ContainsKey(info.playerVehicle.vehicleName))
                    vehicles.Add(info.playerVehicle.vehicleName, info.playerVehicle);
                if (!vehicleList.Contains(info.playerVehicle))
                    vehicleList.Add(info.playerVehicle);
            }
            Traverse.Create(typeof(PilotSaveManager)).Field("vehicles").SetValue(vehicles);
            Traverse.Create(typeof(PilotSaveManager)).Field("vehicleList").SetValue(vehicleList);
        }
        return false;
    }
}

[HarmonyPatch(typeof(PilotSaveManager), nameof(PilotSaveManager.SavePilotsToFile))]
public static class Ensure_VehiclesSaved
{
    public static void Postfix()
    {
        CustomAircraftSaveManager.SaveVSaves();
    }
}

[HarmonyPatch(typeof(PilotSaveManager), nameof(PilotSaveManager.LoadPilotsFromFile))]
public static class Ensure_VehicleLoaded
{
    public static void Postfix()
    {
        CustomAircraftSaveManager.LoadVSaves();
    }
}
