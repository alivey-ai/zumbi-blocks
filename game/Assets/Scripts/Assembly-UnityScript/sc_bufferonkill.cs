using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_bufferonkill : MonoBehaviour
{
	public void OnDestroy()
	{
		((Component)this).NetView().RPC("killmebuffered", (RPCMode)6, new object[0]);
	}

	public void Main()
	{
	}
}
