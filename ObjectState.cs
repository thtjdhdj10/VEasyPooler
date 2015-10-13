using UnityEngine;
using System.Collections;

public class ObjectState : MonoBehaviour {

    [SerializeField]
    private bool isFinite;
    public bool IsFinite
    {
        get
        {
            return IsFinite;
        }
        set
        {
            isFinite = value;
            if (value == false)
                lifeTime = -0.0f;
        }
    }

    [SerializeField]
    private float lifeTime;
    public float LifeTime
    {
        get
        {
            return lifeTime;
        }
        set
        {
            lifeTime = value;
            StopCoroutine("SelfReleaseTimer");
            StartCoroutine("SelfReleaseTimer");
        }
    }

    IEnumerator SelfReleaseTimer()
    {
        yield return new WaitForSeconds(lifeTime);

        if (isFinite == true)
            ObjectPoolManager.ReleaseObjectRequest(gameObject);
    }

    [System.NonSerialized]
    public int indexOfPool;
    
    private string originalName = null;
    public string OriginalName
    {
        get
        {
            return originalName;
        }
        set
        {
            if (originalName == null)
                originalName = value;
        }
    }

    private bool isUse;
    public bool IsUse
    {
        get
        {
            return isUse;
        }
        set
        {
            if (value == true)
                gameObject.SetActive(true);
            else
                gameObject.SetActive(false);

            isUse = value;
        }
    }


}
