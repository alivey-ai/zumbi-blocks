using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_canbetaken : MonoBehaviour
{
	public int itemid;

	public int quant;

	public int ammo;

	public Transform gamecontrol;

	public GUISkin guiskin;

	public float decaytime;

	public bool decay;

	public sc_canbetaken()
	{
		decaytime = 15000f;
	}

	[RPC]
	public void grabitem(int playerid)
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
			array[num].NetView().RPC("receiveitem", (RPCMode)2, new object[3] { itemid, quant, ammo });
		}
		((Component)this).NetView().RPC("destroymeitem", (RPCMode)5, new object[0]);
		Object.Destroy((Object)(object)((Component)this).gameObject);
	}

	public void Start()
	{
		gamecontrol = GameObject.Find("gamecontroler").transform;
	}

	public void Update()
	{
		if (((Component)this).NetView().isMine && decay)
		{
			decaytime -= 1000f * Time.deltaTime;
			if (!(decaytime > 0f))
			{
				((Component)this).NetView().RPC("destroymeitem", (RPCMode)5, new object[0]);
				Object.Destroy((Object)(object)((Component)this).gameObject);
			}
		}
	}

	public void OnGUI()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		GUI.skin = guiskin;
		Vector3 val = default(Vector3);
		float num = 0f;
		num = Vector3.Distance(((Component)this).transform.position, ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).cam.position);
		val = ((Component)((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).cam).GetComponent<Camera>().WorldToScreenPoint(((Component)this).transform.position);
		val.y = (float)Screen.height - val.y;
		if (!(val.z <= 0f) && !(num <= 2.7f))
		{
			num = 25f - num;
			if (!(num >= 0f))
			{
				num = 0f;
			}
			float a = 0.3f * (num / 25f);
			Color color = GUI.color;
			float num2 = (color.a = a);
			Color val2 = (GUI.color = color);
			GUI.Label(new Rect(val.x - 200f, val.y - 20f, 400f, 40f), ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[itemid].nome);
		}
	}

	[RPC]
	public void destroymeitem()
	{
		Object.Destroy((Object)(object)((Component)this).gameObject);
	}

	public void Main()
	{
	}
}
