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

    public void Start()
    {
        instance = this;
        StartCoroutine(Armory.Init());
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

