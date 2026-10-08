using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_sendpositionupdate : MonoBehaviour
{
	public float sendrate;

	public float dataconter;

	public bool senddata;

	public sc_sendpositionupdate()
	{
		sendrate = 10f;
		senddata = true;
	}

	public void Start()
	{
	}

	public void Update()
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		if (senddata)
		{
			dataconter += 1000f * sendrate * Time.deltaTime;
			if (!(dataconter <= 1000f))
			{
				((Component)this).NetView().RPC("updateposition", (RPCMode)1, new object[1] { ((Component)this).transform.position });
				dataconter = 0f;
			}
		}
	}

	public void Main()
	{
	}
}
