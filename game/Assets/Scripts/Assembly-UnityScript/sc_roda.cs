using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_roda : MonoBehaviour
{
	public bool touching;

	public void OnCollisionEnter(Collision collision)
	{
		touching = true;
	}

	public void OnCollisionExit(Collision collision)
	{
		touching = false;
	}

	public void Main()
	{
	}
}
