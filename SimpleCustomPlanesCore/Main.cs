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

    public override void ModLoaded()
    {
        HarmonyInstance.Create("tempy.SCP.core").PatchAll();
        VTOLAPI.SceneLoaded += (scene) =>
        {
            if (scene == VTOLScenes.ReadyRoom)
                selectedPlane = null;
        };

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
        AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(directory + "/" + bundleName);
        yield return request;
        if (request.assetBundle == null)
        {
            Debug.LogError("Couldn't load " + directory + bundleName);
            yield break;
        }
        Coroutine routine = StartCoroutine(new PlaneInformation().LoadFromPath(request.assetBundle, directory));
        yield return routine;
        LoadingTemplate.instance.RemoveVehicle(bundleName);
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