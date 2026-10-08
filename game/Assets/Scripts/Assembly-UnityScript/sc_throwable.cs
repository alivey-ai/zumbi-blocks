using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_throwable : MonoBehaviour
{
	public float throw_force;

	public Transform throw_prefab;

	public bool cinturavel;

	public int decayend;

	public bool islight;

	public bool isgrenade;

	public sc_throwable()
	{
		throw_force = 10f;
		decayend = 100;
	}

	public void Start()
	{
	}

	public void Main()
	{
	}
}
