﻿using Harmony;
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

public static class Armory
{
    public static Dictionary<string, CustomEqInfo> allCustomWeapons = new Dictionary<string, CustomEqInfo>();
    private static HPEquipIRML irml;

    public static IEnumerator Init()
    {
        ResourceRequest request = Resources.LoadAsync("hpequips/afighter/fa26_iris-t-x1");
        yield return request;
        irml = ((GameObject)request.asset).GetComponent<HPEquipIRML>();
    }

    public static void LoadGeneric(GameObject weaponObject, PlaneInformation vehicle, bool isExclude, bool isWMD, string equipPoints = null)
    {
        if (weaponObject == null)
        {
            Debug.LogError("Tried to load a null weapon.");
            return;
        }
        GameObject.DontDestroyOnLoad(weaponObject);
        foreach (AudioSource source in weaponObject.GetComponentsInChildren<AudioSource>(true))
        {
            source.outputAudioMixerGroup = VTResources.GetExteriorMixerGroup();
        }
        HPEquipMissileLauncher launcher = weaponObject.GetComponent<HPEquipMissileLauncher>();
        if (launcher) // I know this solution isn't optimized in the slightest, but meh
        {
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
        allCustomWeapons.Add(weaponObject.name, new CustomEqInfo(weaponObject, vehicle, isExclude, isWMD, equipPoints));
        weaponObject.SetActive(false);
        Debug.Log("Loaded " + weaponObject);
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