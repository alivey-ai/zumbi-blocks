using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_landmine : MonoBehaviour
{
	public float conter;

	public int ownerid;

	public bool activated;

	public float exp_range;

	public float dano;

	public float push;

	public float shooter;

	public Transform std_particle;

	public sc_landmine()
	{
		conter = -1f;
		ownerid = -1;
		exp_range = 5f;
		dano = 5f;
		push = 5f;
		shooter = 5f;
	}

	public void Start()
	{
		if (Network.isClient)
		{
			((Behaviour)this).enabled = false;
		}
	}

	public void OnTriggerEnter(Collider other)
	{
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		if (!activated)
		{
			((sc_explosao)(object)((Component)this).GetComponent(typeof(sc_explosao))).blow_up("Player", exp_range, ((sc_haveowner)(object)((Component)this).GetComponent(typeof(sc_haveowner))).id, (int)dano, push);
			((sc_explosao)(object)((Component)this).GetComponent(typeof(sc_explosao))).blow_up("zumbi", exp_range, ((sc_haveowner)(object)((Component)this).GetComponent(typeof(sc_haveowner))).id, (int)dano, push);
			Network.Instantiate((Object)(object)std_particle, ((Component)this).transform.position, ((Component)this).transform.rotation, 3);
			activated = true;
			((Component)this).NetView().RPC("killmebuffered", (RPCMode)5, new object[0]);
			Object.Destroy((Object)(object)((Component)this).gameObject);
		}
	}

	public void Update()
	{
	}

	public void Main()
	{
	}
}
