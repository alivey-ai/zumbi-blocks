using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_deadplayer : MonoBehaviour
{
	public float conter;

	public int lifetime;

	public int dietime;

	public int state;

	public Transform head;

	public Transform headmesh;

	public Transform luzi;

	public bool died;

	public bool reviving;

	public Transform myspawner;

	public Transform gamecontrol;

	public Transform cam;

	public string playername;

	public Transform playerprefab;

	public float mouse_sense;

	public int myid;

	public bool autoreload;

	public int[] invent;

	public int[] ammo;

	public int[] quant;

	public GUISkin guiskin;

	public int lifes;

	public Transform[] skinpos;

	public Transform[] camisapos;

	public Transform beardpos;

	public Transform beard;

	public int beardnum;

	public int beardcolor;

	public int myskin;

	public int shirtcolor;

	public Transform[] beardprefabs;

	public Material[] skincolors;

	public Material[] camisacolors;

	public Material[] haircolors;

	public bool ap_synced;

	public Transform playerfab;

	public sc_deadplayer()
	{
		lifetime = 1500;
		dietime = 1000;
		lifes = 3;
	}

	[RPC]
	public void dead_ask_aparence()
	{
		if (((Component)this).NetView().isMine)
		{
			((Component)this).NetView().RPC("dead_sync_aparence", (RPCMode)1, new object[4] { beardnum, myskin, beardcolor, shirtcolor });
		}
	}

	[RPC]
	public void dead_sync_aparence(int RPCbeardnum, int RPCmyskin, int RPCbeardcolor, int RPCshirtcolor)
	{
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Expected O, but got Unknown
		myskin = RPCmyskin;
		beardcolor = RPCbeardcolor;
		shirtcolor = RPCshirtcolor;
		for (int i = 0; i < skinpos.Length; i++)
		{
			((Component)skinpos[i]).GetComponent<Renderer>().material = ((sc_player)(object)((Component)playerfab).GetComponent(typeof(sc_player))).skincolors[RPCmyskin];
		}
		for (int i = 0; i < camisapos.Length; i++)
		{
			((Component)camisapos[i]).GetComponent<Renderer>().material = ((sc_player)(object)((Component)playerfab).GetComponent(typeof(sc_player))).camisacolors[RPCshirtcolor];
		}
		if ((Object)(object)beard != (Object)null)
		{
			Object.Destroy((Object)(object)((Component)beard).gameObject);
		}
		if (RPCbeardnum >= 0)
		{
			beard = (Transform)Object.Instantiate((Object)(object)((sc_player)(object)((Component)playerfab).GetComponent(typeof(sc_player))).beardprefabs[RPCbeardnum], beardpos.position, beardpos.rotation);
			beard.parent = beardpos;
			((sc_reference)(object)((Component)beard).GetComponent(typeof(sc_reference))).refer.GetComponent<Renderer>().material = ((sc_player)(object)((Component)playerfab).GetComponent(typeof(sc_player))).haircolors[RPCbeardcolor];
		}
		ap_synced = true;
	}

	public void Start()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		if (!((Component)this).NetView().isMine)
		{
			((Behaviour)this).enabled = false;
		}
		((Component)this).GetComponent<Rigidbody>().AddForce(Vector3.up * 300f);
		conter = 0f;
	}

	public void Update()
	{
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		if (state == 0 && !reviving)
		{
			conter += 100f * Time.deltaTime;
			if (!(conter <= (float)lifetime))
			{
				state = 1;
				conter = 0f;
				((Component)this).NetView().RPC("setdied", (RPCMode)6, new object[0]);
				((sc_spawner)(object)((Component)myspawner).GetComponent(typeof(sc_spawner))).playerobj = null;
				((sc_scenecontroller)(object)((Component)((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).scenecontroller).GetComponent(typeof(sc_scenecontroller))).observator.eulerAngles = Vector3.zero;
				cam.parent = ((sc_scenecontroller)(object)((Component)((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).scenecontroller).GetComponent(typeof(sc_scenecontroller))).observator;
				cam.localPosition = Vector3.zero;
				cam.localEulerAngles = Vector3.zero;
				for (num = 0; num < 10; num++)
				{
					Vector3 zero = Vector3.zero;
					zero = ((Component)this).transform.position;
					zero.x += Random.Range(-1f, 1f);
					zero.z += Random.Range(-1f, 1f);
					zero.y += Random.Range(-0.5f, 0.5f);
					if (Network.isClient)
					{
						((Component)myspawner).NetView().RPC("dropitem", (RPCMode)0, new object[5]
						{
							invent[num],
							zero,
							((Component)this).transform.rotation,
							quant[num],
							ammo[num]
						});
					}
					else
					{
						((sc_spawner)(object)((Component)myspawner).GetComponent(typeof(sc_spawner))).dropitem(invent[num], zero, ((Component)this).transform.rotation, quant[num], ammo[num]);
					}
				}
			}
		}
		if (state == 1)
		{
			conter += 100f * Time.deltaTime;
			if (!(conter <= (float)dietime))
			{
				((Component)this).NetView().RPC("killmebuffered", (RPCMode)6, new object[0]);
			}
		}
	}

	public void OnGUI()
	{
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		GUI.skin = guiskin;
		if (!died)
		{
			num = (int)(((float)lifetime - conter) / 100f + 1f);
			if (!reviving)
			{
				GUI.Label(new Rect((float)(Screen.width / 2 - 300), (float)(Screen.height / 2 - 25), 600f, 50f), ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).language[((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).languageset].frase[52] + num);
			}
			else
			{
				GUI.Label(new Rect((float)(Screen.width / 2 - 300), (float)(Screen.height / 2 - 25), 600f, 50f), ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).language[((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).languageset].frase[69]);
			}
		}
	}

	[RPC]
	public void revive()
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Expected O, but got Unknown
		if (((Component)this).NetView().isMine && state == 0 && !died)
		{
			Transform val = (Transform)Network.Instantiate((Object)(object)playerprefab, new Vector3(((Component)this).transform.position.x, ((Component)this).transform.position.y + 2.1f, ((Component)this).transform.position.z), playerprefab.rotation, 0);
			((sc_player)(object)((Component)val).GetComponent(typeof(sc_player))).beardnum = beardnum;
			((sc_player)(object)((Component)val).GetComponent(typeof(sc_player))).shirtcolor = shirtcolor;
			((sc_player)(object)((Component)val).GetComponent(typeof(sc_player))).beardcolor = beardcolor;
			((sc_player)(object)((Component)val).GetComponent(typeof(sc_player))).myskin = myskin;
			((sc_player)(object)((Component)val).GetComponent(typeof(sc_player))).cam = cam;
			((sc_player)(object)((Component)val).GetComponent(typeof(sc_player))).gamecontrol = gamecontrol;
			((sc_player)(object)((Component)val).GetComponent(typeof(sc_player))).playername = playername;
			((sc_player)(object)((Component)val).GetComponent(typeof(sc_player))).mouse_sense = mouse_sense;
			((sc_player)(object)((Component)val).GetComponent(typeof(sc_player))).myid = myid;
			((sc_player)(object)((Component)val).GetComponent(typeof(sc_player))).lifes = lifes;
			((Component)(sc_player)(object)((Component)val).GetComponent(typeof(sc_player))).NetView().RPC("setid", (RPCMode)1, new object[1] { myid });
			((sc_player)(object)((Component)val).GetComponent(typeof(sc_player))).autoreload = autoreload;
			((sc_player)(object)((Component)val).GetComponent(typeof(sc_player))).myspawner = myspawner;
			((sc_player)(object)((Component)val).GetComponent(typeof(sc_player))).controled = true;
			((Component)val).GetComponent<Rigidbody>().isKinematic = false;
			((Behaviour)(sc_player)(object)((Component)val).GetComponent(typeof(sc_player))).enabled = true;
			((Behaviour)(sc_onlinecontroller)(object)((Component)val).GetComponent(typeof(sc_onlinecontroller))).enabled = false;
			((sc_spawner)(object)((Component)myspawner).GetComponent(typeof(sc_spawner))).playerobj = val;
			for (int i = 0; i < ((sc_player)(object)((Component)val).GetComponent(typeof(sc_player))).item.Length; i++)
			{
				((sc_player)(object)((Component)val).GetComponent(typeof(sc_player))).item[i] = invent[i];
				((sc_player)(object)((Component)val).GetComponent(typeof(sc_player))).ammo[i] = ammo[i];
				((sc_player)(object)((Component)val).GetComponent(typeof(sc_player))).quant[i] = quant[i];
			}
			((Component)this).NetView().RPC("killmebuffered", (RPCMode)6, new object[0]);
		}
	}

	[RPC]
	public void setdied()
	{
		died = true;
	}

	[RPC]
	public void setreviving(bool isit)
	{
		reviving = isit;
	}

	public void Main()
	{
	}
}
