using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_turnofflightmapp : MonoBehaviour
{
	public void Start()
	{
		((Component)this).GetComponent<Renderer>().lightmapIndex = 255;
	}

	public void Update()
	{
	}

	public void Main()
	{
	}
}
