using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_lootdroper : MonoBehaviour
{
	public Transform destrosso;

	public int lootid;

	public int lootquant;

	public int lootammo;

	public Transform soundef;

	public sc_lootdroper()
	{
		lootid = -1;
		lootammo = 1;
	}

	public void Start()
	{
		if (Network.isClient)
		{
			((Behaviour)this).enabled = false;
		}
	}

	public void Update()
	{
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		if (((sc_life)(object)((Component)this).GetComponent(typeof(sc_life))).quantia > 0f)
		{
			return;
		}
		if (lootid >= 0)
		{
			GameObject val = GameObject.Find("matchcontroller(Clone)");
			if ((Object)(object)val != (Object)null)
			{
				((sc_matchcontroller)(object)val.GetComponent(typeof(sc_matchcontroller))).spawnitem(lootid, ((Component)this).transform.position, ((Component)this).transform.rotation, lootquant, lootammo, true);
			}
		}
		if ((Object)(object)soundef != (Object)null)
		{
			Network.Instantiate((Object)(object)soundef, ((Component)this).transform.position, ((Component)this).transform.rotation, 11);
		}
		((Component)this).NetView().RPC("destroy_droploot", (RPCMode)1, new object[0]);
		((Component)this).NetView().RPC("killmebuffered", (RPCMode)5, new object[0]);
		destroy_droploot();
		((sc_killmebuffered)(object)((Component)this).GetComponent(typeof(sc_killmebuffered))).killmebuffered();
	}

	[RPC]
	public void destroy_droploot()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)destrosso != (Object)null)
		{
			Object.Instantiate((Object)(object)destrosso, ((Component)this).transform.position, ((Component)this).transform.rotation);
		}
	}

	public void Main()
	{
	}
}
