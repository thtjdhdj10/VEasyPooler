using UnityEngine;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;

using Debug = UnityEngine.Debug;

public class ObjectPool : MonoBehaviour
{
    public string originName;
    public string originTag;

    public int unActiveCount = 0;
    public int activeCount = 0;

    public List<GameObject> objectList = new List<GameObject>();
    // lists front are usable objects, backward are unusable objects

    GameObject modelObject = null;

    public void SetModelObject(string name)
    {
        if (modelObject != null) return;

        originName = name;

        Object prefab = null;

        for (int i = 0; i < ObjectPoolManager.manager.includePrefabPath.Count; ++i)
        {
            if (ObjectPoolManager.manager.includePrefabPath[i] != "")
            {
                prefab = Resources.Load(ObjectPoolManager.manager.includePrefabPath[i] + "/" + originName, typeof(GameObject));
            }
            else
            {
                prefab = Resources.Load(originName, typeof(GameObject));
            }

            if (prefab != null)
                break;
        }

        if (prefab == null)
        {
            Debug.LogWarning("\"" + originName + "\" is unvalid prefab name");
            originName = null;
            return;
        }

        if (ObjectPoolManager.IsExclude(originName, originTag) == false)
            Debug.Log("new pool for " + originName);

        modelObject = MonoBehaviour.Instantiate(prefab) as GameObject;

        if (modelObject == null)
        {
            Debug.LogError("GameObject instantiate fail");
            originName = null;
            return;
        }

        originTag = modelObject.tag;

        modelObject.name = originName + "(Origin)";

        ObjectState modelState = modelObject.AddComponent<ObjectState>();

        modelState.IsFinite = false;
        modelState.indexOfPool = -1;
        modelState.IsUse = false;
        modelState.OriginalName = originName;

        modelObject.transform.parent = gameObject.transform;
    }

    // add

    public void AddObjectFromHierarchyRequest(List<GameObject> list)
    {
        if (ObjectPoolManager.IsExclude(originName, originTag) == false)
            Debug.Log("add " + originName + " * " + list.Count);

        for (int i = 0; i < list.Count; ++i)
        {
            list[i].name = originName + "_" + (unActiveCount + i);

            ObjectState state = list[i].GetComponent<ObjectState>();
            if (state == null)
            {
                state = list[i].AddComponent<ObjectState>();
                state.IsFinite = false;
                state.IsUse = true;
                state.indexOfPool = unActiveCount + i;
                state.OriginalName = originName;
            }

            list[i].transform.parent = gameObject.transform;
        }

        activeCount += list.Count;

        objectList.AddRange(list);
    }

    // create

    public void CreateUsableObjectRequest(int count)
    {
        if (ObjectPoolManager.IsExclude(originName, originTag) == false)
            Debug.Log("create unActive " + originName + " * " + count);

        for (int i = unActiveCount; i < unActiveCount + activeCount; ++i)
        {
            objectList[i].name = originName + "_" + (i + count);

            ObjectState state = objectList[i].GetComponent<ObjectState>();
            state.indexOfPool = i + count;
        }

        List<GameObject> newObjects = new List<GameObject>(count);

        for (int i = 0; i < count; ++i)
        {
            GameObject obj = MonoBehaviour.Instantiate(modelObject) as GameObject;

            if (obj == null)
            {
                Debug.LogError("GameObject instantiate fail");
                return;
            }

            obj.name = originName + "_" + (unActiveCount + i);

            ObjectState state = obj.GetComponent<ObjectState>();
            state.IsFinite = false;
            state.IsUse = false;
            state.indexOfPool = unActiveCount + i;
            state.OriginalName = originName;

            obj.transform.parent = gameObject.transform;

            newObjects.Add(obj);
        }

        objectList.InsertRange(unActiveCount, newObjects);

        unActiveCount += count;
    }

    public List<GameObject> CreateUnusableObjectRequest(int count, bool active)
    {
        if (ObjectPoolManager.IsExclude(originName, originTag) == false)
            Debug.Log("create active " + originName + " * " + count);

        for (int i = 0; i < count; ++i)
        {
            GameObject obj = MonoBehaviour.Instantiate(modelObject) as GameObject;

            if (obj == null)
            {
                Debug.LogError("GameObject instantiate fail");
                return null;
            }

            obj.name = originName + "_" + objectList.Count;

            ObjectState state = obj.GetComponent<ObjectState>();
            state.IsFinite = false;
            state.IsUse = active;
            state.indexOfPool = objectList.Count;
            state.OriginalName = originName;

            obj.transform.parent = gameObject.transform;

            objectList.Add(obj);
        }

        activeCount += count;

        return objectList.GetRange(objectList.Count - count, count);
    }

    // get

    public List<GameObject> GetObjectRequest(int count, bool active)
    {
        if (ObjectPoolManager.IsExclude(originName, originTag) == false)
            Debug.Log("get " + originName + " * " + count);

        int needCount = count - unActiveCount;

        if (needCount > 0)
        {
            List<GameObject> retObjects = CreateUnusableObjectRequest(needCount, active);
            if (retObjects == null) return null;

            int countMinusNeed = count - needCount;

            if (countMinusNeed > 0)
            {
                int startIdx = unActiveCount - countMinusNeed;

                List<GameObject> addList = GetObjectList(startIdx, countMinusNeed, active);

                retObjects.AddRange(addList);
            }

            return retObjects;
        }
        else
        {
            int getIndex = unActiveCount - count;

            return GetObjectList(getIndex, count, active);
        }
    }

    private List<GameObject> GetObjectList(int startIdx, int count, bool active)
    {
        unActiveCount -= count;
        activeCount += count;

        for (int i = startIdx; i < startIdx + count; ++i)
        {
            objectList[i].transform.position = modelObject.transform.position;
            objectList[i].transform.rotation = modelObject.transform.rotation;
            objectList[i].transform.localScale = modelObject.transform.localScale;

            ObjectState state = objectList[i].GetComponent<ObjectState>();
            state.IsFinite = false;
            state.IsUse = active;
        }

        return objectList.GetRange(startIdx, count);
    }

    public int GetObjectCountRequest(bool active)
    {
        if (active == true)
            return activeCount;
        else return unActiveCount;
    }

    // get finite

    public List<GameObject> GetFiniteObjectRequest(int count, bool active, float lifeTime)
    {
        List<GameObject> list = GetObjectRequest(count, active);

        for (int i = 0; i < count; ++i)
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
        if (ObjectPoolManager.IsExclude(originName, originTag) == false)
            Debug.Log("release " + obj.name);

        List<GameObject> list = new List<GameObject>(1);
        list.Add(obj);
        ReleaseObjectRequest(list);
    }

    public void ReleaseObjectRequest(List<GameObject> obj)
    {
        if (ObjectPoolManager.IsExclude(originName, originTag) == false && obj.Count != 1)
            Debug.Log("release " + originName + " * " + obj.Count);

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
            if (relIdx < unActiveCount || relIdx >= objectList.Count)
            {
                Debug.LogWarning("release request fail");
                Debug.LogWarning("\"" + obj[i].name + "\" this object already released");
                //
                return;
            }

            int changeIdx = unActiveCount;
            if (changeIdx == relIdx)
            {
                unActiveCount += 1;
                activeCount -= 1;
                continue;
            }

            ObjectState changeObjState = objectList[changeIdx].GetComponent<ObjectState>();

            string tempStr = objectList[relIdx].name;
            objectList[relIdx].name = objectList[changeIdx].name;
            objectList[changeIdx].name = tempStr;

            GameObject tempObj = objectList[relIdx];
            objectList[relIdx] = objectList[changeIdx];
            objectList[changeIdx] = tempObj;

            int tempInt = releaseObjState.indexOfPool;
            releaseObjState.indexOfPool = changeObjState.indexOfPool;
            changeObjState.indexOfPool = tempInt;

            unActiveCount += 1;
            activeCount -= 1;
        }
    }

    //
}

