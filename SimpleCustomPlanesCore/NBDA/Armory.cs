using Harmony;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using VTNetworking;
using VTOLVR.Multiplayer;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Linq;

public class Armory
{
    public static Dictionary<string, CustomEqInfo> allCustomWeapons = new Dictionary<string, CustomEqInfo>();

    public static GameObject LoadGeneric(GameObject weaponObject, string name, PlaneInformation vehicle, bool isExclude, bool isWMD, string equipPoints = null)
    {
        if (weaponObject == null)
        {
            Debug.LogError("Tried to load a null weapon.");
            return null;
        }
        Debug.Log("to inject");
        GameObject weaponToInject = weaponObject;
        Debug.Log("name");
        weaponToInject.name = name;
        Debug.Log("destroy on load");
        GameObject.DontDestroyOnLoad(weaponToInject);
        Debug.Log("audiosources");
        foreach (AudioSource source in weaponToInject.GetComponentsInChildren<AudioSource>(true))
        {
            Debug.Log("got a source");
            source.outputAudioMixerGroup = VTResources.GetExteriorMixerGroup();
        }
        Debug.Log("launcher");
        HPEquipMissileLauncher launcher = weaponObject.GetComponent<HPEquipMissileLauncher>();
        if (launcher) // I know this solution isn't optimized in the slightest, but meh
        {
            Debug.Log("got a launcher");
            GameObject dummyEquipper = Resources.Load("hpequips/afighter/fa26_iris-t-x1") as GameObject; // can't async this as we still need the return so rip
            HPEquipIRML irml = dummyEquipper.GetComponent<HPEquipIRML>();
            launcher.ml.launchAudioClips[0] = GameObject.Instantiate(irml.ml.launchAudioClips[0]);
            launcher.ml.launchAudioSources[0].outputAudioMixerGroup = irml.ml.launchAudioSources[0].outputAudioMixerGroup;

            Missile missile = launcher.ml.missilePrefab.GetComponent<Missile>();
            if (missile.guidanceMode == Missile.GuidanceModes.Heat || missile.guidanceMode == Missile.GuidanceModes.Radar)
            {
                Missile dummyMissile = irml.ml.missilePrefab.GetComponent<Missile>();
                AudioSource source = missile.GetComponent<AudioSource>();
                AudioSource dummySource = dummyMissile.GetComponent<AudioSource>();
                source.outputAudioMixerGroup = dummySource.outputAudioMixerGroup;
                source.clip = GameObject.Instantiate(dummySource.clip);

                if (missile.heatSeeker != null)
                {
                    Transform parent = missile.gameObject.transform.Find("SeekerParent");
                    AudioSource copiedSeeker = parent.Find("SeekerAudio").GetComponent<AudioSource>();
                    AudioSource originalSeeker = dummyMissile.transform.Find("SeekerParent").Find("SeekerAudio").GetComponent<AudioSource>();
                    copiedSeeker.clip = GameObject.Instantiate(originalSeeker.clip);
                    copiedSeeker.outputAudioMixerGroup = GameObject.Instantiate(originalSeeker.outputAudioMixerGroup);

                    AudioSource copiedLock = parent.Find("LockToneAudio").GetComponent<AudioSource>();
                    AudioSource originalLock = dummyMissile.transform.Find("SeekerParent").Find("LockToneAudio").GetComponent<AudioSource>();
                    copiedLock.clip = GameObject.Instantiate(originalLock.clip);
                    copiedLock.outputAudioMixerGroup = originalLock.outputAudioMixerGroup;
                }
            }
        }
        Debug.Log("all custom weapons");
        allCustomWeapons.Add(name, new CustomEqInfo(weaponToInject, vehicle, isExclude, isWMD, equipPoints));
        Debug.Log("set active");
        weaponToInject.SetActive(false);
        Debug.Log("Loaded " + name);
        return weaponToInject;
    }

    public static bool CheckCustomWeapon(string name)
    {
        if (String.IsNullOrEmpty(name))
            return false;
        foreach (string customName in allCustomWeapons.Keys)
            if (name.ToLower().Contains(customName.ToLower()))
                return true;
        return false;
    }
    public static CustomEqInfo TryGetWeapon(string name)
    {
        foreach (string customName in allCustomWeapons.Keys)
            if (name.ToLower().Contains(customName.ToLower()))
                return allCustomWeapons[customName];
        return null;
    }
}