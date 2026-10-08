using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_wheel : MonoBehaviour
{
	public Transform thewheel;

	public bool motorized;

	public float motorforce;

	public sc_wheel()
	{
		motorized = true;
		motorforce = 100f;
	}

	public void Main()
	{
	}
}
