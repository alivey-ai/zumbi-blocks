using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_barbwire : MonoBehaviour
{
	public int lifetime;

	public Transform bloodpart;

	public float dmgtime;

	public Transform soundef;

	public sc_barbwire()
	{
		lifetime = 25;
	}

	public void OnTriggerStay(Collider other)
	{
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)(sc_zumbi)(object)((Component)other).GetComponent(typeof(sc_zumbi)) != (Object)null && ((sc_zumbi)(object)((Component)other).GetComponent(typeof(sc_zumbi))).state != 4 && !(dmgtime > 0f))
		{
			((Component)other).gameObject.NetView().RPC("setkillerid", (RPCMode)2, new object[1] { -1 });
			((sc_life)(object)((Component)other).gameObject.GetComponent(typeof(sc_life))).hurtme(25f);
			((Component)other).gameObject.NetView().RPC("hurtme", (RPCMode)1, new object[1] { 25f });
			((Component)other).gameObject.NetView().RPC("pushme", (RPCMode)2, new object[0]);
			lifetime--;
			dmgtime = 0.25f;
			Network.Instantiate((Object)(object)bloodpart, (((Component)other).transform.position + ((Component)this).transform.position) / 2f, bloodpart.rotation, 3);
		}
	}

	public void OnTriggerEnter(Collider other)
	{
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)(sc_player)(object)((Component)other).GetComponent(typeof(sc_player)) != (Object)null && !(dmgtime > 0f))
		{
			((sc_life)(object)((Component)other).gameObject.GetComponent(typeof(sc_life))).quantia = ((sc_life)(object)((Component)other).gameObject.GetComponent(typeof(sc_life))).quantia - 10f;
			((Component)other).gameObject.NetView().RPC("hurtme", (RPCMode)1, new object[1] { 10f });
			((Component)other).gameObject.NetView().RPC("spillblood", (RPCMode)2, new object[0]);
			lifetime--;
			dmgtime = 0.25f;
			Network.Instantiate((Object)(object)bloodpart, (((Component)other).transform.position + ((Component)this).transform.position) / 2f, bloodpart.rotation, 3);
		}
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
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		if (!(dmgtime <= 0f))
		{
			dmgtime -= Time.deltaTime;
		}
		if (lifetime <= 0)
		{
			if ((Object)(object)soundef != (Object)null)
			{
				Network.Instantiate((Object)(object)soundef, ((Component)this).transform.position, ((Component)this).transform.rotation, 11);
			}
			((Component)((Component)this).transform.parent).NetView().RPC("killmebuffered", (RPCMode)5, new object[0]);
			Object.Destroy((Object)(object)((Component)((Component)this).transform.parent).gameObject);
		}
	}

	public void Main()
	{
	}
}
