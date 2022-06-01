using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Harmony;
using VTOLVR.Multiplayer;
using System.IO;

public class Main : VTOLMOD
{
    public static Main instance;
    public static PlaneInformation selectedPlane = null;
    //public static Texture texture;

    public void Start()
    {
        instance = this;
    }

    public override void ModLoaded()
    {
        HarmonyInstance.Create("tempy.SCP.core").PatchAll();
        VTOLAPI.SceneLoaded += (scene) =>
        {
            if (scene == VTOLScenes.ReadyRoom)
                selectedPlane = null;
        };

        // I stole this code from csa lmao
        Debug.Log("Searching for .plane files in the mod folder");
        string address = Directory.GetCurrentDirectory();
        Debug.Log("Checking for: " + address);
        if (Directory.Exists(address))
        {
            Debug.Log(address + " exists!");
            DirectoryInfo info = new DirectoryInfo(address);

            Debug.Log("Searching " + address + info.Name + " for .plane");
            foreach (FileInfo file in info.GetFiles("*.plane", SearchOption.AllDirectories))
            {
                Debug.Log("Found " + file.FullName);
                StartCoroutine(asyncLoad(file.DirectoryName, file.Name));
            }
        }
        else
        {
            Debug.Log(address + " doesn't exist.");
        }

        base.ModLoaded();
    }

    private IEnumerator asyncLoad(string directory, string bundleName)
    {
        AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(directory + "/" + bundleName);
        yield return request;
        if (request.assetBundle == null)
        {
            Debug.LogError("Couldn't laod " + directory + bundleName);
            yield break;
        }
        new PlaneInformation(request.assetBundle, directory);
    }
}

[HarmonyPatch(typeof(PilotSaveManager))]
[HarmonyPatch("EnsureVehicleCollections")]
public class EnsureVehicleCollectionsPatch // C stop stealing my code
{
    public static bool Prefix()
    {
        Dictionary<string, PlayerVehicle> vehicles = Traverse.Create(typeof(PilotSaveManager)).Field("vehicles").GetValue() as Dictionary<string, PlayerVehicle>;
        if (vehicles == null)
        {
            Debug.Log("SCP: ensuring vehicle collections.");    
            List<PlayerVehicle> vehicleList = new List<PlayerVehicle>();
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

[HarmonyPatch(typeof(PilotSelectUI))]
[HarmonyPatch("SetupVehicleScreen")]
public class SetupVehicleScreenPatch
{
    public static bool Prefix(ref PilotSelectUI __instance)
    {
        Debug.Log("SCP: Setting vehicle screen.");
        Traverse.Create(__instance).Field("vehicles").SetValue(PilotSaveManager.GetVehicleList());
        return true;
    }
}

[HarmonyPatch(typeof(CampaignSelectorUI), nameof(CampaignSelectorUI.OpenCampaignSelector))]
public static class Inject_Scenarios
{
    public static bool Prefix()
    {
        foreach (PlaneInformation info in PlaneInformation.planes)
            info.SwapVehicleName(true);
        return true;
    }
}

[HarmonyPatch(typeof(CampaignInfoUI), nameof(CampaignInfoUI.UpdateDisplay))]
public static class Ensure_PlayerVehiclesNamedRight
{
    public static bool Prefix()
    {
        foreach (PlaneInformation info in PlaneInformation.planes)
            info.SwapVehicleName(false);
        return true;
    }
}

[HarmonyPatch(typeof(PilotSave), nameof(PilotSave.GetVehicleSave))]
public static class Ensure_VehicleSave
{
    public static bool Prefix(ref string vehicleID)
    {
        if (VTOLAPI.currentScene == VTOLScenes.VehicleConfiguration)
        {
            foreach (PlaneInformation info in PlaneInformation.planes)
            {
                if (info.trueVehicleName == vehicleID)
                {
                    vehicleID = PlaneInformation.convertString(info.baseVehicle);
                    Debug.Log("Changed vehicle ID");
                    Main.selectedPlane = info;
                    return true;
                }
            }
        }
        return true;
    }
}

[HarmonyPatch(typeof(PlayerSpawn), nameof(PlayerSpawn.OnPreSpawnUnit))]
public static class Ensure_SelectedVehicleChosen
{
    public static bool Prefix()
    {
        VTScenario.current.vehicle = PlaneInformation.planes[0].playerVehicle;
        if (Main.selectedPlane != null)
        {
            Debug.Log("Changing player vehicle to custom vehicle");
            VTScenario.current.vehicle = Main.selectedPlane.playerVehicle;
        }
        return true;
    }
}

[HarmonyPatch(typeof(PilotSaveManager), nameof(PilotSaveManager.LoadPilotsFromFile))]
public static class Ensure_CorrectVehicle
{
    public static void Postfix()
    {
        if (VTOLAPI.currentScene == VTOLScenes.VehicleConfiguration)
        {
            Debug.Log("setting vehicle to x02s");
            PilotSaveManager.currentVehicle = PlaneInformation.planes[0].playerVehicle;
            Debug.Log("changed vehicle to x02s");
        }
    }
}