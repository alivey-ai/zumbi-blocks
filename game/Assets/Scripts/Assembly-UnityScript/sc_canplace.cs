using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_canplace : MonoBehaviour
{
	public Renderer[] rends;

	public bool willnotcan;

	public bool can;

	public float willcan;

	public Material[] mats;

	public sc_canplace()
	{
		willcan = -1f;
	}

	public void OnTriggerStay(Collider other)
	{
		can = false;
		willcan = 0f;
	}

	public void Start()
	{
	}

	public void Update()
	{
		willcan += 1f * Time.deltaTime;
		if (!(willcan <= 0.05f))
		{
			can = true;
		}
		if (can)
		{
			for (int i = 0; i < rends.Length; i++)
			{
				rends[i].material = mats[0];
			}
		}
		else
		{
			for (int i = 0; i < rends.Length; i++)
			{
				rends[i].material = mats[1];
			}
		}
	}

	public void Main()
	{
	}
}
