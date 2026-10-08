using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_manequim : MonoBehaviour
{
	public GameObject animadorleg;

	public GameObject animadorarm;

	public Transform[] skinpart;

	public Transform[] shirtpart;

	public Transform bearpos;

	public void Start()
	{
		animadorleg.GetComponent<Animation>()["standing"].speed = 0.2f;
	}

	public void Main()
	{
	}
}
