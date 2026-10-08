using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_dontdestroyme : MonoBehaviour
{
	public void Start()
	{
		Object.DontDestroyOnLoad((Object)(object)((Component)this).gameObject);
	}

	public void Main()
	{
	}
}
