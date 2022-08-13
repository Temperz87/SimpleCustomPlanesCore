using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;
using System.IO;
using UnityEngine.Events;
using System.Text;
using System.Globalization;
using System.Linq;

public class SimpleCustomPlane : MonoBehaviour
{
    public void LoadOntoThisPlane(string path, GameObject prefab)
    {
        GameObject newPrefab = Instantiate(prefab, transform);
        newPrefab.SetActive(true);
        Load(path);
    }

    public void LoadOntoThisConfigurator(string path)
    {
        Load(path);
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
        if (verboseLogs)
            Debug.Log("transform was hashed as hashed " + transform.name);
        return transform.name;
    }

    public void Load(string path)
    {
        List<string> allNames = new List<string>();
        Dictionary<string, string> hashedToOriginal = new Dictionary<string, string>();
        foreach (Transform tf in GetComponentsInChildren<Transform>(true))
        {
            string originalName = tf.name;
            allTfs.Add(HashTransform(tf, allNames), tf);
            hashedToOriginal.Add(tf.name, originalName);
        }
        Dictionary<string, Dictionary<string, List<SCPTextReader.Instruction>>> allInstructions = SCPTextReader.GetLinesFromFile(path);
        foreach (string line in allInstructions.Keys) // we must hash them first so we actually get their names
        {
            Dictionary<string, List<SCPTextReader.Instruction>> currentObject = allInstructions[line];

            if (line.IndexOf(" ") <= 0)
            {
                Debug.LogWarning("Got an index less than 0 for space.");
                continue;
            }

            if (!allTfs.ContainsKey(NextElement(line, 0)))
            {
                Debug.LogWarning("Couldn't resolve object " + NextElement(line, 0));
                continue;
            }

            Transform currentTf = allTfs[NextElement(line, 0)];
            Transform currentParent = null;
            Transform newParent = null;

            string finderString = NextElement(line, 1);
            if (!finderString.Contains("NULL"))
            {
                //Debug.Log("Got string " + finderString + " on " + currentTf.name);
                currentParent = allTfs[finderString];
            }
            finderString = NextElement(line, 2);
            if (!finderString.Contains("NULL"))
            {
                //Debug.Log("Got string " + finderString + " again on " + currentTf.name);
                newParent = allTfs[finderString];
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
                if (verboseLogs)
                    Debug.Log("Try do object " + currentTf.name + " for component " + component.Trim());
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
                if (verboseLogs)
                    Debug.Log("dict    " + type.AssemblyQualifiedName + " has a length of " + instructions.Count + " on " + currentTf.name);
                foreach (SCPTextReader.Instruction instruction in instructions)
                {
                    if (instruction == null)
                        Debug.LogError("How the fuck was an instruction null?");
                    else
                    {
                        instruction.Run(currentComponent, allTfs);
                    }
                }
            }
        }
        foreach (Transform tf in GetComponentsInChildren<Transform>(true))
        {
            if (tf.name.Contains("(Clone)"))
                tf.name = tf.name.Substring(0, tf.name.IndexOf("(Clone)"));
            if (verboseLogs)
                Debug.Log("tf name was " + tf.name);
            tf.name = hashedToOriginal[tf.name];
            if (verboseLogs)
                Debug.Log("tf name is now " + tf.name);
        }
        Debug.Log("Loaded from file!");
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

    public bool verboseLogs = false;


    private Dictionary<string, Transform> allTfs = new Dictionary<string, Transform>();

    public class SCPTextReader
    {
        public static Dictionary<string, Dictionary<string, List<Instruction>>> GetLinesFromFile(string path)
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
                    Debug.Log("adding line " + currentLine);
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
                else if (currentLine.Substring(0, 12) == "            ")
                {
                    Debug.Log("adding new sub instruction to instruction " + currentLine);
                    currentInstruction.AddInstruction(currentLine);
                }
                else
                {
                    currentInstruction = new Instruction(currentLine);
                    Debug.Log("new instruction " + currentLine);
                    currentList.Add(currentInstruction);
                }
            }

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

            public Instruction(string newLine, bool isSubInstruction) // you can pass in anything for the second arg, i'm just overloading here
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

            public void Run(object original, Dictionary<string, Transform> allTransforms)
            {
                if (original == null)
                {
                    Debug.LogWarning("Original is null");
                    return;
                }
                //Debug.Log("Running " + instruction + " type is " + type + " field is " + field + " value is " + value);
                if (instruction == InstructionType.assign)
                {
                    //Debug.Log("Try do assign instruction, value is " + value + " and type is " + type + " and field is " + field);
                    FieldInfo fInfo = original.GetType().GetField(field.Trim());
                    if (fInfo == null && !(original is Renderer))
                    {
                        Debug.LogError("info was null on object " + original + " while trying to find field " + field + " of type " + type);
                        return;
                    }
                    switch (type.Trim())
                    {
                        case "String":
                            Debug.Log("Setting string for " + field + " and value " + value);
                            fInfo.SetValue(original, value);
                            //Debug.Log("Set string for " + field + " and value " + value);
                            break;
                        case "Boolean":
                            Debug.Log("Setting bool for " + field + " and value " + value + " original " + original.GetType());
                            if (original is Renderer)
                                ((Renderer)original).enabled = value.Trim() == "True";
                            else
                                fInfo.SetValue(original, value.Trim() == "True");
                            break;
                        case "Single":
                            Debug.Log("Setting float for " + field + " and value " + value);
                            fInfo.SetValue(original, float.Parse(value));
                            break;
                        case "Int32":
                            Debug.Log("Setting int for " + field + " and value " + value);
                            fInfo.SetValue(original, Int32.Parse(value));
                            break;
                        case "Double":
                            Debug.Log("HOLY SHIT WE GOT A DOUBLE WHAT THE FUCK");
                            original.GetType().GetField(field).SetValue(original, double.Parse(value));
                            break;
                        default:
                            if (original.GetType().GetField(field) != null && original.GetType().GetField(field).FieldType.IsSubclassOf(typeof(Enum)))
                            {
                                //Debug.Log("Setting enum for " + field + " and value " + value);
                                original.GetType().GetField(field).SetValue(original, int.Parse(value));
                            }
                            else
                                Debug.LogError("Couldn't resolve type for assign instruction of field " + field);
                            break;
                    }
                }
                else if (instruction == InstructionType.pointer)
                {
                    // holy shit this is going to give me a fucking aneurism implementing i am not looking forward to this
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
                            Debug.LogError("Couldn't resolve field " + field.Trim() + " on tf " + value.Trim() + " for " + original);
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
                    Debug.Log("try do enumerable of field " + field + " and type " + type + " and sub instruction count is " + subInstructions.Count);
                    object value = original.GetType().GetField(field).GetValue(original);
                    if (value == null)
                    {
                        value = original.GetType().GetField(field).FieldType.GetConstructors()[0].Invoke(new object[] { 1 });
                    }
                    if (value is Array)
                    {
                        Debug.Log("try do array of field " + field + " and type " + type);
                        Array newArray = ResizeArray((Array)value, subInstructions.Count);
                        for (int i = 0; i < subInstructions.Count; i++)
                            newArray.SetValue(subInstructions[i].GetArrayValue(allTransforms), i);
                        original.GetType().GetField(field).SetValue(original, newArray);
                    }
                    else
                    {
                        Debug.Log("Doing IList for type " + type + " of field " + field + " and count is: " + subInstructions.Count);
                        IList list = (IList)value;
                        try
                        {
                            list.Clear();
                        }
                        catch (NullReferenceException e)
                        {
                            Debug.LogError("Couldn't get list");
                            return;
                        }
                        for (int i = 0; i < subInstructions.Count; i++)
                        {
                            list.Add(subInstructions[i].GetArrayValue(allTransforms));
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
                        instruction.Run(newObject, allTransforms);
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
                        // Debug the instruction type
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
                else if (instruction == InstructionType.events)
                {
                    // event code at some point :( 
                }
                else if (instruction == InstructionType.newObject)
                {
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
                    Debug.Log("newObject sub type is " + type + " and field is " + field);
                    if (!Type.GetType(type).IsValueType)
                        newObject = Type.GetType(type).GetConstructor(ctorTypes.ToArray()).Invoke(ctorObj.ToArray());
                    if (subInstructions != null && subInstructions.Count > 0) // this should never be null, so i'm very confused
                        foreach (Instruction instruction in subInstructions)
                        {
                            instruction.Run(newObject, allTransforms);
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
