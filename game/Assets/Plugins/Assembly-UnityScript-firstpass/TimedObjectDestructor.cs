using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class TimedObjectDestructor : MonoBehaviour
{
	public float timeOut;

	public bool detachChildren;

	public TimedObjectDestructor()
	{
		timeOut = 1f;
	}

	public void Awake()
	{
		((MonoBehaviour)this).Invoke("DestroyNow", timeOut);
	}

	public void DestroyNow()
	{
		if (detachChildren)
		{
			((Component)this).transform.DetachChildren();
		}
		Object.DestroyObject((Object)(object)((Component)this).gameObject);
	}

	public void Main()
	{
	}
}
