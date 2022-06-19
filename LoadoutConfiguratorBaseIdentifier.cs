using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;
using System.IO;
using UnityEngine.Events;
using Valve.Newtonsoft.Json;
using System.Text;
using System.Globalization;
using System.Linq;

public class LoadoutConfiguratorBaseIdentifier : MonoBehaviour
{
    [ContextMenu("Initialize")]
    public void Initialize()
    {
        allObjects = new List<ObjectInformation>();

        List<string> allNames = new List<string>();
        foreach (Transform tf in GetComponentsInChildren<Transform>(true))
        {
            if (!Initialized)
            {
                allTfs.Add(HashTransform(tf, allNames), transform);
            }
            else
            {
                allNames.Add(transform.name);
                if (!allTfs.ContainsKey(tf.name))
                    allTfs.Add(tf.name, tf);
            }
        }

        void RecursiveGetTf(Transform tf)
        {
            foreach (Transform child in tf)
            {
                if (child.name == "EjectorSeat-1135239333")
                {
                    //Debug.Log("Skipping blacklisted child");
                    continue;
                }
                allObjects.Add(new ObjectInformation(child.gameObject));
                RecursiveGetTf(child);
            }
        }

        allObjects.Add(new ObjectInformation(transform.gameObject));
        RecursiveGetTf(transform);

        bool shouldDump = !Initialized;
        Initialized = true;

        if (shouldDump)
        {
            if (!Directory.Exists(pathToDump + "/SCPInformation/"))
                Directory.CreateDirectory(pathToDump + "/SCPInformation/");
            List<string> toDump = new List<string>();
            foreach (ObjectInformation info in allObjects)
            {
                if (info == null)
                    Debug.Log("Null object?");
                toDump.Add(info.GetDumpLine());
                //Debug.Log("Added " + info.originalObject.name + " to be dumped.");
            }
            Debug.Log("Dumping initial file.");
            using (StreamWriter writer = new StreamWriter(pathToDump + "/SCPInformation/intialDump" + nickname + ".scpart", false, Encoding.Default, 4096))
            {
                foreach (string line in toDump)
                {
                    writer.WriteLine(line);
                    //Debug.Log("Dumped " + line);
                    writer.Flush();
                }
            }
        }


    }

    private string HashTransform(Transform transform, List<string> allNames)
    {
        if (transform.name.Contains("(Clone)"))
            transform.name = transform.name.Substring(0, transform.name.IndexOf("(Clone)"));
        string toHash = transform.name;

        Transform parent = transform;
        while (parent.parent != null)
        {
            parent = parent.parent;
            toHash += parent.name;
        }

        transform.name = transform.name + toHash.GetHashCode();
        while (allNames.Contains(transform.name))
            transform.name += "1";
        allNames.Add(transform.name);
        //if (verboseLogs)
        //    Debug.Log("hashed " + transform.name);
        return transform.name;
    }

    [ContextMenu("Dump file")]
    public void Dump()
    {
        /* 1: Get File Location
         * 2: Spawn in custom plane first 
         * 3: Yell at users to NEVER add components on anything that isn't a custom object
         * 4: Get ALL values of components and dump them to a file, do pointers for object scripts (ex: take this health on this vehicle part and assign it to this other one on another vehicle, and store them into a giant ass list
         * 5: dump the file to the specified path, format should be gameobject name, position, original parent, reparent 
        */
        List<string> toDump = new List<string>();
        foreach (ObjectInformation info in allObjects)
        {
            if (info == null)
                Debug.Log("Null object?");
            toDump.Add(info.GetDumpLine());
            //Debug.Log("Added " + info.originalObject.name + " to be dumped.");
        }

        Debug.Log("Dumping second file to dif.");
        using (StreamWriter writer = new StreamWriter(pathToDump + "/SCPInformation/intialDump" + nickname + ".temp", false, Encoding.Default, 4096))
        {
            foreach (string line in toDump)
            {
                writer.WriteLine(line);
                //Debug.Log("Dumped " + line);
                writer.Flush();
            }
        }



        Dictionary<string, Dictionary<string, List<SCPTextReader.Instruction>>> initialDump = SCPTextReader.GetLinesFromFile(pathToDump + "/SCPInformation/intialDump" + nickname + ".scpart");
        Dictionary<string, Dictionary<string, List<SCPTextReader.Instruction>>> tempDump = SCPTextReader.GetLinesFromFile(pathToDump + "/SCPInformation/intialDump" + nickname + ".temp");

        Dictionary<string, Dictionary<string, List<SCPTextReader.Instruction>>> dictToDump = new Dictionary<string, Dictionary<string, List<SCPTextReader.Instruction>>>();

        foreach (string key in initialDump.Keys)
        {
            Dictionary<string, List<SCPTextReader.Instruction>> currentDict = new Dictionary<string, List<SCPTextReader.Instruction>>();
            if (!tempDump.ContainsKey(key))
            {
                string hashedName = NextElement(key, 0);
                foreach (string toFind in tempDump.Keys)
                {
                    if (NextElement(toFind, 0) == hashedName)
                    {
                        dictToDump.Add(toFind, currentDict);
                        foreach (string component in tempDump[toFind].Keys)
                        {
                            List<SCPTextReader.Instruction> currentList = new List<SCPTextReader.Instruction>();
                            for (int i = 0; i < tempDump[toFind][component].Count; i++)
                            {
                                if (initialDump[key][component][i] != tempDump[toFind][component][i] && (tempDump[toFind][component][i].instruction != SCPTextReader.Instruction.InstructionType.ignore))
                                    currentList.Add(tempDump[toFind][component][i]);
                            }
                            if (currentList.Count > 0)
                            {
                                currentDict.Add(component, currentList);
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (string component in tempDump[key].Keys)
                {
                    List<SCPTextReader.Instruction> currentList = new List<SCPTextReader.Instruction>();
                    for (int i = 0; i < tempDump[key][component].Count; i++)
                    {
                        if (initialDump[key][component][i] != tempDump[key][component][i] && (tempDump[key][component][i].instruction != SCPTextReader.Instruction.InstructionType.ignore))
                            currentList.Add(tempDump[key][component][i]);
                    }
                    if (currentList.Count > 0)
                    {
                        if (!dictToDump.ContainsKey(key))
                            dictToDump.Add(key, currentDict);
                        currentDict.Add(component, currentList);
                    }
                }
            }
        }

        Debug.Log("Dumping file.");
        using (StreamWriter writer = new StreamWriter(pathToDump + nickname + ".SCLC", false, Encoding.Default, 4096))
        {
            foreach (string line in dictToDump.Keys)
            {
                writer.WriteLine(line);
                writer.Flush();
                foreach (string component in dictToDump[line].Keys)
                {
                    writer.WriteLine(component);
                    writer.Flush();
                    foreach (SCPTextReader.Instruction instruction in dictToDump[line][component])
                    {
                        if (instruction.instruction == SCPTextReader.Instruction.InstructionType.ignore)
                            continue;
                        writer.WriteLine(instruction.GetDumpLine("        "));
                        writer.Flush();
                    }
                }
            }
        }

#if UNITY_EDITOR 
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
#endif
    }

    [ContextMenu("Load from path")]
    public void Load()
    {
        List<string> allNames = new List<string>();
        if (!Initialized)
        {
            foreach (Transform tf in GetComponentsInChildren<Transform>(true))
                allTfs.Add(HashTransform(tf, allNames), tf);
            Initialized = true;
        }
        Dictionary<string, Dictionary<string, List<SCPTextReader.Instruction>>> allInstructions = SCPTextReader.GetLinesFromFile(pathToDump + "/" + nickname + ".SCLC");
        foreach (string line in allInstructions.Keys) // we must hash them first so we actually get their names
        {
            Dictionary<string, List<SCPTextReader.Instruction>> currentObject = allInstructions[line];

            if (line.IndexOf(" ") <= 0)
            {
                Debug.LogWarning("Got an index less than 0 for space.");
                continue;
            }
            try
            {
                if (!allTfs.ContainsKey(NextElement(line, 0)))
                {
                    Debug.LogWarning("Couldn't resolve object " + NextElement(line, 0));
                    continue;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                Debug.LogWarning("Couldn't resolve object");
                continue;
            }

            Transform currentTf = allTfs[NextElement(line, 0)];
            //Transform currentParent = null;
            Transform newParent = null;

            string finderString = NextElement(line, 1);
            //if (!finderString.Contains("NULL"))
            //{
            //    Debug.Log("Got string " + finderString + " on " + currentTf.name);
            //    //currentParent = allTfs[finderString.Trim()];
            //}
            finderString = NextElement(line, 2);
            if (!finderString.Contains("NULL"))
            {
                //Debug.Log("Got string " + finderString + " again on " + currentTf.name);
                try
                {
                    newParent = allTfs[finderString.Trim()];
                }
                catch (KeyNotFoundException e)
                {
                    newParent = null;
                }
            }
            if (newParent != null)
            {
                currentTf.SetParent(newParent);
                //Debug.Log("reparenting " + currentTf + " from " + currentParent?.name + " to " + newParent.name);
            }
            if (currentTf.parent != null)
            {
                Vector3 localPos;
                Vector3 localEulers;
                Vector3 localScale;

                localPos = StringToVector3(NextElement(line, 3));
                localEulers = StringToVector3(NextElement(line, 4));
                localScale = StringToVector3(NextElement(line, 5));

                currentTf.localPosition = localPos;
                currentTf.localEulerAngles = localEulers;
                currentTf.localScale = localScale;
                currentTf.gameObject.SetActive(line[line.Length - 2] == 'u');
                //Debug.Log("Set " + currentTf.name + " to " + localPos);
            }
            foreach (string component in currentObject.Keys)
            {
                //verboseLogs = true;
                //if (verboseLogs)
                //    Debug.Log("Try do object " + currentTf.name + " for component " + component.Trim());
                Type type = Type.GetType(component.Trim());
                if (type == null)
                {
                    Debug.LogError("Couldn't resolve component " + component.Trim());
                    continue;
                }
                Component currentComponent = currentTf.GetComponent(type);
                if (currentComponent == null)
                {
                    Debug.LogError("Current component of type " + type.Name + " was null on " + currentTf.name);
                    continue;
                }
                List<SCPTextReader.Instruction> instructions = currentObject["    " + type.AssemblyQualifiedName];
                //if (verboseLogs)
                //    Debug.Log("dict    " + type.AssemblyQualifiedName + " has a length of " + instructions.Count + " on " + currentTf.name);
                foreach (SCPTextReader.Instruction instruction in instructions)
                {
                    instruction.Run(currentComponent, allTfs, false);
                }
            }
        }
        
        Debug.Log("Loaded from file!");
    }


    class manifestDataModel
    {
        public string playerVehicle;
        public string baseVehicle;
        public string campaign;
        public string configurator;
        public string equips;
        public string dependencyName;
    }

    public static vehicleEnum convertString(string convert)
    {
        switch (convert.Trim())
        {
            case "AV42C":
                return vehicleEnum.AV42C;
            case "FA26B":
                return vehicleEnum.FA26B;
            case "F45A":
                return vehicleEnum.F45A;
            case "AH94":
                return vehicleEnum.AH94;
        }
        return vehicleEnum.None;
    }

    public static string NextElement(string line, int index)
    {
        //Debug.Log("Getting element " + index + " from line " + line);
        return line.Substring(SubStringOfIndex(line, "[{(", index) + 3, SubStringOfIndex(line, ")}]", index) - SubStringOfIndex(line, "[{(", index) - 3).Trim();
    }

    public static Vector3 StringToVector3(string sVector)
    {
        // Remove the parentheses
        if (sVector.StartsWith("(") && sVector.EndsWith(")"))
        {
            sVector = sVector.Substring(1, sVector.Length - 2);
        }

        // split the items
        string[] sArray = sVector.Split(',');

        // store as a Vector3
        Vector3 result = new Vector3(
            float.Parse(sArray[0]),
            float.Parse(sArray[1]),
            float.Parse(sArray[2]));

        return result;
    }

    public static int SubStringOfIndex(string str, string value, int position = 0)
    {
        if (position < 0)
            return -1;

        int offset = str.IndexOf(value);
        for (int i = 0; i < position; i++)
        {
            if (offset == -1) return -1;
            offset = str.IndexOf(value, offset + 1);
        }

        return offset;
    }

    public string pathToDump = "Assets/Simple Custom Planes/SubFolder/";
    [Header("The vehicle's nickname, in your planes final mod folder put an image called lcImage.png for custom images")]
    public string nickname;

    public enum vehicleEnum
    {
        None,
        AV42C,
        FA26B,
        F45A,
        AH94
    }

    //public bool verboseLogs = false;

    [Header("This is to show if the object has been intialized, don't click this")]
    public bool Initialized = false;

    public List<ObjectInformation> allObjects;
    private Dictionary<string, Transform> allTfs = new Dictionary<string, Transform>();

    public class ObjectInformation
    {
        public GameObject originalObject;
        public Transform originalParent;
        public Transform newParent;
        public List<Component> allComponents = new List<Component>();
        public Dictionary<Component, Type> componentsByType = new Dictionary<Component, Type>();
        //private int initialDumpHash;

        public bool Equals(ObjectInformation obj)
        {
            if (this == null ^ obj.Equals(null))
                return false;
            return (this == null && obj == null) || this.originalObject == obj.originalObject;
        }

        public ObjectInformation(GameObject go)
        {
            //if (verboseLogs)
            //    Debug.Log("Making new Object information for " + go.name);
            originalObject = go;
            originalParent = go.transform.parent;

            int i = 0;
            foreach (Component component in go.GetComponents<Component>())
            {
                if (component == null)
                {
                    Debug.LogWarning("Somehow got a null component?");
                    continue;
                }
                allComponents.Add(component);
                componentsByType.Add(component, component.GetType());
                //if (verboseLogs)
                    Debug.Log("Added component to object " + go.name + " of " + component.GetType().Name);
                i++;
            }
            //if (verboseLogs)
            //    Debug.Log("Added " + i + " components to object " + go.name);
            //initialDumpHash = GetProperties().GetHashCode();
        }

        public string GetDumpLine()
        {
            newParent = originalObject.transform.parent;
            string active = originalObject.activeSelf.ToString();
            if (originalObject.name.Contains("Local") && originalObject.GetComponent<VTOLVR.Multiplayer.MultiplayerObjectSwitch>())
                active = "False";
            string result = "[{(" + originalObject.name + ")}] [{(" + (originalParent ? originalParent.name : "NULL") + ")}] [{(" + (newParent ? newParent.name : "NULL") + ")}] [{(" + Vector3ToString(originalObject.transform.localPosition) + ")}] [{(" + Vector3ToString(originalObject.transform.localEulerAngles) + ")}] [{(" + Vector3ToString(originalObject.transform.localScale) + ")}] " + active;
            string toAdd = GetProperties();
            //Debug.Log("Initial hash is " + initialDumpHash);
            if (toAdd.Length != 0)
                result += toAdd + "\nEND";
            return result;
        }

        private string Vector3ToString(Vector3 vector)
        {
            return "(" + vector.x + ", " + vector.y + ", " + vector.z + ")";
        }

        private string GetProperties()
        {
            string result = "";
            foreach (Component component in allComponents)
            {
                if (component is LoadoutConfiguratorBaseIdentifier || component is PerVRDeviceLocalPosition || component is VTSteamVRController || component is VRHandController || component is RiftTouchController || component is Valve.VR.SteamVR_Behaviour_Pose || component is GloveAnimation || component is LookRotationReference || component is IKTwo || component is VRSDKSwitcher)
                    continue;
                if (component == null)
                {
                    Debug.Log("Null component?");
                    continue;
                }

                if (component is Renderer)
                {
                    result += "\n    " + componentsByType[component].AssemblyQualifiedName + "\n        assign enabled [{(Boolean)}] [{(" + ((Renderer)component).enabled + ")}]\n    END";
                    continue;
                }

                FieldInfo[] fInfo = componentsByType[component].GetFields(BindingFlags.Public | BindingFlags.Instance);
                if (fInfo.Length != 0)
                {
                    string componentStr = "\n    " + componentsByType[component].AssemblyQualifiedName;
                    foreach (FieldInfo prop in fInfo)
                    {
                        object value = prop.GetValue(component);
                        //if (value == null)
                        //    continue;
                        //Debug.Log(trueType.Name + " on " + component.gameObject.name + " found prop: " + prop.Name + ": " + value + " of type " + value.GetType().Name);
                        if (value != null && (value is Array || value is IList))
                        {
                            Array newArr = null;
                            Type type;
                            if (value is Array)
                            {
                                newArr = (Array)value;
                                type = newArr.GetType().GetElementType();
                            }
                            else
                            {
                                newArr = new object[((IList)value).Count];
                                int i = 0;
                                foreach (object obj in (IList)value)
                                {
                                    newArr.SetValue(obj, i);
                                    i++;
                                }
                                string badName = ((IList)value).GetType().AssemblyQualifiedName;
                                string newName = badName.Replace("[[", "[{("); // this is an amazing solution and not hacky
                                newName = newName.Replace("]]", ")}]");
                                type = Type.GetType(NextElement(newName, 0));
                            }
                            string arrayStr = "\n        array [{(" + type.AssemblyQualifiedName + ")}] [{(" + prop.Name + ")}] " + newArr.Length;
                            if (newArr is Component[])
                            {
                                for (int i = 0; i < newArr.Length; i++)
                                {
                                    Component arrValue = ((Component)newArr.GetValue(i));
                                    arrayStr += "\n            pointer " + i + " [{(" + type.AssemblyQualifiedName + ")}] [{(" + arrValue.name + ")}]";
                                }
                            }
                            else
                            {
                                if (type.Name == "String" || type.Name == "Int32" || type.Name == "Single" || type.Name == "Boolean")
                                {
                                    //Debug.Log("Got a primitive[]");
                                    for (int i = 0; i < newArr.Length; i++)
                                    {
                                        arrayStr += "\n            assign " + i + " [{(" + type.Name + ")}] [{(" + newArr.GetValue(i) + ")}]";
                                    }
                                }
                                else if (newArr is GameObject[]) // i don't know why i have to do this, should be literally a component
                                {
                                    //if (value is IList)
                                    //Debug.Log("Got a gameObject[] on " + prop.Name);
                                    for (int i = 0; i < newArr.Length; i++)
                                    {
                                        //if (value is IList)
                                        //Debug.Log("Writing " + ((GameObject)newArr.GetValue(i)).name);
                                        arrayStr += "\n            pointer " + i + " [{(" + typeof(GameObject).AssemblyQualifiedName + ")}] [{(" + ((GameObject)newArr.GetValue(i)).name + ")}]";
                                    }
                                }
                                else
                                {
                                    //Debug.Log("Got a weird[]");
                                    string typeName = type.AssemblyQualifiedName;
                                    if ((typeName.Contains("PlayerVehicle") && !typeName.Contains("PlayerVehicleSetup")) || Type.GetType(typeName).IsAssignableFrom(typeof(PerVRDeviceLocalPosition)) || typeName.Contains("SteamId") || typeName.Contains("Friend") || Type.GetType(typeName).IsSubclassOf(typeof(ScriptableObject)) || typeName.Contains("AnimationCurve"))
                                    {
                                        componentStr += "\n ignore " + prop.Name;
                                        continue;
                                    }
                                    for (int i = 0; i < newArr.Length; i++)
                                    {
                                        object element = newArr.GetValue(i);
                                        arrayStr += "\n            " + convertField(i.ToString(), element, type.AssemblyQualifiedName, "    ");
                                    }
                                }
                            }
                            componentStr += arrayStr;
                        }
                        else
                            componentStr += "\n        " + convertField(prop.Name, value, prop.FieldType.AssemblyQualifiedName);
                    }
                    componentStr += "\n    END";
                    result += componentStr;
                }
            }
            return result;
        }

        private string convertField(string fieldName, object field, string typeName, string newObjectIndent = "")
        {
            string result;
            Type fieldType = Type.GetType(typeName);
            if (fieldType.IsSubclassOf(typeof(Component)))
            {
                if (field != null)
                {
                    try
                    {
                        result = "pointer " + fieldName + " [{(" + ((Component)field).name + ")}] [{(" + typeName + " )}]";
                    }
                    catch (UnassignedReferenceException e)
                    {
                        result = "pointer " + fieldName + " [{(null)}] [{(" + typeName + " )}]";
                    }
                    catch (NullReferenceException e)
                    {
                        result = "pointer " + fieldName + " [{(null)}] [{(" + typeName + " )}]";
                    }
                    catch (MissingReferenceException)
                    {
                        result = "ignore " + fieldName;
                    }
                }
                else
                {
                    //Debug.Log("Writing null");
                    result = "pointer " + fieldName + " [{(null)}] [{(" + typeName + " )}]";
                }
            }
            else if (field is GameObject)
            {
                if (field != null)
                {
                    try
                    {
                        result = "pointer " + fieldName + " [{(" + ((GameObject)field).name + ")}] [{(" + typeName + " )}]";
                    }
                    catch (UnassignedReferenceException e)
                    {
                        result = "pointer " + fieldName + " [{(null)}] [{(" + typeName + " )}]";
                    }
                    catch (NullReferenceException e)
                    {
                        result = "pointer " + fieldName + " [{(null)}] [{(" + typeName + " )}]";
                    }
                    catch (MissingReferenceException)
                    {
                        result = "ignore " + fieldName;
                    }
                }
                else
                {
                    //Debug.Log("Writing null");
                    result = "pointer " + fieldName + " [{(null)}] [{(" + typeName + " )}]";
                }
            }
            else if (field is Animator)
            {
                if (field != null)
                    result = "pointer " + fieldName + " [{(" + ((Animator)field).name + ")}] [{(" + typeName + " )}]";
                else
                    result = "pointer " + fieldName + " [{(null)}] [{(" + typeName + " )}]";
            }
            else if (fieldType.IsSubclassOf(typeof(UnityEventBase)))
            {
                return "ignore " + fieldName;
                string events = "";
                UnityEventBase baseEvent = field as UnityEventBase;
                int size = baseEvent.GetPersistentEventCount();
                if (size == 0)
                    return "ignore " + fieldName;
                string eventType = "none";
                if (baseEvent is IntEvent)
                    eventType = "int";
                else if (baseEvent is BoolEvent)
                    eventType = "bool";
                else if (baseEvent is StringEvent)
                    eventType = "string";
                for (int i = 0; i < size; i++)
                {
                    events += "\n[{(" + baseEvent.GetPersistentTarget(i) + ")}] [{(" + baseEvent.GetPersistentMethodName(i) + ")}]";
                }
                result = "events " + eventType + events;
            }
            else
            {
                if (fieldType.IsPrimitive || field is string || field is Enum)
                {
                    if (field is Enum)
                        field = (int)field;
                    result = "assign " + fieldName + " [{(" + fieldType.Name + ")}] [{(" + field.ToString().Replace("\n", "\\n") + ")}]";
                }
                else
                {
                    result = "ignore " + fieldName;
                    Type newType = Type.GetType(typeName);
                    FieldInfo[] fInfo = newType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                    if ((field is Array || field is IList))
                    {
                        Array newArr;
                        if (field is Array)
                        {
                            newArr = (Array)field;
                            result = "array [{(" + field.GetType().GetElementType().AssemblyQualifiedName + ")}] [{(" + fieldName + ")}]";
                        }
                        else
                        {
                            newArr = new object[((IList)field).Count];
                            int i = 0;
                            Type listType = null;
                            foreach (object obj in (IList)field)
                            {
                                if (listType == null)
                                    listType = obj.GetType();
                                newArr.SetValue(obj, i);
                                i++;
                            }
                            string badName = ((IList)field).GetType().AssemblyQualifiedName;
                            Debug.Log("Badname is " + badName);
                            string newName = badName.Replace("[[", "[{("); // this is an amazing solution and not hacky
                            newName = newName.Replace("]]", ")}]");
                            Debug.Log("newName is " + newName);
                            Debug.Log("Typename is " + NextElement(newName, 0));
                            result = "array [{(" + NextElement(newName, 0) + ")}] [{(" + fieldName + ")}]";
                        }
                        string arrayStr = ""/*"\n        array [{(" + newArr.GetValue(0).GetType().AssemblyQualifiedName + ")}] [{(" + prop.Name + ")}] " + newArr.Length*/;
                        bool addArray = false;
                        if (newArr is Component[])
                        {
                            Debug.Log("Got a component[] on " + result);
                            for (int i = 0; i < newArr.Length; i++)
                            {
                                Component arrValue = ((Component)newArr.GetValue(i));
                                arrayStr += "\n            pointer " + i + " [{(" + arrValue.name + ")}] [{(" + arrValue.GetType().AssemblyQualifiedName + ")}]";
                                addArray = true;
                            }
                        }
                        else
                        {
                            Type type = newArr.GetType().GetElementType();
                            if (type.Name == "String" || type.Name == "Int32" || type.Name == "Single" || type.Name == "Boolean")
                            {
                                Debug.Log("Got a primitive[]");
                                for (int i = 0; i < newArr.Length; i++)
                                {
                                    arrayStr += "\n            assign " + i + " [{(" + newArr.GetValue(i) + ")}] [{(" + type.Name + ")}]";
                                    addArray = true;
                                }
                            }
                            else if (newArr is GameObject[]) // i don't know why i have to do this, should be literally a component
                            {
                                for (int i = 0; i < newArr.Length; i++)
                                {
                                    GameObject arrValue = ((GameObject)newArr.GetValue(i));
                                    arrayStr += "\n            " + newObjectIndent + "pointer " + i + " [{(" + arrValue.name + ")}] [{(" + typeof(GameObject).AssemblyQualifiedName + ")}]";
                                    addArray = true;
                                }
                            }
                            else
                            {
                                Debug.Log("Got a weird[] on " + result);
                                addArray = true;
                                ConstructorInfo[] cInfos = Type.GetType(typeName).GetElementType().GetConstructors();
                                List<string> allTypes = new List<string>();
                                if (cInfos.Length > 0)
                                {
                                    foreach (ParameterInfo pInfo in cInfos[0].GetParameters())
                                    {
                                        Debug.Log("adding " + pInfo.ParameterType.AssemblyQualifiedName + " to " + typeName);
                                        allTypes.Add(pInfo.ParameterType.AssemblyQualifiedName);
                                    }
                                }
                                for (int i = 0; i < newArr.Length; i++)
                                {
                                    object element = newArr.GetValue(i);
                                    arrayStr += "\n                newObject [{(" + i + ")}] [{(" + element.GetType().AssemblyQualifiedName + ")}] [{(" + allTypes.Count + ")}] [{(" + ConfigNodeUtils.WriteList(allTypes) + ")}]";
                                    FieldInfo[] info = element.GetType().GetFields();
                                    foreach (FieldInfo eInfo in info)
                                    {
                                        arrayStr += "\n                " + newObjectIndent + convertField(eInfo.Name, eInfo.GetValue(element), eInfo.FieldType.AssemblyQualifiedName, newObjectIndent + "    ");
                                    }
                                }
                            }
                        }
                        if (addArray)
                            result += arrayStr;
                    }
                    else if (fInfo.Length != 0)
                    {
                        if (Type.GetType(typeName).IsValueType)
                            return "ignore " + fieldName;
                        List<string> allTypes = new List<string>();
                        if (Type.GetType(typeName).GetConstructors().Length > 0)
                        {
                            ConstructorInfo cInfo = Type.GetType(typeName).GetConstructors()[0];
                            foreach (ParameterInfo pInfo in cInfo.GetParameters())
                            {
                                allTypes.Add(pInfo.ParameterType.AssemblyQualifiedName);
                            }
                        }
                        if ((typeName.Contains("PlayerVehicle") && !typeName.Contains("PlayerVehicleSetup")) || Type.GetType(typeName).IsAssignableFrom(typeof(PerVRDeviceLocalPosition)) || typeName.Contains("SteamId") || typeName.Contains("Friend") || Type.GetType(typeName).IsSubclassOf(typeof(ScriptableObject)) || typeName.Contains("AnimationCurve"))
                            return "ignore " + fieldName;
                        result = "newObject [{(" + fieldName + ")}] [{(" + typeName + " )}] [{(" + allTypes.Count + ")}] [{(" + ConfigNodeUtils.WriteList(allTypes) + ")}]";
                        if (field == null)
                            return result;
                        foreach (FieldInfo prop in fInfo)
                        {
                            result += "\n            " + newObjectIndent + convertField(prop.Name, prop.GetValue(field), prop.FieldType.AssemblyQualifiedName, newObjectIndent + "    ");
                        }
                    }
                }
            }
            return result;
        }

        //public string ToLiteral(string input) // from https://stackoverflow.com/a/14502246, however if I do end up usign it i should replace this with kano's method
        //{
        //    var literal = new StringBuilder(input.Length + 2);
        //    literal.Append("\"");
        //    foreach (var c in input)
        //    {
        //        switch (c)
        //        {
        //            case '\'': literal.Append(@"\'"); break;
        //            case '\"': literal.Append("\\\""); break;
        //            case '\\': literal.Append(@"\\"); break;
        //            case '\0': literal.Append(@"\0"); break;
        //            case '\a': literal.Append(@"\a"); break;
        //            case '\b': literal.Append(@"\b"); break;
        //            case '\f': literal.Append(@"\f"); break;
        //            case '\n': literal.Append(@"\n"); break;
        //            case '\r': literal.Append(@"\r"); break;
        //            case '\t': literal.Append(@"\t"); break;
        //            case '\v': literal.Append(@"\v"); break;
        //            default:
        //                if (Char.GetUnicodeCategory(c) != UnicodeCategory.Control)
        //                {
        //                    literal.Append(c);
        //                }
        //                else
        //                {
        //                    literal.Append(@"\u");
        //                    literal.Append(((ushort)c).ToString("x4"));
        //                }
        //                break;
        //        }
        //    }
        //    literal.Append("\"");
        //    return literal.ToString().Substring(1, literal.ToString().Length - 2);
        //}
    }

    public class SCPTextReader
    {
        public static Dictionary<string, Dictionary<string, List<Instruction>>> GetLinesFromFile(string path = @"C:\Users\tempe\Desktop\Ripped\VTOLVR\Assets\Simple Custom Planes\MyCoolPlane.SCP")
        {
            StreamReader reader = new StreamReader(path);
            Dictionary<string, Dictionary<string, List<Instruction>>> result = new Dictionary<string, Dictionary<string, List<Instruction>>>();

            Dictionary<string, List<Instruction>> currentDict = null;
            List<Instruction> currentList = null;

            string currentLine;
            Instruction currentInstruction = null;
            while ((currentLine = reader.ReadLine()) != null)
            {
                if (currentLine == null || currentLine.Length <= 0 || currentLine.Trim() == "END")
                    continue;
                if (currentLine[0] != ' ')
                {
                    currentDict = new Dictionary<string, List<Instruction>>();
                    //Debug.Log("adding line " + currentLine);
                    if (!result.ContainsKey(currentLine))
                        result.Add(currentLine, currentDict);
                }
                else if (currentLine[5] != ' ')
                {
                    currentList = new List<Instruction>();
                    while (currentDict.ContainsKey(currentLine))
                        currentLine += "(Copy)";
                    currentDict.Add(currentLine, currentList);
                    //Debug.Log("New dict: " + currentLine);
                }
                else if (currentLine.Length > 11 && currentLine.Substring(0, 12) == "            ")
                {
                    //Debug.Log("adding new sub instruction to instruction " + currentLine);
                    currentInstruction.AddInstruction(currentLine);
                }
                else
                {
                    if (currentLine.Trim().Length == 0)
                        continue;
                    currentInstruction = new Instruction(currentLine);
                    currentList.Add(currentInstruction);
                }
            }

            reader.Close();

            return result;
        }
        public class Instruction
        {
            public int indentation = 0;

            private string field;
            private string value;
            private string type;
            private List<string> allTypes = new List<string>();
            private List<Instruction> subInstructions = new List<Instruction>();
            private Instruction lastNewObject = null;

            public InstructionType instruction;

            public static bool operator ==(Instruction left, Instruction right)
            {
                if (left.field != right.field)
                    throw new ArgumentException("Left instruction field was " + left.field + " and right instruction field was " + right.field);
                return left.value == right.value;
            }

            public static bool operator !=(Instruction left, Instruction right)
            {
                if (left.instruction == InstructionType.ignore || right.instruction == InstructionType.ignore)
                    return true;


                if (left.instruction != right.instruction)
                    throw new ArgumentException("Left instruction type was " + left.instruction + " and right instruction type was " + right.instruction);

                if (left.instruction == InstructionType.array || left.instruction == InstructionType.newObject)
                {
                    if (left.GetDumpLine("") == right.GetDumpLine(""))
                        return false;
                    return true;
                }

                if (left.field != right.field)
                    throw new ArgumentException("Left instruction field was " + left.field + " and right instruction field was " + right.field);
                return left.value != right.value;
            }

            public override bool Equals(object obj)
            {
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return (field + type + value).GetHashCode();
            }

            public string GetDumpLine(string indentation)
            {
                string dumpLine = indentation;
                switch (instruction)
                {
                    case InstructionType.assign:
                        dumpLine += "assign " + field + " [{(" + type + ")}] [{(" + value + ")}]";
                        break;
                    case InstructionType.pointer:
                        dumpLine += "pointer " + field + " [{(" + value + ")}] [{(" + type + ")}]";
                        break;
                    case InstructionType.newObject:
                        dumpLine += "newObject [{(" + field + ")}] [{(" + type + " )}] [{(" + allTypes.Count + ")}] [{(" + ConfigNodeUtils.WriteList(allTypes) + ")}]";
                        foreach (Instruction sub in subInstructions)
                        {
                            dumpLine += "\n" + sub.GetDumpLine("    " + indentation);
                        }
                        break;
                    case InstructionType.array:
                        dumpLine += "array [{(" + type + ")}] [{(" + field + ")}] " + subInstructions.Count;
                        foreach (Instruction sub in subInstructions)
                        {
                            dumpLine += "\n" + sub.GetArrayLine("    " + indentation);
                        }
                        break;
                }
                return dumpLine;
            }

            public string GetArrayLine(string indentation)
            {
                string dumpLine = indentation;
                switch (instruction)
                {
                    case InstructionType.assign:
                        dumpLine += "assign " + field + " [{(" + type + ")}] [{(" + value + ")}]";
                        break;
                    case InstructionType.pointer:
                        dumpLine += "pointer " + field + " [{(" + type + ")}] [{(" + value + ")}]";
                        break;
                    case InstructionType.newObject:
                        dumpLine += "newObject [{(" + field + ")}] [{(" + type + " )}] [{(" + allTypes.Count + ")}] [{(" + ConfigNodeUtils.WriteList(allTypes) + ")}]";
                        foreach (Instruction sub in subInstructions)
                        {
                            dumpLine += "\n" + sub.GetDumpLine("    " + indentation);
                        }
                        break;
                    case InstructionType.array:
                        dumpLine = "array [{(" + type + ")}] [{(" + field + ")}] " + subInstructions.Count;
                        foreach (Instruction sub in subInstructions)
                        {
                            dumpLine += "\n" + sub.GetArrayLine("    " + indentation);
                        }
                        break;
                }
                return dumpLine;
            }

            public void AddInstruction(string line)
            {
                Instruction toAdd = new Instruction(line.Trim(), true);
                if (toAdd.instruction == InstructionType.ignore)
                    return;
                if (line.Length >= indentation + 15 && line.Substring(0, indentation + 15) == string.Concat(Enumerable.Repeat(" ", indentation + 15)))
                {
                    Debug.Log("Adding to LastNewObject " + line);
                    lastNewObject.AddInstruction(line);
                    return;
                }
                else if (toAdd.instruction == InstructionType.newObject || toAdd.instruction == InstructionType.array)
                {
                    Debug.Log("adding newobject with indentation of " + (4 + indentation) + " and line is " + line);
                    lastNewObject = toAdd;
                    lastNewObject.indentation = 4 + indentation;
                }
                subInstructions.Add(toAdd);
            }

            public Instruction(string newLine, bool isSubinstruction) // bool here is present for "fun" overloading
            {
                if (newLine[0] == 'a' && newLine[1] == 's')
                {
                    instruction = InstructionType.assign;
                    type = NextElement(newLine, 0);
                    value = NextElement(newLine, 1);
                    field = newLine.Substring(7, newLine.Substring(7).IndexOf(" "));
                }
                else if (newLine[0] == 'a' && newLine[1] == 'r')
                {
                    instruction = InstructionType.array;
                    type = NextElement(newLine, 0);
                    if (Type.GetType(type) == null)
                    {
                        field = NextElement(newLine, 0);
                        type = NextElement(newLine, 1);
                    }
                    else
                        field = NextElement(newLine, 1);
                }
                else if (newLine[0] == 'p')
                {
                    instruction = InstructionType.pointer;
                    newLine = newLine.Substring(8);
                    field = newLine.Substring(0, newLine.IndexOf(" "));
                    type = NextElement(newLine, 0);
                    value = NextElement(newLine, 1);
                }
                else if (newLine[0] == 'n')
                {
                    instruction = InstructionType.newObject;
                    field = NextElement(newLine, 0);
                    type = NextElement(newLine, 1);
                    if (int.Parse(NextElement(newLine, 2)) > 0)
                    {
                        allTypes = ConfigNodeUtils.ParseList(NextElement(newLine, 3));
                    }
                }
            }

            public Instruction(string newLine)
            {
                newLine = newLine.Trim();
                if (newLine[0] == 'a' && newLine[1] == 's')
                {
                    instruction = InstructionType.assign;
                    newLine = newLine.Substring(7);
                    field = newLine.Substring(0, newLine.IndexOf(" "));
                    type = NextElement(newLine, 0);
                    value = NextElement(newLine, 1);
                }
                else if (newLine[0] == 'p')
                {
                    instruction = InstructionType.pointer;
                    newLine = newLine.Substring(8);
                    field = newLine.Substring(0, newLine.IndexOf(" "));
                    newLine = newLine.Substring(newLine.IndexOf(" ") + 1);
                    value = NextElement(newLine, 0);
                    type = NextElement(newLine, 1);
                    //Debug.Log("New sub type: " + type + " value: " + value);
                }
                else if (newLine[0] == 'e')
                {
                    instruction = InstructionType.events;
                    newLine = newLine.Substring(7);
                    field = newLine.Substring(0, newLine.IndexOf(" "));
                    newLine = newLine.Substring(newLine.IndexOf(" ") + 1);
                    type = newLine.Substring(0, newLine.IndexOf(" ") + 1);
                    value = newLine.Substring(newLine.IndexOf(" ") + 1);
                }
                else if (newLine[0] == 'a' && newLine[1] == 'r')
                {
                    instruction = InstructionType.array;
                    type = NextElement(newLine, 0);
                    field = NextElement(newLine, 1);
                }
                else if (newLine[0] == 'n')
                {
                    instruction = InstructionType.newObject;
                    field = NextElement(newLine, 0);
                    type = NextElement(newLine, 1);
                    if (int.Parse(NextElement(newLine, 2)) > 0)
                    {
                        allTypes = ConfigNodeUtils.ParseList(NextElement(newLine, 3));
                    }
                }
            }

            public void Run(object original, Dictionary<string, Transform> allTransforms, bool log)
            {
                //Debug.Log("Running " + instruction + " " + UnityEngine.Random.Range(0, 999999));
                if (original == null)
                {
                    Debug.LogWarning("Original is null");
                    return;
                }
                if (instruction == InstructionType.assign)
                {
                    if (log)
                        Debug.Log("Try do assign instruction, value is " + value + " and type is " + type + " and field is " + field);
                    switch (type.Trim())
                    {
                        case "String":
                            if (log)
                                Debug.Log("Setting string for " + field + " and value " + value);
                            original.GetType().GetField(field).SetValue(original, value);
                            //Debug.Log("Set string for " + field + " and value " + value);
                            break;
                        case "Boolean":
                            if (log)
                                Debug.Log("Setting bool for field " + field + " and value " + value + " original " + original.GetType());
                            if (original is Renderer)
                                ((Renderer)original).enabled = value.Trim() == "True";
                            else
                                original.GetType().GetField(field).SetValue(original, value.Trim() == "True");
                            break;
                        case "Single":
                            if (log)
                                Debug.Log("Setting float for " + field + " and value " + value + " and original type is " + original.GetType().Name);
                            original.GetType().GetField(field).SetValue(original, float.Parse(value));
                            break;
                        case "Int32":
                            if (log)
                                Debug.Log("Setting int for " + field + " and value " + value);
                            original.GetType().GetField(field).SetValue(original, Int32.Parse(value));
                            break;
                        case "Double":
                            if (log)
                                Debug.Log("HOLY SHIT WE GOT A DOUBLE WHAT THE FUCK");
                            original.GetType().GetField(field).SetValue(original, double.Parse(value));
                            break;
                        default:
                            if (original.GetType().GetField(field) != null && original.GetType().GetField(field).FieldType.IsSubclassOf(typeof(Enum)))
                            {
                                if (log)
                                    Debug.Log("Setting enum for " + field + " and value " + value);
                                original.GetType().GetField(field).SetValue(original, int.Parse(value));
                            }
                            else
                                Debug.LogError("Couldn't resolve type for assign instruction of field " + field + " and type " + type);
                            break;
                    }
                }
                else if (instruction == InstructionType.pointer)
                {
                    // holy shit this is going to give me a fucking aneurism implementing i am not looking forward to this : It wasn't that bad honestly
                    if (log)
                        Debug.Log("Try do pointer instruction, value is " + value + " and type is " + type + " and field is " + field + " on " + original);
                    Type newType = Type.GetType(type.Trim());
                    if (newType == null)
                    {
                        newType = Type.GetType(value.Trim());
                        if (newType != null)
                        {
                            string temp = type;
                            type = value;
                            value = temp;
                            if (log)
                                Debug.Log("Switched values for strings instruction");
                        }
                        else
                        {
                            Debug.LogWarning("Couldn't get type for pointer " + type + " of value " + value);
                            return;
                        }
                    }
                    if (type.Contains("UnityEngine.GameObject"))
                    {
                        if (value.Trim() == "null")
                            original.GetType().GetField(field).SetValue(original, null);
                        else if (!allTransforms.ContainsKey(value.Trim()))
                            Debug.LogError("Couldn't resolve tf " + value.Trim());
                        else
                            original.GetType().GetField(field).SetValue(original, allTransforms[value.Trim()].gameObject);
                    }
                    else
                    {
                        if (original.GetType().GetField(field) == null)
                            Debug.LogError("Couldn't resolve field " + field.Trim() + " on tf " + value.Trim());
                        else
                        {
                            if (value.Trim() == "null")
                                original.GetType().GetField(field).SetValue(original, null);
                            else
                            {
                                try
                                {
                                    if (allTransforms[value.Trim()].gameObject.GetComponent(newType) != null)
                                        original.GetType().GetField(field).SetValue(original, allTransforms[value.Trim()].gameObject.GetComponent(newType));
                                }
                                catch (KeyNotFoundException e)
                                {
                                    Debug.LogError("Couldn't get value " + value.Trim() + " on field " + field);
                                }
                            }
                            //Debug.Log("Did pointer instruction, value is " + value + " and type is " + type + " and field is " + field + " on " + original.name);
                        }
                    }
                }
                else if (instruction == InstructionType.events)
                {
                    // DEEP VEIN THROMBOSIS (dvt) IS WHAT I'LL HAVE AFTER DOING THIS
                    //Debug.Log("Try do event, value is " + value + " and type is " + type + " and field is " + field);
                }
                else if (instruction == InstructionType.array)
                {
                    // This one was fun :) no it wasn't i wrote the comment when it was 95% done i am having a mental breakdown rn
                    Debug.Log("Doing array for type " + type + " of field " + field + " and count is: " + subInstructions.Count);
                    object value = original.GetType().GetField(field).GetValue(original);
                    if (value == null)
                    {
                        value = original.GetType().GetField(field).FieldType.GetConstructors()[0].Invoke(new object[] { 1 });
                    }
                    if (value is Array)
                    {
                        if (log)
                            Debug.Log("Doing array for type " + type + " of field " + field + " and count is: " + subInstructions.Count);
                        Array newArray = ResizeArray((Array)value, subInstructions.Count);
                        for (int i = 0; i < subInstructions.Count; i++)
                        {
                            Debug.Log("Sub instruction type is " + subInstructions[i].instruction);
                            newArray.SetValue(subInstructions[i].GetArrayValue(allTransforms), i);
                        }
                        original.GetType().GetField(field).SetValue(original, newArray);
                    }
                    else
                    {
                        if (log)
                            Debug.Log("Doing IList for type " + type + " of field " + field + " and count is: " + subInstructions.Count);
                        IList list = (IList)value;
                        try
                        {
                            list.Clear();
                        }
                        catch (NullReferenceException e)
                        {
                            Debug.LogError("Couldn't get list for field " + field + " of type " + type);
                            return;
                        }
                        for (int i = 0; i < subInstructions.Count; i++)
                        {
                            object toAdd = subInstructions[i].GetArrayValue(allTransforms);
                            if (toAdd == null)
                                continue;
                            list.Add(toAdd);
                        }
                        original.GetType().GetField(field).SetValue(original, list);
                    }
                }
                else if (instruction == InstructionType.newObject)
                {
                    object newObject = null;
                    if (type.Contains("PlayerVehicle"))
                        return;
                    List<Type> ctorTypes = new List<Type>();
                    foreach (string type in allTypes)
                    {
                        ctorTypes.Add(Type.GetType(type));
                    }
                    List<object> ctorObj = new List<object>();
                    foreach (Type newType in ctorTypes)
                    {
                        if (log)
                            Debug.Log("adding type " + newType.Name);
                        if (newType.IsValueType)
                            ctorObj.Add(Activator.CreateInstance(newType));
                        else
                            ctorObj.Add(null);
                    }
                    if (Type.GetType(type).GetConstructors().Length > 0)
                    {
                        try
                        {
                            newObject = Type.GetType(type).GetConstructor(ctorTypes.ToArray()).Invoke(ctorObj.ToArray());
                        }
                        catch (Exception e)
                        {
                            Debug.Log("The ctor nulled but idgaf.");
                        }
                    }
                    foreach (Instruction instruction in subInstructions)
                    {
                        //Debug.Log("Running instruction " + instruction.instruction + " of indentation " + instruction.indentation + " of field " + instruction.field + " of type " + instruction.type + " of value " + instruction.value);
                        instruction.Run(newObject, allTransforms, false);
                    }
                    original.GetType().GetField(field).SetValue(original, newObject);
                }
            }

            private object GetArrayValue(Dictionary<string, Transform> allTransforms) // this is a shit implementation
            {
                if (instruction == InstructionType.pointer)
                {
                    if (Type.GetType(type) == null)
                    {
                        string temp = type;
                        type = value;
                        value = temp;
                        //Debug.Log("Switched type and value, type is " + type + " and value is " + value);
                    }   
                    if (value.Trim() == "null")
                        return null;
                    try
                    {
                        bool test = allTransforms[value.Trim()] == null;
                    }
                    catch (KeyNotFoundException e)
                    {
                        Debug.LogError("Couldn't find transform for sub pointer instruction " + value.Trim());
                    }
                    //Debug.Log("Try do pointer sub instruction, value is " + value + " and type is " + type + " and field is " + field);
                    if (type.Contains("GameObject"))
                    {
                        //Debug.Log("Type contained GameObject");
                        return allTransforms[value.Trim()].gameObject;
                    }
                    Type newType = Type.GetType(type.Trim());
                    if (newType == null)
                    {
                        Debug.LogError("Type was null for type " + type);
                        return null;
                    }
                    return allTransforms[value.Trim()].gameObject.GetComponent(newType);
                }
                else if (instruction == InstructionType.assign)
                {
                    //Debug.Log("Try do assign sub instruction, value is " + value + " and type is " + type + " and field is " + field);
                    switch (type)
                    {
                        case "String":
                            return value;
                        case "Boolean":
                            //Debug.Log("Setting bool for " + field + " and value " + value + " original " + original.GetType());
                            return value.Trim() == "True";
                        case "Single":
                            //Debug.Log("Setting float for " + field + " and value " + value);
                            return float.Parse(value);
                        case "Int32":
                            //Debug.Log("Setting int for " + field + " and value " + value);
                            return Int32.Parse(value);
                        default:
                            Debug.LogWarning("default: couldn't resolve object for sub array " + type);
                            return null;
                    }
                }
                else if (instruction == InstructionType.events) // enums can be whatever the fuck i want :P
                {
                    // array code for weird classes at some point :(
                }
                else if (instruction == InstructionType.newObject)
                {
                    Debug.LogWarning("New object");
                    object newObject = null;
                    List<Type> ctorTypes = new List<Type>();
                    foreach (string type in allTypes)
                    {
                        ctorTypes.Add(Type.GetType(type));
                    }
                    List<object> ctorObj = new List<object>();
                    foreach (Type newType in ctorTypes)
                    {
                        if (newType.IsValueType)
                            ctorObj.Add(Activator.CreateInstance(newType));
                        else
                            ctorObj.Add(null);
                    }
                    if (!Type.GetType(type).IsValueType)
                        newObject = Type.GetType(type).GetConstructor(ctorTypes.ToArray()).Invoke(ctorObj.ToArray());
                    if (subInstructions != null && subInstructions.Count > 0) // this should never be null, so i'm very confused
                        foreach (Instruction instruction in subInstructions)
                        {
                            instruction.Run(newObject, allTransforms, false);
                        }
                    return newObject;
                }
                Debug.LogWarning("Couldn't resolve type for sub array.");
                return null;
            }

            public static System.Array ResizeArray(System.Array oldArray, int newSize) // from https://stackoverflow.com/a/4840857
            {
                System.Type elementType = oldArray.GetType().GetElementType();
                System.Array newArray = System.Array.CreateInstance(elementType, newSize);
                return newArray;
            }

            public enum InstructionType
            {
                ignore = 0,
                assign = 1,
                pointer = 2,
                events = 4,
                array = 8,
                newObject = 16
            }
        }
    }
}