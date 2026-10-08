using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_terraanim : MonoBehaviour
{
	public float conter;

	public float timetokill;

	public sc_terraanim()
	{
		timetokill = 4.5f;
	}

	public void Start()
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		((Component)(Animation)((Component)this).GetComponent(typeof(Animation))).GetComponent<Animation>()["rise2"].speed = 0.8f;
		if (!((Component)this).NetView().isMine)
		{
			((Behaviour)this).enabled = false;
		}
	}

	public void Update()
	{
		if (((Component)this).NetView().isMine)
		{
			conter += 100f * Time.deltaTime;
			if (!(conter <= timetokill * 100f))
			{
				((Component)this).NetView().RPC("killmebuffered", (RPCMode)6, new object[0]);
			}
		}
	}

	public void Main()
	{
	}
}
