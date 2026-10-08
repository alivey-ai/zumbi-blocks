using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_hidemeshonstart : MonoBehaviour
{
	public void Start()
	{
		((Component)this).GetComponent<Renderer>().enabled = false;
	}

	public void Update()
	{
	}

	public void Main()
	{
	}
}
