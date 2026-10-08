using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_zumbi : MonoBehaviour
{
	public AudioClip[] sounds;

	public AudioSource[] soundsources;

	public float sendrate;

	public float dataconter;

	public bool senddata;

	public AAA_PATHFINDING aaa_pf_component;

	public int path_state;

	public int targetnum;

	public bool havepath;

	public Vector3 lastsawpos;

	public Vector3 postogo;

	public int path_stage;

	public float grav;

	public float walkspeed;

	public float spinspeed;

	public float dmgmin;

	public float dmgmax;

	public float atackreach;

	public int frontsight;

	public int backsight;

	public Transform armpos;

	public Transform gamecontrol;

	public Transform animador;

	public Animation animadorcomp;

	public Transform head;

	public Transform headmesh;

	public int state;

	public float vy;

	public bool isalive;

	public bool isgrounded;

	public Vector3 movimento;

	public float actconter;

	public int actchance;

	public float actrate;

	public bool isacting;

	public int acttime;

	public Transform ragdoll;

	public Transform eyepos;

	public Transform[] parts;

	public Transform[] peles;

	public Material[] skincolor;

	public int skinnum;

	public Transform[] camisas;

	public Material[] camisacolor;

	public int camisanum;

	public Transform alvo;

	public Vector3 alvorelativepos;

	public float animationcont;

	public bool RTH;

	public Vector3 lastforce;

	public bool needtopush;

	public Transform backpackobject;

	public bool carryitem;

	public Transform matchcontrol;

	public float headlife;

	public Transform headblowpart;

	public int killerid;

	public GameObject[] brains;

	public GameObject lastalvoarea;

	public GameObject mylastarea;

	public float timetogiveup;

	public float timetogiveup2;

	public float changealvo_cont;

	public string cur_anim;

	public sc_zumbi()
	{
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		sendrate = 10f;
		senddata = true;
		grav = 10f;
		walkspeed = 20f;
		spinspeed = 50f;
		dmgmin = 5f;
		dmgmax = 10f;
		atackreach = 1.5f;
		frontsight = 6;
		backsight = 3;
		isalive = true;
		isgrounded = true;
		movimento = Vector3.zero;
		actchance = 50;
		actrate = 2f;
		RTH = true;
		lastforce = Vector3.zero;
		headlife = 60f;
		killerid = -1;
	}

	public void Start()
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Expected O, but got Unknown
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		brains = GameObject.FindGameObjectsWithTag("Player");
		((Component)this).NetView().RPC("asktheanimation", (RPCMode)0, new object[0]);
		int num = 0;
		if (!((Component)this).NetView().isMine)
		{
			((Behaviour)this).enabled = false;
		}
		animadorcomp = (Animation)((Component)animador).GetComponent(typeof(Animation));
		((Component)animadorcomp).GetComponent<Animation>()["idle"].speed = 0.2f;
		((Component)animadorcomp).GetComponent<Animation>()["look"].speed = 0.2f;
		((Component)animadorcomp).GetComponent<Animation>()["walkslow"].speed = 0.2f * walkspeed;
		((Component)animadorcomp).GetComponent<Animation>()["walkfast"].speed = 0.2f * walkspeed;
		((Component)animadorcomp).GetComponent<Animation>()["atackstrong"].speed = 1.1f;
		((Component)animadorcomp).GetComponent<Animation>()["fall"].speed = 0.2f;
		((Component)animadorcomp).GetComponent<Animation>()["born"].speed = 0.35f;
		((Component)animadorcomp).GetComponent<Animation>()["stepback"].speed = 0.5f;
		int num2 = Random.Range(0, 360);
		Vector3 eulerAngles = ((Component)this).transform.eulerAngles;
		float num3 = (eulerAngles.y = num2);
		Vector3 val = (((Component)this).transform.eulerAngles = eulerAngles);
		animationcont = 0f;
		state = -1;
		((Component)this).GetComponent<Rigidbody>().constraints = (RigidbodyConstraints)126;
		((Component)animador).GetComponent<Animation>().Play("born");
		((Component)this).NetView().RPC("playanim", (RPCMode)1, new object[2]
		{
			"born",
			((Component)animadorcomp).GetComponent<Animation>()["born"].speed
		});
		skinnum = Random.Range(0, skincolor.Length);
		camisanum = Random.Range(0, camisacolor.Length);
		((Component)this).NetView().RPC("setskin", (RPCMode)6, new object[3] { skinnum, camisanum, carryitem });
	}

	[RPC]
	public void asktheanimation()
	{
		if (!Network.isClient)
		{
			((Component)this).NetView().RPC("playanim", (RPCMode)1, new object[2]
			{
				cur_anim,
				((Component)animadorcomp).GetComponent<Animation>()[cur_anim].speed
			});
		}
	}

	[RPC]
	public void pushme()
	{
		needtopush = true;
	}

	[RPC]
	public void hurthead(float dmg)
	{
		headlife -= dmg;
		if ((Object)(object)alvo == (Object)null)
		{
			headlife -= dmg * 2f;
		}
	}

	[RPC]
	public void hearnoise(int pid)
	{
		int num = 0;
		if (state == -1 || !((Object)(object)alvo == (Object)null))
		{
			return;
		}
		brains = GameObject.FindGameObjectsWithTag("Player");
		for (num = 0; num < brains.Length; num++)
		{
			if (((sc_player)(object)brains[num].GetComponent(typeof(sc_player))).myid == pid)
			{
				alvo = brains[num].transform;
				lastalvoarea = ((sc_findmyarea)(object)((Component)alvo).GetComponent(typeof(sc_findmyarea))).myarea;
				break;
			}
		}
	}

	[RPC]
	public void setskin(int skin, int camisa, bool bakpak)
	{
		skinnum = skin;
		camisanum = camisa;
		carryitem = bakpak;
		if (!carryitem)
		{
			Object.Destroy((Object)(object)((Component)backpackobject).gameObject);
		}
		for (int i = 0; i < peles.Length; i++)
		{
			((Component)peles[i]).GetComponent<Renderer>().material = skincolor[skin];
		}
		for (int i = 0; i < camisas.Length; i++)
		{
			((Component)camisas[i]).GetComponent<Renderer>().material = camisacolor[camisa];
		}
	}

	public void LateUpdate()
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		//IL_031c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0326: Unknown result type (might be due to invalid IL or missing references)
		//IL_032b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0333: Unknown result type (might be due to invalid IL or missing references)
		//IL_0338: Unknown result type (might be due to invalid IL or missing references)
		//IL_0344: Unknown result type (might be due to invalid IL or missing references)
		//IL_0359: Unknown result type (might be due to invalid IL or missing references)
		//IL_035e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0365: Unknown result type (might be due to invalid IL or missing references)
		//IL_036a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0376: Unknown result type (might be due to invalid IL or missing references)
		//IL_2ad0: Unknown result type (might be due to invalid IL or missing references)
		//IL_2ad5: Unknown result type (might be due to invalid IL or missing references)
		//IL_2ae1: Unknown result type (might be due to invalid IL or missing references)
		//IL_2ae7: Invalid comparison between Unknown and I4
		//IL_2aec: Unknown result type (might be due to invalid IL or missing references)
		//IL_2af2: Invalid comparison between Unknown and I4
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_2b1b: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_061d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0622: Unknown result type (might be due to invalid IL or missing references)
		//IL_0627: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_06be: Unknown result type (might be due to invalid IL or missing references)
		//IL_06d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_136c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1371: Unknown result type (might be due to invalid IL or missing references)
		//IL_13bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_13c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_140c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1411: Unknown result type (might be due to invalid IL or missing references)
		//IL_0954: Unknown result type (might be due to invalid IL or missing references)
		//IL_0959: Unknown result type (might be due to invalid IL or missing references)
		//IL_1472: Unknown result type (might be due to invalid IL or missing references)
		//IL_1477: Unknown result type (might be due to invalid IL or missing references)
		//IL_1492: Unknown result type (might be due to invalid IL or missing references)
		//IL_1497: Unknown result type (might be due to invalid IL or missing references)
		//IL_14ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_14b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_132b: Unknown result type (might be due to invalid IL or missing references)
		//IL_1330: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dd3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dd8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e00: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e05: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e30: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e35: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e50: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e55: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c18: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c1d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c45: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c4a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c75: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c7a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c95: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c9a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1742: Unknown result type (might be due to invalid IL or missing references)
		//IL_174d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1756: Unknown result type (might be due to invalid IL or missing references)
		//IL_14d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_14d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_14e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_14e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_14eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_14f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_14fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_1443: Unknown result type (might be due to invalid IL or missing references)
		//IL_1448: Unknown result type (might be due to invalid IL or missing references)
		//IL_1459: Unknown result type (might be due to invalid IL or missing references)
		//IL_145e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1341: Unknown result type (might be due to invalid IL or missing references)
		//IL_1346: Unknown result type (might be due to invalid IL or missing references)
		//IL_1183: Unknown result type (might be due to invalid IL or missing references)
		//IL_1188: Unknown result type (might be due to invalid IL or missing references)
		//IL_11a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_11a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_11d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_11d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_11f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_11f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fd6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fdb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ff5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ffa: Unknown result type (might be due to invalid IL or missing references)
		//IL_1025: Unknown result type (might be due to invalid IL or missing references)
		//IL_102a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1045: Unknown result type (might be due to invalid IL or missing references)
		//IL_104a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0edf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ee4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0eff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f04: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e85: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e8a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ea5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0eaa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d24: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d29: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d44: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d49: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cca: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ccf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cea: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cef: Unknown result type (might be due to invalid IL or missing references)
		//IL_151b: Unknown result type (might be due to invalid IL or missing references)
		//IL_1527: Unknown result type (might be due to invalid IL or missing references)
		//IL_154d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1281: Unknown result type (might be due to invalid IL or missing references)
		//IL_1286: Unknown result type (might be due to invalid IL or missing references)
		//IL_12a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_12a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_1227: Unknown result type (might be due to invalid IL or missing references)
		//IL_122c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1247: Unknown result type (might be due to invalid IL or missing references)
		//IL_124c: Unknown result type (might be due to invalid IL or missing references)
		//IL_10d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_10d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_10f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_10f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_107a: Unknown result type (might be due to invalid IL or missing references)
		//IL_107f: Unknown result type (might be due to invalid IL or missing references)
		//IL_109a: Unknown result type (might be due to invalid IL or missing references)
		//IL_109f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f34: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f39: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f54: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f59: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d79: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d7e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d99: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d9e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b40: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b45: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b4c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b57: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b60: Unknown result type (might be due to invalid IL or missing references)
		//IL_12d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_12db: Unknown result type (might be due to invalid IL or missing references)
		//IL_12f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_12fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_1129: Unknown result type (might be due to invalid IL or missing references)
		//IL_112e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1149: Unknown result type (might be due to invalid IL or missing references)
		//IL_114e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1de3: Unknown result type (might be due to invalid IL or missing references)
		//IL_1dee: Unknown result type (might be due to invalid IL or missing references)
		//IL_1cfa: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d05: Unknown result type (might be due to invalid IL or missing references)
		//IL_2406: Unknown result type (might be due to invalid IL or missing references)
		//IL_240b: Unknown result type (might be due to invalid IL or missing references)
		//IL_2410: Unknown result type (might be due to invalid IL or missing references)
		//IL_241c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e45: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e4a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e51: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e5c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e61: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e6e: Unknown result type (might be due to invalid IL or missing references)
		//IL_2a6f: Unknown result type (might be due to invalid IL or missing references)
		//IL_2a79: Unknown result type (might be due to invalid IL or missing references)
		//IL_2a7e: Unknown result type (might be due to invalid IL or missing references)
		//IL_257e: Unknown result type (might be due to invalid IL or missing references)
		//IL_2589: Unknown result type (might be due to invalid IL or missing references)
		//IL_2593: Unknown result type (might be due to invalid IL or missing references)
		//IL_259a: Expected O, but got Unknown
		//IL_21b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_21be: Unknown result type (might be due to invalid IL or missing references)
		//IL_21c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_25c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_25f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_2668: Unknown result type (might be due to invalid IL or missing references)
		//IL_2688: Unknown result type (might be due to invalid IL or missing references)
		//IL_277d: Unknown result type (might be due to invalid IL or missing references)
		//IL_26c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_284c: Unknown result type (might be due to invalid IL or missing references)
		//IL_2857: Unknown result type (might be due to invalid IL or missing references)
		//IL_2862: Unknown result type (might be due to invalid IL or missing references)
		//IL_2869: Expected O, but got Unknown
		//IL_2989: Unknown result type (might be due to invalid IL or missing references)
		//IL_2956: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		RaycastHit val = default(RaycastHit);
		LayerMask val2 = default(LayerMask);
		bool flag = false;
		int num2 = 0;
		Transform val3 = null;
		float num3 = 0f;
		Ray val4 = default(Ray);
		movimento = Vector3.zero;
		if (isalive)
		{
			if ((Object)(object)alvo != (Object)null)
			{
				changealvo_cont += 1f * Time.deltaTime;
				if (!(changealvo_cont <= 1f))
				{
					brains = GameObject.FindGameObjectsWithTag("Player");
					if (targetnum < brains.Length)
					{
						if ((Object)(object)brains[targetnum] != (Object)null)
						{
							alvorelativepos = ((Component)this).transform.InverseTransformPoint(brains[targetnum].transform.position);
							num3 = ((alvorelativepos.z <= 0f) ? ((float)backsight * ((sc_player)(object)brains[targetnum].GetComponent(typeof(sc_player))).exposiback) : ((float)frontsight * ((sc_player)(object)brains[targetnum].GetComponent(typeof(sc_player))).exposi));
							num3 += 0.005f;
							val2 = (768);
							if (Physics.Raycast(eyepos.position, brains[targetnum].transform.position - ((Component)this).transform.position, out val, num3, (val2)))
							{
								if ((Object)(object)((Component)val.collider).gameObject == (Object)(object)brains[targetnum] && !(Vector3.Distance(alvo.position, ((Component)this).transform.position) <= Vector3.Distance(brains[targetnum].transform.position, ((Component)this).transform.position) * 1.5f))
								{
									alvo = brains[targetnum].transform;
									lastalvoarea = ((sc_findmyarea)(object)((Component)alvo).GetComponent(typeof(sc_findmyarea))).myarea;
								}
								if ((Object)(object)((Component)val.collider).gameObject.transform.parent != (Object)null && (Object)(object)((Component)((Component)val.collider).gameObject.transform.parent).gameObject == (Object)(object)brains[targetnum])
								{
									alvo = brains[targetnum].transform;
									lastalvoarea = ((sc_findmyarea)(object)((Component)alvo).GetComponent(typeof(sc_findmyarea))).myarea;
								}
							}
						}
						targetnum++;
					}
					else
					{
						brains = GameObject.FindGameObjectsWithTag("Player");
						targetnum = 0;
					}
					changealvo_cont = 0f;
				}
			}
			movimento = Vector3.zero;
			val2 = (256);
			isgrounded = Physics.Raycast(((Component)this).transform.position, Vector3.down, out val, 1.4f, (val2));
			val2 = (33554432);
			if (Physics.Raycast(((Component)this).transform.position, Vector3.down, out val, 1.4f, (val2)))
			{
				aaa_pf_component.atual = ((Component)val.collider).gameObject;
				timetogiveup = 0f;
			}
			else
			{
				timetogiveup += 1f * Time.deltaTime;
				if (!(timetogiveup <= 2f))
				{
					aaa_pf_component.atual = null;
					timetogiveup = 0f;
				}
			}
			if ((Object)(object)aaa_pf_component.atual != (Object)(object)mylastarea)
			{
				if ((Object)(object)alvo != (Object)null)
				{
					if ((Object)(object)aaa_pf_component.atual != (Object)null && (Object)(object)((sc_findmyarea)(object)((Component)alvo).GetComponent(typeof(sc_findmyarea))).myarea != (Object)null)
					{
						if ((Object)(object)((sc_findmyarea)(object)((Component)alvo).GetComponent(typeof(sc_findmyarea))).myarea != (Object)(object)lastalvoarea)
						{
							if ((Object)(object)aaa_pf_component.atual != (Object)(object)((sc_findmyarea)(object)((Component)alvo).GetComponent(typeof(sc_findmyarea))).myarea)
							{
								aaa_pf_component.destino = ((sc_findmyarea)(object)((Component)alvo).GetComponent(typeof(sc_findmyarea))).myarea;
								aaa_pf_component.menordist = float.PositiveInfinity;
								aaa_pf_component.achou = false;
								aaa_pf_component.search(aaa_pf_component.atual, 0, aaa_pf_component.menorcaminho.Length);
								if (aaa_pf_component.achou)
								{
									havepath = true;
									path_stage = 0;
									path_state = 0;
								}
								else
								{
									alvo = null;
									havepath = false;
								}
							}
							else
							{
								path_state = 2;
							}
							if ((Object)(object)alvo != (Object)null)
							{
								lastalvoarea = ((sc_findmyarea)(object)((Component)alvo).GetComponent(typeof(sc_findmyarea))).myarea;
							}
						}
					}
					else
					{
						alvo = null;
					}
				}
				bool flag2 = (Object)(object)mylastarea != (Object)(object)aaa_pf_component.atual;
			}
			if (state != -1 && (Object)(object)alvo == (Object)null)
			{
				if (targetnum < brains.Length)
				{
					if ((Object)(object)brains[targetnum] != (Object)null)
					{
						alvorelativepos = ((Component)this).transform.InverseTransformPoint(brains[targetnum].transform.position);
						num3 = ((alvorelativepos.z <= 0f) ? ((float)backsight * ((sc_player)(object)brains[targetnum].GetComponent(typeof(sc_player))).exposiback) : ((float)frontsight * ((sc_player)(object)brains[targetnum].GetComponent(typeof(sc_player))).exposi));
						num3 += 0.005f;
						val2 = (768);
						if (Physics.Raycast(eyepos.position, brains[targetnum].transform.position - ((Component)this).transform.position, out val, num3, (val2)))
						{
							if ((Object)(object)((Component)val.collider).gameObject == (Object)(object)brains[targetnum])
							{
								alvo = brains[targetnum].transform;
								lastalvoarea = ((sc_findmyarea)(object)((Component)alvo).GetComponent(typeof(sc_findmyarea))).myarea;
							}
							if ((Object)(object)((Component)val.collider).gameObject.transform.parent != (Object)null && (Object)(object)((Component)((Component)val.collider).gameObject.transform.parent).gameObject == (Object)(object)brains[targetnum])
							{
								alvo = brains[targetnum].transform;
								lastalvoarea = ((sc_findmyarea)(object)((Component)alvo).GetComponent(typeof(sc_findmyarea))).myarea;
							}
						}
					}
					targetnum++;
				}
				else
				{
					brains = GameObject.FindGameObjectsWithTag("Player");
					targetnum = 0;
				}
			}
			if (!havepath && (Object)(object)alvo != (Object)null)
			{
				if ((Object)(object)aaa_pf_component.atual != (Object)null && (Object)(object)((sc_findmyarea)(object)((Component)alvo).GetComponent(typeof(sc_findmyarea))).myarea != (Object)null)
				{
					if ((Object)(object)aaa_pf_component.atual != (Object)(object)((sc_findmyarea)(object)((Component)alvo).GetComponent(typeof(sc_findmyarea))).myarea)
					{
						aaa_pf_component.destino = ((sc_findmyarea)(object)((Component)alvo).GetComponent(typeof(sc_findmyarea))).myarea;
						aaa_pf_component.menordist = float.PositiveInfinity;
						aaa_pf_component.achou = false;
						aaa_pf_component.search(aaa_pf_component.atual, 0, aaa_pf_component.menorcaminho.Length);
						if (aaa_pf_component.achou)
						{
							havepath = true;
							path_stage = 0;
							path_state = 0;
						}
					}
					else
					{
						havepath = true;
						path_state = 2;
					}
					lastsawpos = alvo.position;
				}
				else
				{
					alvo = null;
				}
			}
			if ((Object)(object)alvo != (Object)null)
			{
				if ((Object)(object)aaa_pf_component.atual != (Object)null && (Object)(object)((sc_findmyarea)(object)((Component)alvo).GetComponent(typeof(sc_findmyarea))).myarea != (Object)null)
				{
					if ((Object)(object)((sc_findmyarea)(object)((Component)alvo).GetComponent(typeof(sc_findmyarea))).myarea != (Object)(object)lastalvoarea)
					{
						if ((Object)(object)aaa_pf_component.atual != (Object)(object)((sc_findmyarea)(object)((Component)alvo).GetComponent(typeof(sc_findmyarea))).myarea)
						{
							aaa_pf_component.destino = ((sc_findmyarea)(object)((Component)alvo).GetComponent(typeof(sc_findmyarea))).myarea;
							aaa_pf_component.menordist = float.PositiveInfinity;
							aaa_pf_component.achou = false;
							aaa_pf_component.search(aaa_pf_component.atual, 0, aaa_pf_component.menorcaminho.Length);
							if (aaa_pf_component.achou)
							{
								havepath = true;
								path_stage = 0;
								path_state = 0;
							}
						}
						else
						{
							path_state = 2;
						}
						lastalvoarea = ((sc_findmyarea)(object)((Component)alvo).GetComponent(typeof(sc_findmyarea))).myarea;
					}
				}
				else
				{
					alvo = null;
				}
			}
			if ((Object)(object)alvo != (Object)null)
			{
				if ((Object)(object)((sc_findmyarea)(object)((Component)alvo).GetComponent(typeof(sc_findmyarea))).realarea == (Object)null)
				{
					if ((Object)(object)aaa_pf_component.atual == (Object)(object)((sc_findmyarea)(object)((Component)alvo).GetComponent(typeof(sc_findmyarea))).myarea)
					{
						timetogiveup2 += 1f * Time.deltaTime;
						if (!(timetogiveup2 <= 4f))
						{
							alvo = null;
							havepath = false;
						}
					}
				}
				else
				{
					timetogiveup2 = 0f;
				}
			}
			if (havepath)
			{
				if (path_state == 0)
				{
					if (!((float)path_stage >= aaa_pf_component.menordoornum - 1f))
					{
						if (((AI_porta)(object)((Component)aaa_pf_component.menorcaminho[path_stage]).GetComponent(typeof(AI_porta))).direction.x != 0f)
						{
							postogo.x = aaa_pf_component.menorcaminho[path_stage].position.x;
							postogo.z = aaa_pf_component.menorcaminho[path_stage + 1].position.z;
							if (!(postogo.z <= aaa_pf_component.menorcaminho[path_stage].position.z + aaa_pf_component.menorcaminho[path_stage].localScale.z / 2f - 0.5f))
							{
								postogo.z = aaa_pf_component.menorcaminho[path_stage].position.z + aaa_pf_component.menorcaminho[path_stage].localScale.z / 2f - 0.5f;
							}
							if (!(postogo.z >= aaa_pf_component.menorcaminho[path_stage].position.z - aaa_pf_component.menorcaminho[path_stage].localScale.z / 2f + 0.5f))
							{
								postogo.z = aaa_pf_component.menorcaminho[path_stage].position.z - aaa_pf_component.menorcaminho[path_stage].localScale.z / 2f + 0.5f;
							}
						}
						else
						{
							postogo.z = aaa_pf_component.menorcaminho[path_stage].position.z;
							postogo.x = aaa_pf_component.menorcaminho[path_stage + 1].position.x;
							if (!(postogo.x <= aaa_pf_component.menorcaminho[path_stage].position.x + aaa_pf_component.menorcaminho[path_stage].localScale.x / 2f - 0.5f))
							{
								postogo.x = aaa_pf_component.menorcaminho[path_stage].position.x + aaa_pf_component.menorcaminho[path_stage].localScale.x / 2f - 0.5f;
							}
							if (!(postogo.x >= aaa_pf_component.menorcaminho[path_stage].position.x - aaa_pf_component.menorcaminho[path_stage].localScale.x / 2f + 0.5f))
							{
								postogo.x = aaa_pf_component.menorcaminho[path_stage].position.x - aaa_pf_component.menorcaminho[path_stage].localScale.x / 2f + 0.5f;
							}
						}
					}
					else if ((Object)(object)alvo != (Object)null)
					{
						if (((AI_porta)(object)((Component)aaa_pf_component.menorcaminho[path_stage]).GetComponent(typeof(AI_porta))).direction.x != 0f)
						{
							postogo.x = aaa_pf_component.menorcaminho[path_stage].position.x;
							postogo.z = alvo.position.z;
							if (!(postogo.z <= aaa_pf_component.menorcaminho[path_stage].position.z + aaa_pf_component.menorcaminho[path_stage].localScale.z / 2f - 0.5f))
							{
								postogo.z = aaa_pf_component.menorcaminho[path_stage].position.z + aaa_pf_component.menorcaminho[path_stage].localScale.z / 2f - 0.5f;
							}
							if (!(postogo.z >= aaa_pf_component.menorcaminho[path_stage].position.z - aaa_pf_component.menorcaminho[path_stage].localScale.z / 2f + 0.5f))
							{
								postogo.z = aaa_pf_component.menorcaminho[path_stage].position.z - aaa_pf_component.menorcaminho[path_stage].localScale.z / 2f + 0.5f;
							}
						}
						else
						{
							postogo.z = aaa_pf_component.menorcaminho[path_stage].position.z;
							postogo.x = alvo.position.x;
							if (!(postogo.x <= aaa_pf_component.menorcaminho[path_stage].position.x + aaa_pf_component.menorcaminho[path_stage].localScale.x / 2f - 0.5f))
							{
								postogo.x = aaa_pf_component.menorcaminho[path_stage].position.x + aaa_pf_component.menorcaminho[path_stage].localScale.x / 2f - 0.5f;
							}
							if (!(postogo.x >= aaa_pf_component.menorcaminho[path_stage].position.x - aaa_pf_component.menorcaminho[path_stage].localScale.x / 2f + 0.5f))
							{
								postogo.x = aaa_pf_component.menorcaminho[path_stage].position.x - aaa_pf_component.menorcaminho[path_stage].localScale.x / 2f + 0.5f;
							}
						}
					}
					else
					{
						postogo = aaa_pf_component.menorcaminho[path_stage].position;
					}
					postogo.y = ((Component)this).transform.position.y;
				}
				if (path_state == 1)
				{
					postogo.x = ((Component)this).transform.position.x + ((AI_porta)(object)((Component)aaa_pf_component.menorcaminho[path_stage]).GetComponent(typeof(AI_porta))).direction.x;
					postogo.z = ((Component)this).transform.position.z + ((AI_porta)(object)((Component)aaa_pf_component.menorcaminho[path_stage]).GetComponent(typeof(AI_porta))).direction.y;
					postogo.y = ((Component)this).transform.position.y;
				}
				if (path_state == 2 && (Object)(object)alvo != (Object)null)
				{
					postogo = alvo.position;
					postogo.y = ((Component)this).transform.position.y;
				}
				Vector2 val5 = new Vector2(((Component)this).transform.position.x - postogo.x, ((Component)this).transform.position.z - postogo.z);
				if (!(val5.magnitude <= 0.1f))
				{
					((Component)this).transform.rotation = Quaternion.Lerp(((Component)this).transform.rotation, Quaternion.LookRotation(postogo - ((Component)this).transform.position, Vector3.up), 10f * Time.deltaTime);
				}
				if (path_state == 0)
				{
					val4.origin = ((Component)this).transform.position;
					val4.direction = Vector3.down;
					if (((Component)aaa_pf_component.menorcaminho[path_stage]).gameObject.GetComponent<Collider>().Raycast(val4, out val, 1.4f))
					{
						path_state = 1;
					}
				}
				if (path_state == 1 && (Object)(object)aaa_pf_component.atual == (Object)(object)((AI_porta)(object)((Component)aaa_pf_component.menorcaminho[path_stage]).GetComponent(typeof(AI_porta))).destino)
				{
					path_state = 0;
					path_stage++;
					if (!((float)path_stage >= aaa_pf_component.menordoornum))
					{
						path_state = 0;
					}
					else
					{
						path_state = 2;
					}
				}
			}
			if (state == -1)
			{
				animationcont += 100f * Time.deltaTime;
				if (!(animationcont <= 350f))
				{
					((Component)this).NetView().RPC("setzumbistate", (RPCMode)5, new object[0]);
					((Component)this).GetComponent<Rigidbody>().constraints = (RigidbodyConstraints)112;
					state = 0;
					vy = 0f;
				}
			}
			if (state != -1)
			{
				((Component)this).GetComponent<Rigidbody>().constraints = (RigidbodyConstraints)112;
			}
			if (state == 4)
			{
				movimento.z = -2f;
				animationcont += 100f * Time.deltaTime;
				if (!(animationcont <= 100f))
				{
					state = 1;
					vy = 0f;
					((Component)animador).GetComponent<Animation>().CrossFade("idle", 0.2f);
					((Component)this).NetView().RPC("fadeanim", (RPCMode)1, new object[3]
					{
						"idle",
						0.2f,
						((Component)animadorcomp).GetComponent<Animation>()["idle"].speed
					});
					cur_anim = "idle";
				}
			}
			if (state == 0)
			{
				if (Physics.Raycast(((Component)this).transform.position, ((Component)this).transform.forward, out val, 1f, (val2)))
				{
					movimento.x = walkspeed * 0.7f;
					if (((Object)(object)(sc_punchme)(object)((Component)val.collider).gameObject.GetComponent(typeof(sc_punchme)) != null))
					{
						RTH = true;
						state = 3;
						animationcont = 0f;
						vy = 0f;
						((Component)animador).GetComponent<Animation>().CrossFade("atackstrong", 0.2f);
						((Component)this).NetView().RPC("fadeanim", (RPCMode)1, new object[3]
						{
							"atackstrong",
							0.2f,
							((Component)animadorcomp).GetComponent<Animation>()["atackstrong"].speed
						});
						cur_anim = "atackstrong";
					}
				}
				vy -= grav * Time.deltaTime;
				if (isgrounded && !(vy >= 0f))
				{
					state = 1;
					vy = 0f;
					((Component)animador).GetComponent<Animation>().CrossFade("idle", 0.2f);
					((Component)this).NetView().RPC("fadeanim", (RPCMode)1, new object[3]
					{
						"idle",
						0.2f,
						((Component)animadorcomp).GetComponent<Animation>()["idle"].speed
					});
					cur_anim = "idle";
				}
			}
			if (state == 1)
			{
				if (!isacting)
				{
					actconter += 1000f * actrate * Time.deltaTime;
					if (!(actconter <= 1000f))
					{
						if (Random.Range(0, 100) < actchance)
						{
							((Component)animador).GetComponent<Animation>().CrossFade("look", 0.2f);
							((Component)this).NetView().RPC("fadeanim", (RPCMode)1, new object[3]
							{
								"look",
								0.2f,
								((Component)animadorcomp).GetComponent<Animation>()["look"].speed
							});
							cur_anim = "look";
							isacting = true;
							acttime = 250;
						}
						actconter = 0f;
					}
				}
				if (isacting)
				{
					actconter += 100f * Time.deltaTime;
					if (!(actconter <= (float)acttime))
					{
						((Component)animador).GetComponent<Animation>().CrossFade("idle", 0.5f);
						((Component)this).NetView().RPC("fadeanim", (RPCMode)1, new object[3]
						{
							"idle",
							0.5f,
							((Component)animadorcomp).GetComponent<Animation>()["idle"].speed
						});
						cur_anim = "idle";
						actconter = 0f;
						isacting = false;
					}
				}
				if ((Object)(object)alvo != (Object)null && havepath)
				{
					state = 2;
					((Component)animador).GetComponent<Animation>().CrossFade("walkfast", 0.3f);
					((Component)this).NetView().RPC("fadeanim", (RPCMode)1, new object[3]
					{
						"walkfast",
						0.3f,
						((Component)animadorcomp).GetComponent<Animation>()["walkfast"].speed
					});
					cur_anim = "walkfast";
				}
			}
			if (state == 2)
			{
				movimento.z = walkspeed;
				val2 = (2304);
				if (Physics.Raycast(((Component)this).transform.position, ((Component)this).transform.forward, out val, 1f, (val2)))
				{
					movimento.x = walkspeed * 0.7f;
					if (((Object)(object)(sc_punchme)(object)((Component)val.collider).gameObject.GetComponent(typeof(sc_punchme)) != null))
					{
						RTH = true;
						state = 3;
						animationcont = 0f;
						vy = 0f;
						((Component)animador).GetComponent<Animation>().CrossFade("atackstrong", 0.2f);
						((Component)this).NetView().RPC("fadeanim", (RPCMode)1, new object[3]
						{
							"atackstrong",
							0.2f,
							((Component)animadorcomp).GetComponent<Animation>()["atackstrong"].speed
						});
						cur_anim = "atackstrong";
					}
				}
				if ((Object)(object)alvo == (Object)null)
				{
					state = 1;
					vy = 0f;
					((Component)animador).GetComponent<Animation>().CrossFade("idle", 0.2f);
					((Component)this).NetView().RPC("fadeanim", (RPCMode)1, new object[3]
					{
						"idle",
						0.2f,
						((Component)animadorcomp).GetComponent<Animation>()["idle"].speed
					});
					cur_anim = "idle";
					alvo = null;
					havepath = false;
				}
				if ((Object)(object)alvo != (Object)null && !(Vector3.Distance(((Component)this).transform.position, alvo.position) >= 1.7f))
				{
					RTH = true;
					state = 3;
					animationcont = 0f;
					vy = 0f;
					((Component)animador).GetComponent<Animation>().CrossFade("atackstrong", 0.2f);
					((Component)this).NetView().RPC("fadeanim", (RPCMode)1, new object[3]
					{
						"atackstrong",
						0.2f,
						((Component)animadorcomp).GetComponent<Animation>()["atackstrong"].speed
					});
					cur_anim = "atackstrong";
				}
			}
			if (state == 3)
			{
				animationcont += 100f * Time.deltaTime;
				if ((Object)(object)alvo != (Object)null && !(Vector3.Distance(((Component)this).transform.position, alvo.position) <= 1.1f))
				{
					movimento.z = walkspeed * 0.5f;
				}
				if (RTH && !(animationcont <= 40f) && !(animationcont >= 75f))
				{
					val2 = (768);
					if (Physics.Raycast(armpos.position, ((Component)this).transform.TransformDirection(Vector3.forward), out val, atackreach, (val2)))
					{
						if ((Object)(object)(sc_dmgtaker)(object)((Component)val.collider).gameObject.GetComponent(typeof(sc_dmgtaker)) != (Object)null)
						{
							num3 = Random.Range(dmgmin, dmgmax);
							((sc_life)(object)((Component)((sc_dmgtaker)(object)((Component)val.collider).gameObject.GetComponent(typeof(sc_dmgtaker))).owner).GetComponent(typeof(sc_life))).quantia = ((sc_life)(object)((Component)((sc_dmgtaker)(object)((Component)val.collider).gameObject.GetComponent(typeof(sc_dmgtaker))).owner).GetComponent(typeof(sc_life))).quantia - num3;
							((Component)((sc_dmgtaker)(object)((Component)val.collider).gameObject.GetComponent(typeof(sc_dmgtaker))).owner).NetView().RPC("hurtme", (RPCMode)1, new object[1] { num3 });
							if ((Object)(object)(sc_punchme)(object)((Component)((sc_dmgtaker)(object)((Component)val.collider).gameObject.GetComponent(typeof(sc_dmgtaker))).owner).GetComponent(typeof(sc_punchme)) != (Object)null)
							{
								soundsources[0].clip = sounds[1];
								soundsources[0].pitch = Random.Range(0.9f, 1.1f);
								soundsources[0].Play();
								((Component)this).NetView().RPC("playzombiesound", (RPCMode)1, new object[2] { 0, 1 });
							}
							if ((Object)(object)(sc_player)(object)((Component)((sc_dmgtaker)(object)((Component)val.collider).gameObject.GetComponent(typeof(sc_dmgtaker))).owner).GetComponent(typeof(sc_player)) != (Object)null)
							{
								((Component)((sc_dmgtaker)(object)((Component)val.collider).gameObject.GetComponent(typeof(sc_dmgtaker))).owner).NetView().RPC("spillblood", (RPCMode)2, new object[0]);
								soundsources[0].clip = sounds[0];
								soundsources[0].pitch = Random.Range(0.9f, 1.1f);
								soundsources[0].Play();
								((Component)this).NetView().RPC("playzombiesound", (RPCMode)1, new object[2] { 0, 0 });
								num2 = Random.Range(0, 100);
								if (num2 < 5)
								{
									((Component)((sc_dmgtaker)(object)((Component)val.collider).gameObject.GetComponent(typeof(sc_dmgtaker))).owner).NetView().RPC("addbleed", (RPCMode)2, new object[0]);
								}
							}
							RTH = false;
						}
						if ((Object)(object)(sc_particula)(object)((Component)val.collider).gameObject.GetComponent(typeof(sc_particula)) != (Object)null)
						{
							Network.Instantiate((Object)(object)((sc_particula)(object)((Component)val.collider).gameObject.GetComponent(typeof(sc_particula))).prefab, val.point, Quaternion.LookRotation(val.normal), 3);
						}
					}
				}
				if (!(animationcont <= 100f))
				{
					state = 2;
					((Component)animador).GetComponent<Animation>().Play("walkfast");
					((Component)this).NetView().RPC("playanim", (RPCMode)1, new object[2]
					{
						"walkfast",
						((Component)animadorcomp).GetComponent<Animation>()["walkfast"].speed
					});
					cur_anim = "walkfast";
				}
				if ((Object)(object)alvo == (Object)null)
				{
					state = 1;
					vy = 0f;
					((Component)animador).GetComponent<Animation>().CrossFade("idle", 0.2f);
					((Component)this).NetView().RPC("fadeanim", (RPCMode)1, new object[3]
					{
						"idle",
						0.2f,
						((Component)animadorcomp).GetComponent<Animation>()["idle"].speed
					});
					cur_anim = "idle";
					havepath = false;
				}
			}
			if ((state == 1 || state == 2 || state == 3) && !isgrounded)
			{
				state = 0;
				((Component)animador).GetComponent<Animation>().CrossFade("fall", 0.3f);
				((Component)this).NetView().RPC("fadeanim", (RPCMode)1, new object[3]
				{
					"fall",
					0.3f,
					((Component)animadorcomp).GetComponent<Animation>()["fall"].speed
				});
				cur_anim = "fall";
				vy = 0f;
			}
			movimento.y = vy;
			if (movimento.x != 0f && movimento.z != 0f)
			{
				movimento.z *= 0.707f;
				movimento.x *= 0.707f;
			}
			movimento = ((Component)this).transform.TransformDirection(movimento);
			((Component)this).GetComponent<Rigidbody>().velocity = movimento;
			if (!(headlife > 0f))
			{
				((sc_life)(object)((Component)this).GetComponent(typeof(sc_life))).quantia = 0f;
			}
			if (!(((sc_life)(object)((Component)this).GetComponent(typeof(sc_life))).quantia > 0f))
			{
				((sc_life)(object)((Component)this).GetComponent(typeof(sc_life))).quantia = 0f;
				isalive = false;
				if (killerid >= 0)
				{
					((sc_matchcontroller)(object)((Component)matchcontrol).GetComponent(typeof(sc_matchcontroller))).everybodyspawners[killerid].NetView().RPC("addkill", (RPCMode)2, new object[0]);
					num2 = (int)(7f * Mathf.Pow(1.11f, (float)((sc_matchcontroller)(object)((Component)matchcontrol).GetComponent(typeof(sc_matchcontroller))).wave));
					((sc_matchcontroller)(object)((Component)matchcontrol).GetComponent(typeof(sc_matchcontroller))).everybodyspawners[killerid].NetView().RPC("addmoney", (RPCMode)2, new object[1] { num2 });
				}
				((Component)animador).GetComponent<Animation>().Stop();
				((Behaviour)((Component)animador).GetComponent<Animation>()).enabled = false;
				val3 = (Transform)Object.Instantiate((Object)(object)ragdoll, ((Component)this).transform.position, ((Component)this).transform.rotation);
				for (num = 0; num < parts.Length; num++)
				{
					((sc_ragdoll)(object)((Component)val3).GetComponent(typeof(sc_ragdoll))).parts[num].position = parts[num].position;
					((sc_ragdoll)(object)((Component)val3).GetComponent(typeof(sc_ragdoll))).parts[num].eulerAngles = parts[num].eulerAngles;
				}
				if (!(headlife > 0f))
				{
					{ var __em = ((sc_ragdoll)(object)((Component)val3).GetComponent(typeof(sc_ragdoll))).bloodfont.emission; __em.rateOverTime = 500f; }
					Object.Instantiate((Object)(object)headblowpart, ((sc_ragdoll)(object)((Component)val3).GetComponent(typeof(sc_ragdoll))).headpos.position, ((sc_ragdoll)(object)((Component)val3).GetComponent(typeof(sc_ragdoll))).headpos.rotation);
					for (num = 0; num < ((sc_ragdoll)(object)((Component)val3).GetComponent(typeof(sc_ragdoll))).headmesh.Length; num++)
					{
						((Renderer)((Component)((sc_ragdoll)(object)((Component)val3).GetComponent(typeof(sc_ragdoll))).headmesh[num]).GetComponent(typeof(Renderer))).enabled = false;
					}
				}
				if ((Object)(object)gamecontrol != (Object)null && ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).iamserver)
				{
					((sc_matchdata)(object)((Component)((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).matchdata).GetComponent(typeof(sc_matchdata))).addzombiedeath();
				}
				if (!(headlife <= 0f))
				{
					((Component)val3).GetComponent<Rigidbody>().AddForce(lastforce);
				}
				((sc_ragdoll)(object)((Component)val3).GetComponent(typeof(sc_ragdoll))).skin = skinnum;
				((sc_ragdoll)(object)((Component)val3).GetComponent(typeof(sc_ragdoll))).camisanum = camisanum;
				if (carryitem)
				{
					num2 = Random.Range(0, ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemtosell.Length);
					num2 = ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemtosell[num2];
					val3 = (Transform)Network.Instantiate((Object)(object)((sc_matchcontroller)(object)((Component)matchcontrol).GetComponent(typeof(sc_matchcontroller))).grounditens[num2], ((Component)this).transform.position, ((Component)this).transform.rotation, 5);
					((Behaviour)(sc_updateposition)(object)((Component)val3).GetComponent(typeof(sc_updateposition))).enabled = false;
					((Behaviour)(sc_sendpositionupdate)(object)((Component)val3).GetComponent(typeof(sc_sendpositionupdate))).enabled = true;
					if ((Object)(object)(sc_gunscript)(object)((Component)val3).GetComponent(typeof(sc_gunscript)) != (Object)null)
					{
						((sc_canbetaken)(object)((Component)val3).GetComponent(typeof(sc_canbetaken))).quant = ((sc_gunscript)(object)((Component)val3).GetComponent(typeof(sc_gunscript))).maxammo;
					}
					((sc_canbetaken)(object)((Component)val3).GetComponent(typeof(sc_canbetaken))).decay = true;
					((sc_canbetaken)(object)((Component)val3).GetComponent(typeof(sc_canbetaken))).decaytime = 30000f;
				}
				if (!(headlife > 0f))
				{
					((Component)this).NetView().RPC("killme", (RPCMode)5, new object[2]
					{
						Vector3.zero,
						true
					});
				}
				else
				{
					((Component)this).NetView().RPC("killme", (RPCMode)5, new object[2] { lastforce, false });
				}
				Object.Destroy((Object)(object)((Component)this).gameObject);
			}
			if (needtopush)
			{
				if (state != 0 && state != -1)
				{
					state = 4;
					animationcont = 0f;
					((Component)animador).GetComponent<Animation>().Rewind("stepback");
					((Component)animador).GetComponent<Animation>().CrossFade("stepback", 0.2f);
					((Component)this).NetView().RPC("fadeanim", (RPCMode)1, new object[3]
					{
						"stepback",
						0.2f,
						((Component)animadorcomp).GetComponent<Animation>()["stepback"].speed
					});
					cur_anim = "stepback";
				}
				needtopush = false;
			}
			lastforce *= 0.5f;
		}
		if (!isalive || !senddata)
		{
			return;
		}
		dataconter += 1000f * sendrate * Time.deltaTime;
		if (!(dataconter <= 1000f))
		{
			num2 = (int)((Component)this).transform.eulerAngles.y;
			if ((int)Network.peerType != 0 && (int)Network.peerType != 3)
			{
				((Component)this).NetView().RPC("setzumbistuff", (RPCMode)1, new object[2]
				{
					num2,
					((Component)this).transform.position
				});
				dataconter = 0f;
			}
		}
	}

	[RPC]
	public void playzombiesound(int source, int id)
	{
		soundsources[source].clip = sounds[id];
		soundsources[source].pitch = Random.Range(0.9f, 1.1f);
		soundsources[source].Play();
	}

	[RPC]
	public void setkillerid(int id)
	{
		killerid = id;
	}

	[RPC]
	public void forcar(Vector3 dir)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		lastforce += dir;
	}

	[RPC]
	public void setzumbistate()
	{
		state = 0;
	}

	[RPC]
	public void fadeanim(string anim_name, float time, float animspeed)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		((Component)(Animation)((Component)animador).GetComponent(typeof(Animation))).GetComponent<Animation>()[anim_name].speed = animspeed;
		((Component)(Animation)((Component)animador).GetComponent(typeof(Animation))).GetComponent<Animation>().CrossFade(anim_name, time);
	}

	[RPC]
	public void playanim(string anim_name, float animspeed)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		((Component)(Animation)((Component)animador).GetComponent(typeof(Animation))).GetComponent<Animation>().Rewind(anim_name);
		((Component)(Animation)((Component)animador).GetComponent(typeof(Animation))).GetComponent<Animation>()[anim_name].speed = animspeed;
		((Component)(Animation)((Component)animador).GetComponent(typeof(Animation))).GetComponent<Animation>().Play(anim_name);
	}

	[RPC]
	public void killme(Vector3 deathdir, bool blowhead)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		Transform val = null;
		val = (Transform)Object.Instantiate((Object)(object)ragdoll, ((Component)this).transform.position, ((Component)this).transform.rotation);
		if ((Object)(object)gamecontrol != (Object)null && ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).iamserver)
		{
			((sc_matchdata)(object)((Component)((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).matchdata).GetComponent(typeof(sc_matchdata))).addzombiedeath();
		}
		((sc_ragdoll)(object)((Component)val).GetComponent(typeof(sc_ragdoll))).skin = skinnum;
		((sc_ragdoll)(object)((Component)val).GetComponent(typeof(sc_ragdoll))).camisanum = camisanum;
		for (int i = 0; i < parts.Length; i++)
		{
			((sc_ragdoll)(object)((Component)val).GetComponent(typeof(sc_ragdoll))).parts[i].position = parts[i].position;
			((sc_ragdoll)(object)((Component)val).GetComponent(typeof(sc_ragdoll))).parts[i].eulerAngles = parts[i].eulerAngles;
		}
		if (blowhead)
		{
			Object.Instantiate((Object)(object)headblowpart, ((sc_ragdoll)(object)((Component)val).GetComponent(typeof(sc_ragdoll))).headpos.position, ((sc_ragdoll)(object)((Component)val).GetComponent(typeof(sc_ragdoll))).headpos.rotation);
			for (int i = 0; i < ((sc_ragdoll)(object)((Component)val).GetComponent(typeof(sc_ragdoll))).headmesh.Length; i++)
			{
				((Renderer)((Component)((sc_ragdoll)(object)((Component)val).GetComponent(typeof(sc_ragdoll))).headmesh[i]).GetComponent(typeof(Renderer))).enabled = false;
			}
			{ var __em = ((sc_ragdoll)(object)((Component)val).GetComponent(typeof(sc_ragdoll))).bloodfont.emission; __em.rateOverTime = 500f; }
		}
		((Component)val).GetComponent<Rigidbody>().AddForce(deathdir);
		Object.Destroy((Object)(object)((Component)this).gameObject);
	}

	public void Main()
	{
	}
}
