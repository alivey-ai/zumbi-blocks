using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_suspension : MonoBehaviour
{
	public Rigidbody rootcar;

	public float wheelforce;

	public bool touching;

	public bool trassao;

	public sc_suspension()
	{
		wheelforce = 5f;
		trassao = true;
	}

	public void OnTriggerStay(Collider other)
	{
		touching = true;
	}

	public void OnTriggerExit(Collider other)
	{
		touching = false;
	}

	public void Update()
	{
	}

	public void Main()
	{
	}
}
