using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_killmebuffered : MonoBehaviour
{
	[RPC]
	public void killmebuffered()
	{
		Object.Destroy((Object)(object)((Component)this).gameObject);
	}

	public void Main()
	{
	}
}
