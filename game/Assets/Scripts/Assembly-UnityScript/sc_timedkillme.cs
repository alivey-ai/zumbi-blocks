using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_timedkillme : MonoBehaviour
{
	public float lifetime;

	public float conter;

	public sc_timedkillme()
	{
		lifetime = 2f;
	}

	public void Start()
	{
		if (!((Component)this).NetView().isMine)
		{
			((Behaviour)this).enabled = false;
		}
	}

	public void Update()
	{
		if (!(conter >= lifetime))
		{
			conter += 1f * Time.deltaTime;
			if (!(conter < lifetime))
			{
				((Component)this).NetView().RPC("killmebuffered", (RPCMode)6, new object[0]);
			}
		}
	}

	public void Main()
	{
	}
}
