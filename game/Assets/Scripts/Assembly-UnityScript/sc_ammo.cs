using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_ammo : MonoBehaviour
{
	public int gunid;

	[RPC]
	public void grabammo(int playerid)
	{
		if (!((Component)this).NetView().isMine)
		{
			return;
		}
		GameObject[] array = null;
		array = GameObject.FindGameObjectsWithTag("Player");
		int i = 0;
		int num = -1;
		for (; i < array.Length; i++)
		{
			if (((sc_player)(object)array[i].GetComponent(typeof(sc_player))).myid == playerid)
			{
				num = i;
				i = array.Length;
			}
		}
		if (num >= 0)
		{
			array[num].NetView().RPC("receiveammo", (RPCMode)2, new object[2]
			{
				gunid,
				((sc_canbetaken)(object)((Component)this).GetComponent(typeof(sc_canbetaken))).ammo
			});
		}
		((Component)this).NetView().RPC("destroymeitem", (RPCMode)5, new object[0]);
		Object.Destroy((Object)(object)((Component)this).gameObject);
	}

	public void Main()
	{
	}
}
