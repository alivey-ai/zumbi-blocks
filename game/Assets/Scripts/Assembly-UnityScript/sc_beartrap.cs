using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_beartrap : MonoBehaviour
{
	public float conter;

	public int ownerid;

	public Transform bloodpart;

	public bool activated;

	public sc_beartrap()
	{
		conter = -1f;
		ownerid = -1;
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
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)(sc_player)(object)((Component)other).GetComponent(typeof(sc_player)) != (Object)null && !activated)
		{
			((sc_life)(object)((Component)other).gameObject.GetComponent(typeof(sc_life))).quantia = ((sc_life)(object)((Component)other).gameObject.GetComponent(typeof(sc_life))).quantia - 999999f;
			((Component)other).gameObject.NetView().RPC("hurtme", (RPCMode)1, new object[1] { 999999f });
			((Component)other).gameObject.NetView().RPC("spillblood", (RPCMode)2, new object[0]);
		}
		if ((Object)(object)(sc_zumbi)(object)((Component)other).GetComponent(typeof(sc_zumbi)) != (Object)null && !activated && ((sc_zumbi)(object)((Component)other).GetComponent(typeof(sc_zumbi))).state != 4)
		{
			((Component)other).gameObject.NetView().RPC("setkillerid", (RPCMode)2, new object[1] { ((sc_haveowner)(object)((Component)this).GetComponent(typeof(sc_haveowner))).id });
			((sc_life)(object)((Component)other).gameObject.GetComponent(typeof(sc_life))).hurtme(999999f);
			((Component)other).gameObject.NetView().RPC("pushme", (RPCMode)1, new object[0]);
			((Component)other).gameObject.NetView().RPC("hurtme", (RPCMode)1, new object[1] { 999999f });
		}
		if (!activated)
		{
			((Component)this).NetView().RPC("activatebeartrap", (RPCMode)5, new object[0]);
			((Component)(Animation)((Component)this).GetComponent(typeof(Animation))).GetComponent<Animation>()["chop"].speed = 10f;
			((Component)this).GetComponent<Animation>().Play("chop");
			activated = true;
			conter = 0f;
			Network.Instantiate((Object)(object)bloodpart, (((Component)other).transform.position + ((Component)this).transform.position) / 2f, bloodpart.rotation, 3);
		}
	}

	[RPC]
	public void activatebeartrap()
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		((Component)(Animation)((Component)this).GetComponent(typeof(Animation))).GetComponent<Animation>()["chop"].speed = 10f;
		((Component)this).GetComponent<Animation>().Play("chop");
		activated = true;
	}

	public void Update()
	{
		if (!(conter < 0f))
		{
			conter += 1f * Time.deltaTime;
			if (!(conter <= 20f))
			{
				((Component)this).NetView().RPC("killmebuffered", (RPCMode)5, new object[0]);
				Object.Destroy((Object)(object)((Component)this).gameObject);
			}
		}
	}

	public void Main()
	{
	}
}
