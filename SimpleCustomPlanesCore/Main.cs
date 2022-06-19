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
using UnityEngine.UI;

public class Main : VTOLMOD
{
    public static Main instance;
    public static PlaneInformation selectedPlane = null;
    //public static Texture texture;

    public void Start()
    {
        instance = this;
    }

    public static void Log(string message)
    {
        Debug.Log(message);
    }

    public override void ModLoaded()
    {
        HarmonyInstance.Create("tempy.SCP.core").PatchAll();

        PilotSelectUI ui = Resources.FindObjectsOfTypeAll<PilotSelectUI>().FirstOrDefault(); // this should be fine cuz there's not many objects here
        GameObject template = GameObject.Instantiate(ui.createPilotDisplayObject, ui.createPilotDisplayObject.transform.parent).AddComponent<LoadingTemplate>().gameObject;
        template.SetActive(true); // we're tryna call awake() here
        template.SetActive(false);
        base.ModLoaded();

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
    }

    private IEnumerator asyncLoad(string directory, string bundleName)
    {
        LoadingTemplate.instance.AddVehicle(bundleName);
        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        watch.Start();
        AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(directory + "/" + bundleName);
        yield return request;
        if (request.assetBundle == null)
        {
            Debug.LogError("Couldn't load " + directory + bundleName);
            yield break;
        }
        Coroutine routine = StartCoroutine(new PlaneInformation().LoadFromPath(request.assetBundle, directory));
        yield return routine;
        watch.Stop();
        LoadingTemplate.instance.RemoveVehicle(bundleName);
        Debug.Log("Loaded vehicle " + bundleName + " in " + watch.ElapsedMilliseconds / 1000 + " seconds.");
    }
}

public class LoadingTemplate : MonoBehaviour
{
    public static LoadingTemplate instance;
    public bool hasVehicle = false;

    private GameObject textTemplate;
    private PilotSelectUI pui;
    private Dictionary<string, GameObject> loadingVehicles = new Dictionary<string, GameObject>();
    private int currIdx = 0;

    public void Awake()
    {
        instance = this;
        Debug.Log("This is a new instance");
        pui = GetComponentInParent<PilotSelectUI>();
        Debug.Log("got pui");
        transform.Find("nameBorder").gameObject.SetActive(false);
        Debug.Log("got nameborder");
        transform.Find("NameText").gameObject.SetActive(false);
        Debug.Log("got nametext");
        transform.Find("EditNameButton").gameObject.SetActive(false);
        Debug.Log("got editname");
        transform.Find("BackButton").gameObject.SetActive(false);
        Debug.Log("got BackButton");
        transform.Find("StartButton").gameObject.SetActive(false);
        Debug.Log("got startbutton");
        transform.Find("Label (1)").gameObject.GetComponent<Text>().text = "Loading Vehicles";
        Debug.Log("got label (1)");
        textTemplate = transform.Find("Label").gameObject;
        Debug.Log("got label");
        textTemplate.SetActive(false);
        Debug.Log("disabled label");
    }

    public void Open()
    {
        pui.selectPilotDisplayObject.SetActive(false);
        gameObject.SetActive(true);
    }

    public void AddVehicle(string name)
    {
        hasVehicle = true;
        GameObject newTemplate = Instantiate(textTemplate, textTemplate.transform.parent);
        newTemplate.GetComponent<Text>().text = name;
        newTemplate.transform.localPosition = new Vector3(0.0001296997f, 383 - (81 * currIdx), 0f);
        newTemplate.gameObject.SetActive(true);
        loadingVehicles.Add(name, newTemplate);
        currIdx++;
    }

    public void RemoveVehicle(string name)
    {
        loadingVehicles[name].SetActive(false);
        loadingVehicles.Remove(name);
        currIdx--;
        if (loadingVehicles.Count == 0)
        {
            hasVehicle = false;
            pui.StartSelectedPilotButton();
            gameObject.SetActive(false);
            return;
        }
        for (int i = 0; i < currIdx; i++)
            loadingVehicles.Values.ToArray()[i].transform.localPosition = new Vector3(0.0001296997f, 383 - (81 * i), 0f);
    }
}

[HarmonyPatch(typeof(PilotSelectUI))]
[HarmonyPatch("StartSelectedPilotButton")]
public class Ensure_VehiclesLoaded
{
    public static bool Prefix()
    {
        if (LoadingTemplate.instance != null && LoadingTemplate.instance.hasVehicle)
        {
            LoadingTemplate.instance.Open();
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(PilotSaveManager))]
[HarmonyPatch("EnsureVehicleCollections")]
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

[HarmonyPatch(typeof(PilotSelectUI), nameof(PilotSelectUI.SelectVehicle))]
public static class Ensure_VehicleSelected
{
    public static void Postfix(PlayerVehicle vehicle)
    {
        Debug.Log("SCP: Setting custom vehicle " + vehicle.nickname);
        Main.selectedPlane = PlaneInformation.GetCustomVehicleFromNickName(vehicle.nickname);
    }
}

[HarmonyPatch(typeof(PlayerSpawn), nameof(PlayerSpawn.OnPreSpawnUnit))]
public static class Ensure_SelectedVehicleChosen
{
    public static bool Prefix()
    {
        if (Main.selectedPlane != null)
        {
            Debug.Log("Changing player vehicle to custom vehicle " + Main.selectedPlane.trueVehicleName);
            VTScenario.current.vehicle = Main.selectedPlane.playerVehicle;
        }
        return true;
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

[HarmonyPatch(typeof(PilotSaveManager), nameof(PilotSaveManager.SavePilotsToFile))]
public static class Ensure_VehiclesSaved
{
    public static void Postfix()
    {
        CustomAircraftSaveManager.SaveVSaves();
    }
}

[HarmonyPatch(typeof(VehicleSave), nameof(VehicleSave.GetCampaignSave))]
public static class Ensure_VehicleHasSave
{
    public static void Postfix(VehicleSave __instance, string campaignID, ref CampaignSave __result)
    {
        if (__result == null && PlaneInformation.CheckCustomVehicleName(__instance.vehicleName))
        {
            PlaneInformation info = PlaneInformation.GetCustomVehicle(__instance.vehicleName);
            Main.Log("Adding campaign to vehicle save " + campaignID);
            VTCampaignInfo cInfo = VTResources.GetBuiltInCampaign(campaignID);
            if (cInfo == null)
            {

                cInfo = VTResources.GetCustomCampaign(campaignID);
                if (cInfo == null)
                {
                    Debug.LogError("No campaign found for id " + campaignID);
                    return;
                }
            }
            Campaign campaign = cInfo.ToIngameCampaign();
            CampaignSave campaignSave = new CampaignSave();
            campaignSave.campaignName = campaign.campaignName;
            campaignSave.campaignID = campaign.campaignID;
            campaignSave.vehicleName = info.trueVehicleName;
            campaignSave.completedScenarios = new List<CampaignSave.CompletedScenarioInfo>();
            campaignSave.availableScenarios = new List<string>();
            campaignSave.currentFuel = 1f;
            campaignSave.currentWeapons = new string[info.playerVehicle.hardpointCount];
            campaignSave.availableWeapons = new List<string>();
            foreach (string item in campaign.weaponsOnStart)
                campaignSave.availableWeapons.Add(item);
            foreach (string item2 in campaign.scenariosOnStart)
                campaignSave.availableScenarios.Add(item2);
            __instance.campaignSaves.Add(campaignSave);
            __result = campaignSave;
        }
    }
}