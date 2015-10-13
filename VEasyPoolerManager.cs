﻿using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;


public class VEasyPoolerManager : MonoBehaviour
{
    public bool useDebugFlow = true;
    public bool getOnResetTransform = true;

    // use this, than can changing object`s name and parents
    public bool visualizeObjectList = true;

    [System.Serializable]
    private struct NameCount
    {
        public string name;
        public int count;
    }

    [SerializeField]
    private List<NameCount> prePoolingList = new List<NameCount>();
    public List<string> poolingFromHierarchy = new List<string>();
    public List<string> excludeLogTags = new List<string>();
    public List<string> excludeLogNames = new List<string>();
    public List<string> includePrefabPath = new List<string>();

    static Dictionary<string, VEasyPooler> poolDic = new Dictionary<string, VEasyPooler>();

    public static VEasyPoolerManager manager;


    public enum TargetObject
    {
        ACTIVE_ONLY = 0,
        INACTIVE_ONLY,
        BOTH_OBJECT,
    }

    void Awake()
    {

        manager = this;

        includePrefabPath.Add("");

        for (int i = 0; i < poolingFromHierarchy.Count; ++i)
        {
            PoolingObjectFromHierarchyRequest(poolingFromHierarchy[i]);
        }

        for (int i = 0; i < prePoolingList.Count; ++i)
        {
            CreateUsableObjectRequest(prePoolingList[i].name, prePoolingList[i].count);
        }

    }

    public delegate void ProcessingFunction(GameObject obj);

    public static void ProcessFunctionToObjects(ProcessingFunction func, string name, TargetObject to)
    {
        if (IsValidArgs(name) == false)
            return;

        List<GameObject> list = poolDic[name].objectList;

        int count = 0;
        int startIndex = 0;

        if (to == TargetObject.ACTIVE_ONLY)
        {
            count = poolDic[name].activeCount;
            startIndex = poolDic[name].inActiveCount;
        }
        else if (to == TargetObject.INACTIVE_ONLY)
            count = poolDic[name].inActiveCount;
        else
            count = poolDic[name].activeCount + poolDic[name].inActiveCount;

        for (int i = startIndex; i < startIndex + count; ++i)
        {
            func(list[i]);
        }
    }

    // add

    public static void PoolingObjectFromHierarchyRequest(string name)
    {
        if (IsValidArgs(name) == false)
            return;

        GameObject[] gameObjects = (GameObject[])GameObject.FindObjectsOfType(typeof(GameObject));
        List<GameObject> objectList = new List<GameObject>();

        for (int i = 0;i < gameObjects.Length; ++i)
        {
            // include same name, name (1), name (2) ...
            if(gameObjects[i].name == name ||
                gameObjects[i].name.Contains(name + " (") &&
                gameObjects[i].name.Contains(" " + name) == false)
            {
                if (gameObjects[i].GetComponent<ObjectState>() == null)
                    objectList.Add(gameObjects[i]);
            }
        }

        if (objectList.Count == 0)
            return;

        poolDic[name].AddObjectFromHierarchyRequest(objectList);
    }

    // create

    public static void CreateUsableObjectRequest(string name)
    {
        CreateUsableObjectRequest(name, 1);
    }

    public static void CreateUsableObjectRequest(string name, int count)
    {
        if (IsValidArgs(name, count) == false)
            return;

        poolDic[name].CreateUsableObjectRequest(count);
    }

    // get

    public static GameObject GetObjectRequest(string name)
    {
        List<GameObject> list = GetObjectRequest(name, 1);

        if (list == null) return null;
        return list[0];
    }

    public static List<GameObject> GetObjectRequest(string name, int count)
    {
        return GetObjectRequest(name, count, true);
    }

    public static List<GameObject> GetObjectRequest(string name, int count, bool active)
    {
        if (IsValidArgs(name, count) == false)
            return null;

        return poolDic[name].GetObjectRequest(count, active);
    }

    public static int GetObjectCountRequest(string name, bool active)
    {
        if (IsValidArgs(name) == false)
            return 0;

        return poolDic[name].GetObjectCountRequest(active);
    }

    // get finite

    public static GameObject GetFiniteObjectRequest(string name, float lifeTime)
    {
        List<GameObject> list = GetFiniteObjectRequest(name, 1, lifeTime);

        if (list == null) return null;
        return list[0];
    }

    public static List<GameObject> GetFiniteObjectRequest(string name, int count, float lifeTime)
    {
        return GetFiniteObjectRequest(name, count, true, lifeTime);
    }

    public static List<GameObject> GetFiniteObjectRequest(string name, int count, bool active, float lifeTime)
    {
        if (IsValidArgs(name, count, lifeTime) == false)
            return null;

        return poolDic[name].GetFiniteObjectRequest(count, active, lifeTime);
    }

    // release

    public static void ReleaseObjectRequest(GameObject obj)
    {
        List<GameObject> list = new List<GameObject>(1);
        list.Add(obj);

        ReleaseObjectRequest(list);
    }

    public static void ReleaseObjectRequest(List<GameObject> obj)
    {
        if (obj == null)
        {
            Debug.LogWarning("release request fail");
            return;
        }

        if (obj.Count == 0)
        {
            Debug.LogWarning("release request fail");
            Debug.LogWarning("wrong release request");
            Debug.LogWarning("this list.Count is zero");
            return;
        }
        
        ObjectState state = obj[0].GetComponent<ObjectState>();
        if (state == null)
        {
            Debug.LogWarning("release request fail");
            Debug.LogError(obj[0].name + " have not ObjectState script");
            return;
        }
        
        string name = state.OriginalName;
        if(IsValidArgs(name) == false)
        {
            Debug.LogWarning("release request fail");
            Debug.LogError("\"" + name + "\" is unvalid key");
            return;
        }

        if (obj.Count == 1)
            poolDic[name].ReleaseObjectRequest(obj[0]);
        else
            poolDic[name].ReleaseObjectRequest(obj);
    }

    // release with clear

    public static void ReleaseObjectWithClearRequest(GameObject obj)
    {
        List<GameObject> list = new List<GameObject>(1);
        list.Add(obj);

        ReleaseObjectWithClearRequest(list);
    }

    public static void ReleaseObjectWithClearRequest(List<GameObject> obj)
    {
        ReleaseObjectRequest(obj);
        obj.Clear();
    }

    //

    public static bool IsExclude(string name, string tag)
    {
        if (IsExcludeName(name) == true ||
            IsExcludeTag(tag) == true)
        {
            return true;
        }
        return false;
    }

    public static bool IsExcludeName(string name)
    {
        for (int i = 0; i < manager.excludeLogNames.Count; ++i)
        {
            if (manager.excludeLogNames[i] == name)
                return true;
        }

        return false;
    }

    public static bool IsExcludeTag(string tag)
    {
        for (int i = 0; i < manager.excludeLogTags.Count; ++i)
        {
            if (manager.excludeLogTags[i] == tag)
                return true;
        }

        return false;
    }

    private static bool IsValidArgs(string name, int count)
    {
        if (IsValidArgs(name) == false ||
            IsValidArgs(count) == false)
        {
            return false;
        }
        return true;
    }

    private static bool IsValidArgs(string name, float lifeTime)
    {
        if (IsValidArgs(name) == false ||
            IsValidArgs(lifeTime) == false)
        {
            return false;
        }
        return true;
    }

    private static bool IsValidArgs(string name, int count, float lifeTime)
    {
        if (IsValidArgs(name) == false ||
            IsValidArgs(count) == false ||
            IsValidArgs(lifeTime) == false)
        {
            return false;
        }
        return true;
    }

    private static bool IsValidArgs(string name)
    {
        if (name == null)
            return false;

        if (poolDic.ContainsKey(name) == false)
        {
            GameObject poolObj = new GameObject();
            poolObj.name = "pool " + name;

            VEasyPooler poolScript = poolObj.AddComponent<VEasyPooler>();
            poolDic.Add(name, poolScript);

            poolDic[name].SetModelObject(name);

            if (poolDic[name].originName == null)
            {
                poolDic.Remove(name);
                Destroy(poolObj);
                return false;
            }

            poolObj.transform.parent = manager.transform;
        }

        return true;
    }

    private static bool IsValidArgs(int count)
    {
        if (count < 0)
        {
            Debug.LogWarning(count + " this objectCount is too little");
            return false;
        }
        return true;
    }

    private static bool IsValidArgs(float lifeTime)
    {
        if (lifeTime < 0.0f)
        {
            Debug.LogWarning(lifeTime + " this lifeTime is too little");
            return false;
        }
        return true;
    }
}