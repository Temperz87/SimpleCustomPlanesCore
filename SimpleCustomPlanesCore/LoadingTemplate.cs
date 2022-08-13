using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

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
        pui = GetComponentInParent<PilotSelectUI>();
        transform.Find("nameBorder").gameObject.SetActive(false);
        transform.Find("NameText").gameObject.SetActive(false);
        transform.Find("EditNameButton").gameObject.SetActive(false);
        transform.Find("BackButton").gameObject.SetActive(false);
        transform.Find("StartButton").gameObject.SetActive(false);
        transform.Find("Label (1)").gameObject.GetComponent<Text>().text = "Loading Vehicles";
        textTemplate = transform.Find("Label").gameObject;
        textTemplate.SetActive(false);
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