using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_fortification : MonoBehaviour
{
	public Transform ghost_fab;

	public Transform ghost;

	public Transform fort_fab;

	public int fabid;

	public string hold_anim;

	public float upintheair;

	public float downintheground;

	public sc_fortification()
	{
		upintheair = 1f;
	}

	public void Start()
	{
	}

	public void Update()
	{
	}

	public void Main()
	{
	}
}
