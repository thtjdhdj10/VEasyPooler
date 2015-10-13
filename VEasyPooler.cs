﻿using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;

using Debug = UnityEngine.Debug;

public class VEasyPooler : MonoBehaviour
{
    public string originName;
    public string originTag;

    public int inActiveCount = 0;
    public int activeCount = 0;

    public List<GameObject> objectList = new List<GameObject>();
    // lists front are inactive objects, backward are active objects

    GameObject modelObject = null;

    void ExactlyLog(string str)
    {
        if (VEasyPoolerManager.manager.useDebugFlow == false)
            return;

        if (VEasyPoolerManager.IsExclude(originName, originTag) == true)
            return;

        Debug.Log(str);
    }

    public void SetModelObject(string name)
    {
        if (modelObject != null) return;

        originName = name;

        Object prefab = null;

        for (int i = 0; i < VEasyPoolerManager.manager.includePrefabPath.Count; ++i)
        {
            if (VEasyPoolerManager.manager.includePrefabPath[i] != "")
                prefab = Resources.Load(VEasyPoolerManager.manager.includePrefabPath[i] + "/" + originName, typeof(GameObject));
            else
                prefab = Resources.Load(originName, typeof(GameObject));

            if (prefab != null)
                break;
        }

        if (prefab == null)
        {
            Debug.LogWarning("\"" + originName + "\" is unvalid prefab name");
            originName = null;
            return;
        }

        ExactlyLog("new pool for " + originName);

        modelObject = MonoBehaviour.Instantiate(prefab) as GameObject;

        if (modelObject == null)
        {
            Debug.LogError("GameObject instantiate fail");
            originName = null;
            return;
        }

        originTag = modelObject.tag;

        ObjectState modelState = modelObject.AddComponent<ObjectState>();

        modelState.IsFinite = false;
        modelState.indexOfPool = -1;
        modelState.IsUse = false;
        modelState.OriginalName = originName;
        
        if(VEasyPoolerManager.manager.visualizeObjectList == true)
        {
            modelObject.name = originName + "(Origin)";
            modelObject.transform.parent = gameObject.transform;
        }
    }

    // add

    public void AddObjectFromHierarchyRequest(List<GameObject> list)
    {
        ExactlyLog("add " + originName + " * " + list.Count);

        for(int i = 0; i < list.Count; ++i)
        {
            ObjectState state = list[i].GetComponent<ObjectState>();
            if (state == null)
            {
                state = list[i].AddComponent<ObjectState>();
                state.IsFinite = false;
                state.IsUse = true;
                state.indexOfPool = inActiveCount + i;
                state.OriginalName = originName;
            }

            if (VEasyPoolerManager.manager.visualizeObjectList == true)
            {
                list[i].name = originName + "_" + (inActiveCount + i);
                list[i].transform.parent = gameObject.transform;
            }
        }

        activeCount += list.Count;

        objectList.AddRange(list);
    }

    // create

    public void CreateUsableObjectRequest(int count)
    {
        ExactlyLog("create inActive " + originName + " * " + count);

        for (int i = inActiveCount; i < inActiveCount + activeCount; ++i)
        {
            objectList[i].name = originName + "_" + (i + count);

            ObjectState state = objectList[i].GetComponent<ObjectState>();
            state.indexOfPool = i + count;
        }

        List<GameObject> newObjects = new List<GameObject>(count);
        
        for(int i = 0; i < count; ++i)
        {
            GameObject obj = MonoBehaviour.Instantiate(modelObject) as GameObject;

            if(obj == null)
            {
                Debug.LogError("GameObject instantiate fail");
                return;
            }


            ObjectState state = obj.GetComponent<ObjectState>();
            state.IsFinite = false;
            state.IsUse = false;
            state.indexOfPool = inActiveCount + i;
            state.OriginalName = originName;

            if (VEasyPoolerManager.manager.visualizeObjectList == true)
            {
                obj.name = originName + "_" + (inActiveCount + i);
                obj.transform.parent = gameObject.transform;
            }

            newObjects.Add(obj);
        }

        objectList.InsertRange(inActiveCount, newObjects);

        inActiveCount += count;
    }

    public List<GameObject> CreateUnusableObjectRequest(int count, bool active)
    {
        ExactlyLog("create active " + originName + " * " + count);

        for (int i = 0; i < count; ++i)
        {
            GameObject obj = MonoBehaviour.Instantiate(modelObject) as GameObject;

            if (obj == null)
            {
                Debug.LogError("GameObject instantiate fail");
                return null;
            }


            ObjectState state = obj.GetComponent<ObjectState>();
            state.IsFinite = false;
            state.IsUse = active;
            state.indexOfPool = objectList.Count;
            state.OriginalName = originName;

            if (VEasyPoolerManager.manager.visualizeObjectList == true)
            {
                obj.name = originName + "_" + objectList.Count;
                obj.transform.parent = gameObject.transform;
            }

            objectList.Add(obj);
        }

        activeCount += count;

        return objectList.GetRange(objectList.Count - count, count);
    }

    // get

    public List<GameObject> GetObjectRequest(int count, bool active)
    {
        ExactlyLog("get " + originName + " * " + count);

        int needCount = count - inActiveCount;
        
        if (needCount > 0)
        {
            List<GameObject> retObjects = CreateUnusableObjectRequest(needCount, active);
            if (retObjects == null) return null;

            int countMinusNeed = count - needCount;

            if (countMinusNeed > 0)
            {
                int startIdx = inActiveCount - countMinusNeed;

                List<GameObject> addList = GetObjectList(startIdx, countMinusNeed, active);

                retObjects.AddRange(addList);
            }

            return retObjects;
        }
        else
        {
            int getIndex = inActiveCount - count;

            return GetObjectList(getIndex, count, active);
        }
    }

    private List<GameObject> GetObjectList(int startIdx, int count, bool active)
    {
        inActiveCount -= count;
        activeCount += count;

        for (int i = startIdx; i < startIdx + count; ++i)
        {
            ObjectState state = objectList[i].GetComponent<ObjectState>();
            state.IsFinite = false;
            state.IsUse = active;
        }

        if(VEasyPoolerManager.manager.getOnResetTransform == true)
        {
            for (int i = startIdx; i < startIdx + count; ++i)
            {
                objectList[i].transform.position = modelObject.transform.position;
                objectList[i].transform.rotation = modelObject.transform.rotation;
                objectList[i].transform.localScale = modelObject.transform.localScale;
            }
        }

        return objectList.GetRange(startIdx, count);
    }

    public int GetObjectCountRequest(bool active)
    {
        if (active == true)
            return activeCount;
        else return inActiveCount;
    }

    // get finite

    public List<GameObject> GetFiniteObjectRequest(int count, bool active, float lifeTime)
    {
        List<GameObject> list = GetObjectRequest(count, active);

        for(int i = 0; i < count; ++i)
        {
            ObjectState state = list[i].GetComponent<ObjectState>();
            state.IsFinite = true;
            state.LifeTime = lifeTime;
        }

        return list;
    }

    // release

    public void ReleaseObjectRequest(GameObject obj)
    {
        ExactlyLog("release " + obj.name);

        List<GameObject> list = new List<GameObject>(1);
        list.Add(obj);
        ReleaseObjectRequest(list);
    }

    public void ReleaseObjectRequest(List<GameObject> obj)
    {
        ExactlyLog("release " + originName + " * " + obj.Count);

        for (int i = 0; i < obj.Count; ++i)
        {

            ObjectState releaseObjState = obj[i].GetComponent<ObjectState>();
            if (releaseObjState == null)
            {
                Debug.LogWarning("release request fail");
                Debug.LogWarning("\"" + obj[i].name + "\" have not ObjectState script");
                continue;
            }

            releaseObjState.IsFinite = false;
            releaseObjState.IsUse = false;

            int relIdx = releaseObjState.indexOfPool;
            if(relIdx < inActiveCount || relIdx >= objectList.Count)
            {
                Debug.LogWarning("release request fail");
                Debug.LogWarning("\"" + obj[i].name + "\" this object already released");
                //
                return;
            }

            int changeIdx = inActiveCount;
            if (changeIdx == relIdx)
            {
                inActiveCount += 1;
                activeCount -= 1;
                continue;
            }

            ObjectState changeObjState = objectList[changeIdx].GetComponent<ObjectState>();

            if (VEasyPoolerManager.manager.visualizeObjectList == true)
            {
                string tempStr = objectList[relIdx].name;
                objectList[relIdx].name = objectList[changeIdx].name;
                objectList[changeIdx].name = tempStr;
            }

            GameObject tempObj = objectList[relIdx];
            objectList[relIdx] = objectList[changeIdx];
            objectList[changeIdx] = tempObj;

            int tempInt = releaseObjState.indexOfPool;
            releaseObjState.indexOfPool = changeObjState.indexOfPool;
            changeObjState.indexOfPool = tempInt;

            inActiveCount += 1;
            activeCount -= 1;
        }
    }

    //
}
