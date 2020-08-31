
/*
 *	Unit Object Pooler Implemenation Created By Jared Massa
 *
 *	Copyright (c) 2020 Jared Massa
 *
 *	Permission is hereby granted, free of charge, to any person obtaining a copy
 *	of this software and associated documentation files (the "Software"), to deal
 *	in the Software without restriction, including without limitation the rights
 *	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *	copies of the Software, and to permit persons to whom the Software is
 *	furnished to do so, subject to the following conditions:
 *
 *	The above copyright notice and this permission notice shall be included in all
 *	copies or substantial portions of the Software.
 *
 *	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *	SOFTWARE.
 */
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#region PoolableType Definition
[System.Serializable]
public class PoolableType
{
    [Tooltip(@"The Name of this Type of Poolable Object (Does not effect operation)")]
    public string TypeName;

    [Tooltip(@"The Prefab of the gameobject to be pooled")]
	public GameObject Prefab;

    [Tooltip(@"The sorting tag is used to organize object pools.  Each object in a given pool will have the tag set.")]
    [TagSelector] public string SortingTag = "";

    [Tooltip(@"The maximum number of objects to use.")]
    public int Max;

    [Tooltip(@"If true, the object pool can ignore the maximum if there are no objects in storage, and another is requested.")]
    public bool AutoExpand;
}
#endregion

public class ObjectPooler : MonoBehaviour
{
    #region Variables
    /// <summary>
    /// A static instance of the ObjectPooler
    /// </summary>
	private static ObjectPooler Instance;

    [Tooltip("If true, the Object Pooler will organize individual object pools under assigned parent transforms. Only works if checked before starting the game.")]
    [SerializeField]
    private bool UseParentTransforms = false;

    [Tooltip(@"A list of Poolable Type Definitions")]
    [SerializeField]
    private List<PoolableType> PoolableTypes = new List<PoolableType>();


    // private variables
    private Dictionary<string, PoolableType> TagTypeLookup;
    private Dictionary<GameObject, PoolableType> PrefabTypeLookup;
    private Dictionary<string, Queue<GameObject>> SleepingObjects;
    private Dictionary<string, HashSet<GameObject>> ActiveObjects;
    private Dictionary<string, Transform> TypeParents;
    #endregion

    #region Initialize
    private void Awake()
    {
        Instance = this;
        // init dictionaries
        if (UseParentTransforms)
            TypeParents = new Dictionary<string, Transform>();
        TagTypeLookup = new Dictionary<string, PoolableType>();
        PrefabTypeLookup = new Dictionary<GameObject, PoolableType>();
        SleepingObjects = new Dictionary<string, Queue<GameObject>>();
        ActiveObjects = new Dictionary<string, HashSet<GameObject>>();
        // init types and sets in dicts
        foreach (PoolableType type in PoolableTypes)
        {
            if (UseParentTransforms)
            {
                GameObject Par = new GameObject(type.TypeName + " Parent");
                Par.transform.parent = transform;
                TypeParents[type.SortingTag] = Par.transform;
            }
            // add reference to type
            TagTypeLookup.Add(type.SortingTag, type);
            PrefabTypeLookup.Add(type.Prefab, type);
            SleepingObjects.Add(type.SortingTag, new Queue<GameObject>());
            ActiveObjects.Add(type.SortingTag, new HashSet<GameObject>());
            // init sleeping objects
            for (int i = 0; i < type.Max; i++)
            {
                GameObject t = Instantiate(type.Prefab, UseParentTransforms ? TypeParents[type.SortingTag] : transform);
                t.tag = type.SortingTag;
                t.SetActive(false);
                SleepingObjects[type.SortingTag].Enqueue(t);
            }
        }
    }
    #endregion

    #region Destroy
    public static void Destroy(GameObject obj) => Instance._Destroy(obj);
    /// <summary>
    /// Deactivate an active object, and enqueue it for later restoration.
    /// </summary>
    /// <param name="obj">The active object to deactivate</param>
    private void _Destroy(GameObject obj)
    {
        // check if this object is one we actually pool
        if (!ActiveObjects.ContainsKey(obj.tag)) return;
        // check if this object is actually in our active set right now
        if (!ActiveObjects[obj.tag].Contains(obj)) return;
        // since it is, we will deactivate the object
        obj.SetActive(false);
        // then add it to the sleeping queue
        SleepingObjects[obj.tag].Enqueue(obj);
        // and remove it from the active set
        ActiveObjects[obj.tag].Remove(obj);
    }
    #endregion

    #region Instantiate
    /// <summary>
    /// Grab a member from the list of SleepingObjects
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="Identifier">Either the Prefab to instantiate or the Tag for the PoolableType</param>
    /// <param name="Position">The Position to activate the object at</param>
    /// <param name="Rotation">The Rotation to activate the object with</param>
    /// <returns>A gameobject from the correct pool</returns>
    public static GameObject GetObject<T>(T Identifier, Vector3 Position, Quaternion Rotation) =>
        Instance._GetObject(Identifier, Position, Rotation);

    private GameObject _GetObject<T>(T Identifier, Vector3 Position, Quaternion Rotation)
    {
        // get the tag of the prefab we wish to instantiate
        string Tag = "";
        // user passed us a tag
        if (Identifier is string)
        {
            if (!SleepingObjects.ContainsKey(Identifier as string)) return null;
            else Tag = Identifier as string;
        }
        // user passed us a prefab
        else if (Identifier is GameObject)
        {
            if (!PrefabTypeLookup.ContainsKey(Identifier as GameObject)) return null;
            else Tag = PrefabTypeLookup[Identifier as GameObject].SortingTag;
        }
        else return null;

        // If we make it here, we know the tag has been set and exists.
        GameObject Member = null;
        // if no objects are left in the queue, but we can auto expand, then make a new object
        if (SleepingObjects[Tag].Count == 0 && TagTypeLookup[Tag].AutoExpand)
            Member = Instantiate(TagTypeLookup[Tag].Prefab, UseParentTransforms ? TypeParents[Tag] : transform);
        // else if there are members in the queue get the member from the sleeping queue
        else if (SleepingObjects[Tag].Count > 0)
            Member = SleepingObjects[Tag].Dequeue();
        // else we cannot instantiate
        else return null;

        // Now that we have a member, we can update it's variables and return it
        Member.transform.position = Position;
        Member.transform.rotation = Rotation;
        ActiveObjects[Tag].Add(Member);
        // can't forget to do this!
        Member.SetActive(true);

        return Member;
    }
    #endregion
}

#region TagSelectorDefinition
#if UNITY_EDITOR
//Original by DYLAN ENGELMAN http://jupiterlighthousestudio.com/custom-inspectors-unity/
//Altered by Brecht Lecluyse http://www.brechtos.com
public class TagSelectorAttribute : PropertyAttribute
{
    public bool UseDefaultTagFieldDrawer = false;
}
[CustomPropertyDrawer(typeof(TagSelectorAttribute))]
public class TagSelectorPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType == SerializedPropertyType.String)
        {
            EditorGUI.BeginProperty(position, label, property);

            var attrib = attribute as TagSelectorAttribute;

            if (attrib.UseDefaultTagFieldDrawer)
                property.stringValue = EditorGUI.TagField(position, label, property.stringValue);

            else
            {
                //generate the taglist + custom tags
                List<string> tagList = new List<string>();
                tagList.AddRange(UnityEditorInternal.InternalEditorUtility.tags);
                string propertyString = property.stringValue;
                int index = -1;
                if (propertyString == "")
                {
                    //The tag is empty
                    index = 0; //first index is the special <notag> entry
                }
                else
                {
                    //check if there is an entry that matches the entry and get the index
                    //we skip index 0 as that is a special custom case
                    for (int i = 1; i < tagList.Count; i++)
                    {
                        if (tagList[i] == propertyString)
                        {
                            index = i;
                            break;
                        }
                    }
                }

                //Draw the popup box with the current selected index
                index = EditorGUI.Popup(position, label.text, index, tagList.ToArray());

                //Adjust the actual string value of the property based on the selection
                if (index == 0)
                    property.stringValue = "";
                else if (index >= 1)
                    property.stringValue = tagList[index];
                else
                    property.stringValue = "";
            }

            EditorGUI.EndProperty();
        }
        else
            EditorGUI.PropertyField(position, property, label);
    }
}
#endif
#endregion
