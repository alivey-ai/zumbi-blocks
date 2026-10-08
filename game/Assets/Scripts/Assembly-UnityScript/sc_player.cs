using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_player : MonoBehaviour
{
	public float camxmax;

	public float camymax;

	public Renderer[] handtohide;

	public AudioClip[] sounds;

	public AudioSource[] soundsources;

	public float sendrate;

	public float dataconter;

	public bool senddata;

	public int rpcrate;

	public int def_fov;

	public int headanglim;

	public bool controled;

	public int mintosrpint;

	public float sprintregen;

	public float sprintdecay;

	public float mouse_sense;

	public float grav;

	public float jumpspeed;

	public float jumpmovement;

	public float sprintspeed;

	public float walkspeed;

	public float walkbackmulti;

	public Texture crosshair;

	public Texture hitmarker;

	public Texture pointcrosshair;

	public Texture[] interactsign;

	public bool autoreload;

	public float userange;

	public float pushrange;

	public float pushspeed;

	public int lifes;

	public GUISkin guiskin;

	public GUISkin guiskin2;

	public GUISkin guiskinammoL;

	public GUISkin guiskinammoR;

	public GUISkin guiskin_WB;

	public GUISkin whitebarskin;

	public int[] item;

	public int[] quant;

	public int[] ammo;

	public int itemselected;

	public int itemtoselect;

	public Transform backpos;

	public Transform outpos;

	public Transform deadbody;

	public Transform handitem;

	public Transform gamecontrol;

	public Transform myspawner;

	public Transform eye;

	public Transform handpos;

	public Transform lhandpos;

	public Transform luminassao;

	public Transform flash;

	public Transform chest;

	public Transform legs;

	public Transform pivo;

	public Transform[] arms;

	public Transform head;

	public Transform add_capsule;

	public Transform headmesh;

	public ParticleSystem vasamento;

	public Transform std_particle;

	public Transform cam;

	public float headang;

	public int legstate;

	public int armstate;

	public float vy;

	public bool isgrounded;

	public Vector3 movimento;

	public string playername;

	public bool autoaim;

	public float animationcont;

	public bool reloadact1;

	public bool reloadact2;

	public bool reloadact3;

	public bool reloadact4;

	public bool reloadact5;

	public Transform lhandmag;

	public int myid;

	public int interacttype;

	public int armrottype;

	public bool canspin;

	public bool aiming;

	public int futurearmstate;

	public int waitingtime;

	public Transform revivetarget;

	public float rpcconter;

	public float rpcbar;

	public float flashconter;

	public string the_message;

	public float message_a;

	public Color message_col;

	public bool zooming;

	public float start_eyey;

	public float eyey;

	public Texture[] bloodspill;

	public Vector2[] bloodpos;

	public int[] bloodtype;

	public float[] bloodalpha;

	public int bloods;

	public float currentlife;

	public int itemtoload;

	public float stepconter;

	public float bleeding;

	public float bleedingconter;

	public float red_a;

	public Texture whitecube;

	public Texture[] crouch_stand_icons;

	public Texture[] detection_icons;

	public float sprintbar;

	public float hideconter;

	public bool showscope;

	public float throw_charge;

	public float throw_charge_min;

	public GameObject interact_obj;

	public bool canmoveinladder;

	public float startjump;

	public sc_timecontrol timecontroler;

	public GameObject solaris;

	public float fakebarulho;

	public float barulho;

	public float lastbarulho;

	public float soundwarningcont;

	public int walknoise;

	public int sprintnoise;

	public float noisemultiplier;

	public float exposiback;

	public float exposi;

	public float fake_exposi;

	public GameObject[] zbs;

	public Transform[] bendingbones;

	public float bending;

	public Transform cintopos;

	public Transform cintoitem;

	public float cinto_timer;

	public float cinto_timerend;

	public float crouch_cont;

	public bool crouching;

	public bool real_crouching;

	public float walkmulti;

	public float spreadcoef;

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

	public Transform placer;

	public float place_conter;

	public float hitM_conter;

	public float stuntef;

	public float stuntefcont;

	public sc_player()
	{
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		camxmax = 1f;
		camymax = 1f;
		sendrate = 10f;
		senddata = true;
		rpcrate = 10;
		def_fov = 70;
		headanglim = 70;
		mintosrpint = 20;
		sprintregen = 10f;
		sprintdecay = 10f;
		mouse_sense = 1f;
		grav = 10f;
		jumpspeed = 5f;
		jumpmovement = 0.6f;
		sprintspeed = 20f;
		walkspeed = 20f;
		walkbackmulti = 0.6f;
		autoreload = true;
		userange = 2f;
		pushrange = 2f;
		pushspeed = 1.5f;
		lifes = 3;
		isgrounded = true;
		movimento = Vector3.zero;
		interacttype = -1;
		canspin = true;
		the_message = string.Empty;
		message_col = Color.white;
		sprintbar = 100f;
		throw_charge_min = 0.2f;
		walknoise = 20;
		sprintnoise = 50;
		noisemultiplier = 5f;
		bending = 20f;
		walkmulti = 1f;
		spreadcoef = 1f;
	}

	[RPC]
	public void stun_me(float amount)
	{
		stuntef += amount;
	}

	[RPC]
	public void ask_aparence()
	{
		if (((Component)this).NetView().isMine)
		{
			((Component)this).NetView().RPC("sync_aparence", (RPCMode)1, new object[4] { beardnum, myskin, beardcolor, shirtcolor });
		}
	}

	[RPC]
	public void sync_aparence(int RPCbeardnum, int RPCmyskin, int RPCbeardcolor, int RPCshirtcolor)
	{
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Expected O, but got Unknown
		myskin = RPCmyskin;
		beardcolor = RPCbeardcolor;
		shirtcolor = RPCshirtcolor;
		for (int i = 0; i < skinpos.Length; i++)
		{
			((Component)skinpos[i]).GetComponent<Renderer>().material = skincolors[RPCmyskin];
		}
		for (int i = 0; i < camisapos.Length; i++)
		{
			((Component)camisapos[i]).GetComponent<Renderer>().material = camisacolors[RPCshirtcolor];
		}
		if ((Object)(object)beard != (Object)null)
		{
			Object.Destroy((Object)(object)((Component)beard).gameObject);
		}
		if (RPCbeardnum >= 0)
		{
			beard = (Transform)Object.Instantiate((Object)(object)beardprefabs[RPCbeardnum], beardpos.position, beardpos.rotation);
			beard.parent = beardpos;
			((sc_reference)(object)((Component)beard).GetComponent(typeof(sc_reference))).refer.GetComponent<Renderer>().material = haircolors[RPCbeardcolor];
		}
		ap_synced = true;
	}

	[RPC]
	public void call_for_fort(int fabid, int ownerid, Vector3 pos, Quaternion rot)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = GameObject.Find("matchcontroller(Clone)");
		((sc_matchcontroller)(object)val.GetComponent(typeof(sc_matchcontroller))).generate_bulding(fabid, ownerid, pos, rot);
	}

	[RPC]
	public void setbleeding(bool x)
	{
		if (x)
		{
			{ var __em = vasamento.emission; __em.rateOverTime = 100f; }
		}
		else
		{
			{ var __em = vasamento.emission; __em.rateOverTime = 0f; }
		}
	}

	[RPC]
	public void receiveammocrate()
	{
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		if (item[itemtoload] >= 0 && ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[itemtoload]].tipo == 0)
		{
			float num = Random.Range(0.5f, 1.5f);
			float num2 = (float)((sc_gunscript)(object)((Component)((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[itemtoload]].prefab).GetComponent(typeof(sc_gunscript))).receiverammo * num;
			if (!(num2 >= 1f))
			{
				num2 = 1f;
			}
			ammo[itemtoload] = (int)((float)ammo[itemtoload] + num2);
			the_message = ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).language[((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).languageset].frase[67];
			message_a = 1f;
			message_col = Color.yellow;
		}
	}

	[RPC]
	public void playasound(int num, int source)
	{
		((Component)this).GetComponent<AudioSource>().clip = sounds[num];
		soundsources[source].Play();
	}

	[RPC]
	public void addbleed()
	{
		if (!(bleeding > 0f))
		{
			((Component)this).NetView().RPC("setbleeding", (RPCMode)6, new object[1] { true });
		}
		bleeding++;
		bleedingconter = 30f;
	}

	public void Start()
	{
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0260: Unknown result type (might be due to invalid IL or missing references)
		//IL_0293: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_032c: Unknown result type (might be due to invalid IL or missing references)
		//IL_035f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0392: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_042c: Unknown result type (might be due to invalid IL or missing references)
		//IL_045f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0492: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_052b: Unknown result type (might be due to invalid IL or missing references)
		//IL_055e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0591: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_05fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0638: Unknown result type (might be due to invalid IL or missing references)
		//IL_0672: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0720: Unknown result type (might be due to invalid IL or missing references)
		//IL_075a: Unknown result type (might be due to invalid IL or missing references)
		//IL_078d: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0801: Unknown result type (might be due to invalid IL or missing references)
		//IL_0869: Unknown result type (might be due to invalid IL or missing references)
		//IL_08a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_08a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_08a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_08be: Unknown result type (might be due to invalid IL or missing references)
		//IL_08bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_08c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_08c7: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = null;
		if (!((Component)this).NetView().isMine)
		{
			((Behaviour)this).enabled = false;
		}
		if (((Component)this).NetView().isMine)
		{
			sync_aparence(-1, myskin, beardcolor, shirtcolor);
			val = GameObject.Find("timecontrol");
			if ((Object)(object)val != (Object)null)
			{
				timecontroler = (sc_timecontrol)(object)val.GetComponent(typeof(sc_timecontrol));
			}
			cam.parent = eye;
			cam.localPosition = Vector3.zero;
			cam.localEulerAngles = Vector3.zero;
			((Behaviour)((Component)luminassao).GetComponent<Light>()).enabled = true;
			start_eyey = cam.parent.parent.localPosition.x;
			eyey = start_eyey;
			currentlife = ((sc_life)(object)((Component)this).GetComponent(typeof(sc_life))).quantia;
			Object.Destroy((Object)(object)((Component)headmesh).gameObject);
			((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["mp5grabmag"].speed = 0.7f;
			((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["mp5grabmag"].speed = 0.5f;
			((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["m202grabmag"].speed = 0.5f;
			((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["shotpistol"].speed = 2f;
			((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["sprint"].speed = 0.3f * sprintspeed;
			((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["nogunidle"].speed = 0.25f;
			((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["interact"].speed = 0.25f;
			((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["holdingmeele1h"].speed = 0.25f;
			((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["cprhand"].speed = 1.6f;
			((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["handsback"].speed = 0.2f;
			((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["seringhold"].speed = 0.2f;
			((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["radiohold"].speed = 0.2f;
			((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["push"].speed = pushspeed;
			((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["move_ladder"].speed = 2.8f;
			((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["climb_from_ladder"].speed = 1.1f;
			((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["flashlighthold"].speed = 0.25f;
			((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["gunbash"].speed = 2.5f;
			((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["axehold"].speed = 0.2f;
			((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["bathold"].speed = 0.2f;
			((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["barricade_hold"].speed = 0.2f;
			((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["m202remag"].speed = 0.7f;
			((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["cprleg"].speed = 1.6f;
			((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["standing"].speed = 0.2f;
			((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["sprint"].speed = 0.3f * sprintspeed;
			((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["walk"].speed = 0.45f * walkspeed;
			((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["walkleft"].speed = 0.45f * walkspeed;
			((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["crouch_forward"].speed = 0.3f * walkspeed;
			((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["crouch_back"].speed = 0.17f * walkspeed;
			((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["crouch_left"].speed = 0.17f * walkspeed;
			((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["crouch_right"].speed = 0.17f * walkspeed;
			((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["move_ladder"].speed = 2.8f;
			((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["climb_from_ladder"].speed = 1.1f;
			((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["walkback"].speed = 0.7f * walkspeed * walkbackmulti;
			((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["jump"].speed = 2f;
			generatehanditem();
			((Component)legs).GetComponent<Animation>().Play("fall");
			((Component)this).NetView().RPC("playleganim", (RPCMode)1, new object[2]
			{
				"standing",
				((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["standing"].speed
			});
			int num = Random.Range(0, 360);
			Vector3 eulerAngles = ((Component)this).transform.eulerAngles;
			float num2 = (eulerAngles.y = num);
			Vector3 val2 = (((Component)this).transform.eulerAngles = eulerAngles);
			((Component)this).NetView().RPC("setmyname", (RPCMode)5, new object[1] { playername });
		}
	}

	[RPC]
	public void asktheplayeranim()
	{
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		if (((Component)this).NetView().isMine)
		{
			((Component)this).NetView().RPC("changeitem", (RPCMode)1, new object[1] { item[itemselected] });
			((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
			{
				"nogunidle",
				0.2f,
				((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["nogunidle"].speed
			});
		}
	}

	public void animateinoptions()
	{
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_037b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0449: Unknown result type (might be due to invalid IL or missing references)
		//IL_0531: Unknown result type (might be due to invalid IL or missing references)
		if (((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[itemselected]].tipo == 0)
		{
			((Component)chest).GetComponent<Animation>().CrossFade(((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).idleanim, 0.2f);
			((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
			{
				((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).idleanim,
				0.2f,
				((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()[((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).idleanim].speed
			});
		}
		if (((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[itemselected]].tipo == 1)
		{
			((Component)chest).GetComponent<Animation>().CrossFade("nogunidle", 0.2f);
			((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
			{
				"nogunidle",
				0.2f,
				((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["nogunidle"].speed
			});
		}
		if (((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[itemselected]].tipo == 2)
		{
			((Component)chest).GetComponent<Animation>().CrossFade(((sc_meelegun)(object)((Component)handitem).GetComponent(typeof(sc_meelegun))).holdanim, 0.2f);
			((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
			{
				((sc_meelegun)(object)((Component)handitem).GetComponent(typeof(sc_meelegun))).holdanim,
				0.2f,
				((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()[((sc_meelegun)(object)((Component)handitem).GetComponent(typeof(sc_meelegun))).holdanim].speed
			});
		}
		if (((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[itemselected]].tipo == 3)
		{
			((Component)chest).GetComponent<Animation>().CrossFade(((sc_consumable)(object)((Component)handitem).GetComponent(typeof(sc_consumable))).holdanim, 0.2f);
			((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
			{
				((sc_consumable)(object)((Component)handitem).GetComponent(typeof(sc_consumable))).holdanim,
				0.2f,
				((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()[((sc_consumable)(object)((Component)handitem).GetComponent(typeof(sc_consumable))).holdanim].speed
			});
		}
		if (((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[itemselected]].tipo == 4)
		{
			((Component)chest).GetComponent<Animation>().CrossFade("seringhold", 0.2f);
			((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
			{
				"seringhold",
				0.2f,
				((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["seringhold"].speed
			});
		}
		if (((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[itemselected]].tipo == 5)
		{
			((Component)chest).GetComponent<Animation>().CrossFade(((sc_fortification)(object)((Component)handitem).GetComponent(typeof(sc_fortification))).hold_anim, 0.2f);
			((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
			{
				((sc_fortification)(object)((Component)handitem).GetComponent(typeof(sc_fortification))).hold_anim,
				0.2f,
				((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()[((sc_fortification)(object)((Component)handitem).GetComponent(typeof(sc_fortification))).hold_anim].speed
			});
		}
	}

	public void generatehanditem()
	{
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Expected O, but got Unknown
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)handitem != (Object)null)
		{
			Object.Destroy((Object)(object)((Component)handitem).gameObject);
		}
		if (item[itemselected] >= 0)
		{
			handitem = (Transform)Object.Instantiate((Object)(object)((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[itemselected]].prefab);
			handitem.parent = handpos;
			handitem.localPosition = Vector3.zero;
			handitem.localEulerAngles = Vector3.zero;
			if (((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[itemselected]].tipo == 0 && ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).hidemagonoutammo && quant[itemtoselect] <= 0 && (Object)(object)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).magmesh != (Object)null)
			{
				((Component)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).magmesh).GetComponent<Renderer>().enabled = false;
			}
			animateinoptions();
		}
		else
		{
			((Component)chest).GetComponent<Animation>().Play("nogunidle");
			((Component)this).NetView().RPC("playarmanim", (RPCMode)1, new object[2]
			{
				"nogunidle",
				((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["nogunidle"].speed
			});
		}
		((Component)this).NetView().RPC("changeitem", (RPCMode)1, new object[1] { item[itemselected] });
	}

	public void OnCollisionStay(Collision collision)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		if ((legstate != 8 && legstate != 9) || ((Component)collision.collider).gameObject.layer != 9)
		{
			return;
		}
		Debug.Log((object)(string.Empty + Vector2.Distance(new Vector2(((Component)this).transform.position.x, ((Component)this).transform.position.z), new Vector2(interact_obj.transform.position.x, interact_obj.transform.position.z))));
		if (!(Vector2.Distance(new Vector2(((Component)this).transform.position.x, ((Component)this).transform.position.z), new Vector2(interact_obj.transform.position.x, interact_obj.transform.position.z)) <= 0.7f))
		{
			armrottype = 0;
			((Component)this).NetView().RPC("setrot", (RPCMode)1, new object[1] { armrottype });
			canspin = true;
			legstate = 0;
			((Component)legs).GetComponent<Animation>().CrossFade("jump", 0.3f);
			((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
			{
				"jump",
				0.3f,
				((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["jump"].speed
			});
			armstate = 11;
			animationcont = 0f;
			futurearmstate = 0;
			waitingtime = 250;
			if (item[itemselected] >= 0)
			{
				animateinoptions();
				return;
			}
			((Component)chest).GetComponent<Animation>().CrossFade("nogunidle", 0.2f);
			((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
			{
				"nogunidle",
				0.2f,
				((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["nogunidle"].speed
			});
		}
	}

	public void LateUpdate()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0293: Unknown result type (might be due to invalid IL or missing references)
		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_031d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0322: Unknown result type (might be due to invalid IL or missing references)
		//IL_0323: Unknown result type (might be due to invalid IL or missing references)
		//IL_0345: Unknown result type (might be due to invalid IL or missing references)
		//IL_0347: Unknown result type (might be due to invalid IL or missing references)
		//IL_0348: Unknown result type (might be due to invalid IL or missing references)
		//IL_034f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0490: Unknown result type (might be due to invalid IL or missing references)
		//IL_0495: Unknown result type (might be due to invalid IL or missing references)
		//IL_049c: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_0522: Unknown result type (might be due to invalid IL or missing references)
		//IL_0527: Unknown result type (might be due to invalid IL or missing references)
		//IL_058d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0895: Unknown result type (might be due to invalid IL or missing references)
		//IL_07fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_05da: Unknown result type (might be due to invalid IL or missing references)
		//IL_06eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_09bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_093c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ae5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a64: Unknown result type (might be due to invalid IL or missing references)
		//IL_15ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_152d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d42: Unknown result type (might be due to invalid IL or missing references)
		//IL_1cc1: Unknown result type (might be due to invalid IL or missing references)
		//IL_1053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fbc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c1e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b9d: Unknown result type (might be due to invalid IL or missing references)
		//IL_25dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_255b: Unknown result type (might be due to invalid IL or missing references)
		//IL_17d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_1751: Unknown result type (might be due to invalid IL or missing references)
		//IL_16c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e82: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d46: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cc5: Unknown result type (might be due to invalid IL or missing references)
		//IL_2e76: Unknown result type (might be due to invalid IL or missing references)
		//IL_2df5: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ef0: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e59: Unknown result type (might be due to invalid IL or missing references)
		//IL_278a: Unknown result type (might be due to invalid IL or missing references)
		//IL_26f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_1246: Unknown result type (might be due to invalid IL or missing references)
		//IL_11c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_1366: Unknown result type (might be due to invalid IL or missing references)
		//IL_12e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_2154: Unknown result type (might be due to invalid IL or missing references)
		//IL_20c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_203f: Unknown result type (might be due to invalid IL or missing references)
		//IL_19da: Unknown result type (might be due to invalid IL or missing references)
		//IL_1959: Unknown result type (might be due to invalid IL or missing references)
		//IL_29ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_295a: Unknown result type (might be due to invalid IL or missing references)
		//IL_28d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_1afa: Unknown result type (might be due to invalid IL or missing references)
		//IL_1a79: Unknown result type (might be due to invalid IL or missing references)
		//IL_1417: Unknown result type (might be due to invalid IL or missing references)
		//IL_149a: Unknown result type (might be due to invalid IL or missing references)
		//IL_327b: Unknown result type (might be due to invalid IL or missing references)
		//IL_2274: Unknown result type (might be due to invalid IL or missing references)
		//IL_21f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_3390: Unknown result type (might be due to invalid IL or missing references)
		//IL_33a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_33ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_33c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_33c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_33d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_33d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_33f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_33f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_3403: Unknown result type (might be due to invalid IL or missing references)
		//IL_340d: Unknown result type (might be due to invalid IL or missing references)
		//IL_3412: Unknown result type (might be due to invalid IL or missing references)
		//IL_3420: Unknown result type (might be due to invalid IL or missing references)
		//IL_3430: Unknown result type (might be due to invalid IL or missing references)
		//IL_3435: Unknown result type (might be due to invalid IL or missing references)
		//IL_3449: Unknown result type (might be due to invalid IL or missing references)
		//IL_344e: Unknown result type (might be due to invalid IL or missing references)
		//IL_3462: Unknown result type (might be due to invalid IL or missing references)
		//IL_3467: Unknown result type (might be due to invalid IL or missing references)
		//IL_346b: Unknown result type (might be due to invalid IL or missing references)
		//IL_3470: Unknown result type (might be due to invalid IL or missing references)
		//IL_331c: Unknown result type (might be due to invalid IL or missing references)
		//IL_2b0e: Unknown result type (might be due to invalid IL or missing references)
		//IL_2a8d: Unknown result type (might be due to invalid IL or missing references)
		//IL_2394: Unknown result type (might be due to invalid IL or missing references)
		//IL_2313: Unknown result type (might be due to invalid IL or missing references)
		//IL_1bab: Unknown result type (might be due to invalid IL or missing references)
		//IL_1c2e: Unknown result type (might be due to invalid IL or missing references)
		//IL_37c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_37d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_37da: Unknown result type (might be due to invalid IL or missing references)
		//IL_37e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_37fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_380a: Unknown result type (might be due to invalid IL or missing references)
		//IL_3818: Unknown result type (might be due to invalid IL or missing references)
		//IL_382d: Unknown result type (might be due to invalid IL or missing references)
		//IL_3832: Unknown result type (might be due to invalid IL or missing references)
		//IL_383c: Unknown result type (might be due to invalid IL or missing references)
		//IL_3841: Unknown result type (might be due to invalid IL or missing references)
		//IL_385b: Unknown result type (might be due to invalid IL or missing references)
		//IL_3860: Unknown result type (might be due to invalid IL or missing references)
		//IL_386d: Unknown result type (might be due to invalid IL or missing references)
		//IL_3877: Unknown result type (might be due to invalid IL or missing references)
		//IL_387c: Unknown result type (might be due to invalid IL or missing references)
		//IL_388a: Unknown result type (might be due to invalid IL or missing references)
		//IL_348e: Unknown result type (might be due to invalid IL or missing references)
		//IL_3493: Unknown result type (might be due to invalid IL or missing references)
		//IL_349a: Unknown result type (might be due to invalid IL or missing references)
		//IL_34a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_2c2e: Unknown result type (might be due to invalid IL or missing references)
		//IL_2bad: Unknown result type (might be due to invalid IL or missing references)
		//IL_3ef5: Unknown result type (might be due to invalid IL or missing references)
		//IL_3f05: Unknown result type (might be due to invalid IL or missing references)
		//IL_3f13: Unknown result type (might be due to invalid IL or missing references)
		//IL_3f23: Unknown result type (might be due to invalid IL or missing references)
		//IL_3f28: Unknown result type (might be due to invalid IL or missing references)
		//IL_3f3c: Unknown result type (might be due to invalid IL or missing references)
		//IL_3f41: Unknown result type (might be due to invalid IL or missing references)
		//IL_3f55: Unknown result type (might be due to invalid IL or missing references)
		//IL_3f5a: Unknown result type (might be due to invalid IL or missing references)
		//IL_3f5e: Unknown result type (might be due to invalid IL or missing references)
		//IL_3f63: Unknown result type (might be due to invalid IL or missing references)
		//IL_3908: Unknown result type (might be due to invalid IL or missing references)
		//IL_3984: Unknown result type (might be due to invalid IL or missing references)
		//IL_3137: Unknown result type (might be due to invalid IL or missing references)
		//IL_2445: Unknown result type (might be due to invalid IL or missing references)
		//IL_24c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_3fa2: Unknown result type (might be due to invalid IL or missing references)
		//IL_3fa7: Unknown result type (might be due to invalid IL or missing references)
		//IL_3fbb: Unknown result type (might be due to invalid IL or missing references)
		//IL_3fc0: Unknown result type (might be due to invalid IL or missing references)
		//IL_3fd4: Unknown result type (might be due to invalid IL or missing references)
		//IL_3fd9: Unknown result type (might be due to invalid IL or missing references)
		//IL_3fdd: Unknown result type (might be due to invalid IL or missing references)
		//IL_3fe2: Unknown result type (might be due to invalid IL or missing references)
		//IL_3f81: Unknown result type (might be due to invalid IL or missing references)
		//IL_3f88: Unknown result type (might be due to invalid IL or missing references)
		//IL_3f92: Unknown result type (might be due to invalid IL or missing references)
		//IL_31f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_2cdf: Unknown result type (might be due to invalid IL or missing references)
		//IL_2d62: Unknown result type (might be due to invalid IL or missing references)
		//IL_4000: Unknown result type (might be due to invalid IL or missing references)
		//IL_4007: Unknown result type (might be due to invalid IL or missing references)
		//IL_4011: Unknown result type (might be due to invalid IL or missing references)
		//IL_39c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_39cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_39df: Unknown result type (might be due to invalid IL or missing references)
		//IL_39e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_39f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_39fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_3a01: Unknown result type (might be due to invalid IL or missing references)
		//IL_3a06: Unknown result type (might be due to invalid IL or missing references)
		//IL_3566: Unknown result type (might be due to invalid IL or missing references)
		//IL_4472: Unknown result type (might be due to invalid IL or missing references)
		//IL_4322: Unknown result type (might be due to invalid IL or missing references)
		//IL_40d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_3a86: Unknown result type (might be due to invalid IL or missing references)
		//IL_3b02: Unknown result type (might be due to invalid IL or missing references)
		//IL_362e: Unknown result type (might be due to invalid IL or missing references)
		//IL_43ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_429f: Unknown result type (might be due to invalid IL or missing references)
		//IL_4190: Unknown result type (might be due to invalid IL or missing references)
		//IL_3c2c: Unknown result type (might be due to invalid IL or missing references)
		//IL_3b48: Unknown result type (might be due to invalid IL or missing references)
		//IL_3b4d: Unknown result type (might be due to invalid IL or missing references)
		//IL_3b61: Unknown result type (might be due to invalid IL or missing references)
		//IL_3b66: Unknown result type (might be due to invalid IL or missing references)
		//IL_3b7a: Unknown result type (might be due to invalid IL or missing references)
		//IL_3b7f: Unknown result type (might be due to invalid IL or missing references)
		//IL_3b83: Unknown result type (might be due to invalid IL or missing references)
		//IL_3b88: Unknown result type (might be due to invalid IL or missing references)
		//IL_36d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_374d: Unknown result type (might be due to invalid IL or missing references)
		//IL_3db8: Unknown result type (might be due to invalid IL or missing references)
		//IL_3ce8: Unknown result type (might be due to invalid IL or missing references)
		//IL_3e80: Unknown result type (might be due to invalid IL or missing references)
		//IL_4562: Unknown result type (might be due to invalid IL or missing references)
		//IL_4538: Unknown result type (might be due to invalid IL or missing references)
		//IL_45c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_45d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_4581: Unknown result type (might be due to invalid IL or missing references)
		//IL_458c: Unknown result type (might be due to invalid IL or missing references)
		//IL_4629: Unknown result type (might be due to invalid IL or missing references)
		//IL_462e: Unknown result type (might be due to invalid IL or missing references)
		//IL_4635: Unknown result type (might be due to invalid IL or missing references)
		//IL_4640: Unknown result type (might be due to invalid IL or missing references)
		//IL_464d: Unknown result type (might be due to invalid IL or missing references)
		//IL_4613: Unknown result type (might be due to invalid IL or missing references)
		//IL_474f: Unknown result type (might be due to invalid IL or missing references)
		//IL_4754: Unknown result type (might be due to invalid IL or missing references)
		//IL_4759: Unknown result type (might be due to invalid IL or missing references)
		//IL_4abb: Unknown result type (might be due to invalid IL or missing references)
		//IL_4ac6: Unknown result type (might be due to invalid IL or missing references)
		//IL_4ad1: Unknown result type (might be due to invalid IL or missing references)
		//IL_4ad7: Expected O, but got Unknown
		//IL_4a1f: Unknown result type (might be due to invalid IL or missing references)
		//IL_484d: Unknown result type (might be due to invalid IL or missing references)
		//IL_4852: Unknown result type (might be due to invalid IL or missing references)
		//IL_486c: Unknown result type (might be due to invalid IL or missing references)
		//IL_4871: Unknown result type (might be due to invalid IL or missing references)
		//IL_4881: Unknown result type (might be due to invalid IL or missing references)
		//IL_4886: Unknown result type (might be due to invalid IL or missing references)
		//IL_488a: Unknown result type (might be due to invalid IL or missing references)
		//IL_488f: Unknown result type (might be due to invalid IL or missing references)
		//IL_4e59: Unknown result type (might be due to invalid IL or missing references)
		//IL_4e5e: Unknown result type (might be due to invalid IL or missing references)
		//IL_4e65: Unknown result type (might be due to invalid IL or missing references)
		//IL_4e6a: Unknown result type (might be due to invalid IL or missing references)
		//IL_4e74: Unknown result type (might be due to invalid IL or missing references)
		//IL_4e79: Unknown result type (might be due to invalid IL or missing references)
		//IL_4e84: Unknown result type (might be due to invalid IL or missing references)
		//IL_4e89: Unknown result type (might be due to invalid IL or missing references)
		//IL_4e95: Unknown result type (might be due to invalid IL or missing references)
		//IL_4db4: Unknown result type (might be due to invalid IL or missing references)
		//IL_4cd6: Unknown result type (might be due to invalid IL or missing references)
		//IL_4784: Unknown result type (might be due to invalid IL or missing references)
		//IL_4789: Unknown result type (might be due to invalid IL or missing references)
		//IL_47a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_47a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_47b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_47bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_47c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_47c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_4edb: Unknown result type (might be due to invalid IL or missing references)
		//IL_4ee0: Unknown result type (might be due to invalid IL or missing references)
		//IL_4eea: Unknown result type (might be due to invalid IL or missing references)
		//IL_4eef: Unknown result type (might be due to invalid IL or missing references)
		//IL_4efa: Unknown result type (might be due to invalid IL or missing references)
		//IL_4f06: Unknown result type (might be due to invalid IL or missing references)
		//IL_4be8: Unknown result type (might be due to invalid IL or missing references)
		//IL_4bed: Unknown result type (might be due to invalid IL or missing references)
		//IL_4c11: Unknown result type (might be due to invalid IL or missing references)
		//IL_47e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_47e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_4803: Unknown result type (might be due to invalid IL or missing references)
		//IL_4808: Unknown result type (might be due to invalid IL or missing references)
		//IL_4818: Unknown result type (might be due to invalid IL or missing references)
		//IL_481d: Unknown result type (might be due to invalid IL or missing references)
		//IL_4821: Unknown result type (might be due to invalid IL or missing references)
		//IL_4826: Unknown result type (might be due to invalid IL or missing references)
		//IL_861d: Unknown result type (might be due to invalid IL or missing references)
		//IL_8622: Unknown result type (might be due to invalid IL or missing references)
		//IL_8629: Unknown result type (might be due to invalid IL or missing references)
		//IL_8634: Unknown result type (might be due to invalid IL or missing references)
		//IL_863f: Unknown result type (might be due to invalid IL or missing references)
		//IL_8644: Unknown result type (might be due to invalid IL or missing references)
		//IL_8651: Unknown result type (might be due to invalid IL or missing references)
		//IL_865c: Unknown result type (might be due to invalid IL or missing references)
		//IL_866c: Unknown result type (might be due to invalid IL or missing references)
		//IL_809a: Unknown result type (might be due to invalid IL or missing references)
		//IL_809f: Unknown result type (might be due to invalid IL or missing references)
		//IL_80a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_80b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_80be: Unknown result type (might be due to invalid IL or missing references)
		//IL_86f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_81bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_51b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_51c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_7df7: Unknown result type (might be due to invalid IL or missing references)
		//IL_5356: Unknown result type (might be due to invalid IL or missing references)
		//IL_52a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_92ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_8c57: Unknown result type (might be due to invalid IL or missing references)
		//IL_8c5c: Unknown result type (might be due to invalid IL or missing references)
		//IL_8c63: Unknown result type (might be due to invalid IL or missing references)
		//IL_8c6e: Unknown result type (might be due to invalid IL or missing references)
		//IL_8c94: Unknown result type (might be due to invalid IL or missing references)
		//IL_83c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_83cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_595e: Unknown result type (might be due to invalid IL or missing references)
		//IL_5969: Unknown result type (might be due to invalid IL or missing references)
		//IL_5973: Unknown result type (might be due to invalid IL or missing references)
		//IL_5978: Unknown result type (might be due to invalid IL or missing references)
		//IL_5983: Unknown result type (might be due to invalid IL or missing references)
		//IL_58c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_58d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_58da: Unknown result type (might be due to invalid IL or missing references)
		//IL_58df: Unknown result type (might be due to invalid IL or missing references)
		//IL_58f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_7eb9: Unknown result type (might be due to invalid IL or missing references)
		//IL_6add: Unknown result type (might be due to invalid IL or missing references)
		//IL_6aec: Unknown result type (might be due to invalid IL or missing references)
		//IL_6af1: Unknown result type (might be due to invalid IL or missing references)
		//IL_6af8: Unknown result type (might be due to invalid IL or missing references)
		//IL_6b03: Unknown result type (might be due to invalid IL or missing references)
		//IL_6b0e: Unknown result type (might be due to invalid IL or missing references)
		//IL_6b13: Unknown result type (might be due to invalid IL or missing references)
		//IL_6b20: Unknown result type (might be due to invalid IL or missing references)
		//IL_6b2b: Unknown result type (might be due to invalid IL or missing references)
		//IL_6b3b: Unknown result type (might be due to invalid IL or missing references)
		//IL_602e: Unknown result type (might be due to invalid IL or missing references)
		//IL_5b56: Unknown result type (might be due to invalid IL or missing references)
		//IL_5b61: Unknown result type (might be due to invalid IL or missing references)
		//IL_5b6b: Unknown result type (might be due to invalid IL or missing references)
		//IL_5b75: Expected O, but got Unknown
		//IL_5a65: Unknown result type (might be due to invalid IL or missing references)
		//IL_5713: Unknown result type (might be due to invalid IL or missing references)
		//IL_571e: Unknown result type (might be due to invalid IL or missing references)
		//IL_5728: Unknown result type (might be due to invalid IL or missing references)
		//IL_572d: Unknown result type (might be due to invalid IL or missing references)
		//IL_5738: Unknown result type (might be due to invalid IL or missing references)
		//IL_5686: Unknown result type (might be due to invalid IL or missing references)
		//IL_5691: Unknown result type (might be due to invalid IL or missing references)
		//IL_569b: Unknown result type (might be due to invalid IL or missing references)
		//IL_56a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_56b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_85e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_84f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_6baf: Unknown result type (might be due to invalid IL or missing references)
		//IL_610d: Unknown result type (might be due to invalid IL or missing references)
		//IL_5b92: Unknown result type (might be due to invalid IL or missing references)
		//IL_5ba1: Unknown result type (might be due to invalid IL or missing references)
		//IL_5ba6: Unknown result type (might be due to invalid IL or missing references)
		//IL_5bad: Unknown result type (might be due to invalid IL or missing references)
		//IL_5bb8: Unknown result type (might be due to invalid IL or missing references)
		//IL_5bc1: Unknown result type (might be due to invalid IL or missing references)
		//IL_88d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_8855: Unknown result type (might be due to invalid IL or missing references)
		//IL_6a7a: Unknown result type (might be due to invalid IL or missing references)
		//IL_6248: Unknown result type (might be due to invalid IL or missing references)
		//IL_6332: Unknown result type (might be due to invalid IL or missing references)
		//IL_61b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_5c1c: Unknown result type (might be due to invalid IL or missing references)
		//IL_5c27: Unknown result type (might be due to invalid IL or missing references)
		//IL_5c2e: Unknown result type (might be due to invalid IL or missing references)
		//IL_5c33: Unknown result type (might be due to invalid IL or missing references)
		//IL_5c38: Unknown result type (might be due to invalid IL or missing references)
		//IL_5c3a: Unknown result type (might be due to invalid IL or missing references)
		//IL_5c3c: Unknown result type (might be due to invalid IL or missing references)
		//IL_5c45: Unknown result type (might be due to invalid IL or missing references)
		//IL_5bd9: Unknown result type (might be due to invalid IL or missing references)
		//IL_5bde: Unknown result type (might be due to invalid IL or missing references)
		//IL_5c02: Unknown result type (might be due to invalid IL or missing references)
		//IL_5c07: Unknown result type (might be due to invalid IL or missing references)
		//IL_583d: Unknown result type (might be due to invalid IL or missing references)
		//IL_9649: Unknown result type (might be due to invalid IL or missing references)
		//IL_964e: Unknown result type (might be due to invalid IL or missing references)
		//IL_9655: Unknown result type (might be due to invalid IL or missing references)
		//IL_9660: Unknown result type (might be due to invalid IL or missing references)
		//IL_966d: Unknown result type (might be due to invalid IL or missing references)
		//IL_91cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_91d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_91d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_9147: Unknown result type (might be due to invalid IL or missing references)
		//IL_914e: Unknown result type (might be due to invalid IL or missing references)
		//IL_9153: Unknown result type (might be due to invalid IL or missing references)
		//IL_63f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_5ce1: Unknown result type (might be due to invalid IL or missing references)
		//IL_5ce3: Unknown result type (might be due to invalid IL or missing references)
		//IL_5d07: Unknown result type (might be due to invalid IL or missing references)
		//IL_5d0c: Unknown result type (might be due to invalid IL or missing references)
		//IL_5c5d: Unknown result type (might be due to invalid IL or missing references)
		//IL_5c62: Unknown result type (might be due to invalid IL or missing references)
		//IL_5c86: Unknown result type (might be due to invalid IL or missing references)
		//IL_5c8b: Unknown result type (might be due to invalid IL or missing references)
		//IL_a4b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_6612: Unknown result type (might be due to invalid IL or missing references)
		//IL_654f: Unknown result type (might be due to invalid IL or missing references)
		//IL_94f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_a8f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_96f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_96f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_96fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_95ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_691e: Unknown result type (might be due to invalid IL or missing references)
		//IL_993b: Unknown result type (might be due to invalid IL or missing references)
		//IL_9940: Unknown result type (might be due to invalid IL or missing references)
		//IL_995a: Unknown result type (might be due to invalid IL or missing references)
		//IL_995f: Unknown result type (might be due to invalid IL or missing references)
		//IL_996f: Unknown result type (might be due to invalid IL or missing references)
		//IL_9974: Unknown result type (might be due to invalid IL or missing references)
		//IL_9978: Unknown result type (might be due to invalid IL or missing references)
		//IL_997d: Unknown result type (might be due to invalid IL or missing references)
		//IL_8fbc: Unknown result type (might be due to invalid IL or missing references)
		//IL_8fc1: Unknown result type (might be due to invalid IL or missing references)
		//IL_8fc8: Unknown result type (might be due to invalid IL or missing references)
		//IL_9013: Unknown result type (might be due to invalid IL or missing references)
		//IL_79ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_6e39: Unknown result type (might be due to invalid IL or missing references)
		//IL_6f06: Unknown result type (might be due to invalid IL or missing references)
		//IL_6ce5: Unknown result type (might be due to invalid IL or missing references)
		//IL_6db2: Unknown result type (might be due to invalid IL or missing references)
		//IL_5e1c: Unknown result type (might be due to invalid IL or missing references)
		//IL_5e27: Unknown result type (might be due to invalid IL or missing references)
		//IL_5dc8: Unknown result type (might be due to invalid IL or missing references)
		//IL_5ddb: Unknown result type (might be due to invalid IL or missing references)
		//IL_55a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_b531: Unknown result type (might be due to invalid IL or missing references)
		//IL_b536: Unknown result type (might be due to invalid IL or missing references)
		//IL_ad24: Unknown result type (might be due to invalid IL or missing references)
		//IL_a7e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_a7ea: Expected O, but got Unknown
		//IL_a801: Unknown result type (might be due to invalid IL or missing references)
		//IL_a811: Unknown result type (might be due to invalid IL or missing references)
		//IL_a64c: Unknown result type (might be due to invalid IL or missing references)
		//IL_a657: Unknown result type (might be due to invalid IL or missing references)
		//IL_9a07: Unknown result type (might be due to invalid IL or missing references)
		//IL_9a8b: Unknown result type (might be due to invalid IL or missing references)
		//IL_9726: Unknown result type (might be due to invalid IL or missing references)
		//IL_972b: Unknown result type (might be due to invalid IL or missing references)
		//IL_9745: Unknown result type (might be due to invalid IL or missing references)
		//IL_974a: Unknown result type (might be due to invalid IL or missing references)
		//IL_975a: Unknown result type (might be due to invalid IL or missing references)
		//IL_975f: Unknown result type (might be due to invalid IL or missing references)
		//IL_9763: Unknown result type (might be due to invalid IL or missing references)
		//IL_9768: Unknown result type (might be due to invalid IL or missing references)
		//IL_7c10: Unknown result type (might be due to invalid IL or missing references)
		//IL_b5f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_b5f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_b54c: Unknown result type (might be due to invalid IL or missing references)
		//IL_b551: Unknown result type (might be due to invalid IL or missing references)
		//IL_b571: Unknown result type (might be due to invalid IL or missing references)
		//IL_b576: Unknown result type (might be due to invalid IL or missing references)
		//IL_b577: Unknown result type (might be due to invalid IL or missing references)
		//IL_b58f: Unknown result type (might be due to invalid IL or missing references)
		//IL_b591: Unknown result type (might be due to invalid IL or missing references)
		//IL_b592: Unknown result type (might be due to invalid IL or missing references)
		//IL_b599: Unknown result type (might be due to invalid IL or missing references)
		//IL_b5a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_b5a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_b286: Unknown result type (might be due to invalid IL or missing references)
		//IL_b28b: Unknown result type (might be due to invalid IL or missing references)
		//IL_b2b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_b2bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_b2bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_b2d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_b2d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_b2d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_b2de: Unknown result type (might be due to invalid IL or missing references)
		//IL_b21a: Unknown result type (might be due to invalid IL or missing references)
		//IL_b21f: Unknown result type (might be due to invalid IL or missing references)
		//IL_b250: Unknown result type (might be due to invalid IL or missing references)
		//IL_b255: Unknown result type (might be due to invalid IL or missing references)
		//IL_b256: Unknown result type (might be due to invalid IL or missing references)
		//IL_b26e: Unknown result type (might be due to invalid IL or missing references)
		//IL_b270: Unknown result type (might be due to invalid IL or missing references)
		//IL_b271: Unknown result type (might be due to invalid IL or missing references)
		//IL_b278: Unknown result type (might be due to invalid IL or missing references)
		//IL_a6ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_a6f9: Expected O, but got Unknown
		//IL_a710: Unknown result type (might be due to invalid IL or missing references)
		//IL_a720: Unknown result type (might be due to invalid IL or missing references)
		//IL_9786: Unknown result type (might be due to invalid IL or missing references)
		//IL_978b: Unknown result type (might be due to invalid IL or missing references)
		//IL_97a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_97aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_97ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_97bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_97c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_97c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_7a72: Unknown result type (might be due to invalid IL or missing references)
		//IL_6fac: Unknown result type (might be due to invalid IL or missing references)
		//IL_6fd0: Unknown result type (might be due to invalid IL or missing references)
		//IL_6fda: Unknown result type (might be due to invalid IL or missing references)
		//IL_6fe0: Expected O, but got Unknown
		//IL_7005: Unknown result type (might be due to invalid IL or missing references)
		//IL_7010: Unknown result type (might be due to invalid IL or missing references)
		//IL_701a: Unknown result type (might be due to invalid IL or missing references)
		//IL_7040: Unknown result type (might be due to invalid IL or missing references)
		//IL_5f2e: Unknown result type (might be due to invalid IL or missing references)
		//IL_b6b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_b6bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_b60f: Unknown result type (might be due to invalid IL or missing references)
		//IL_b614: Unknown result type (might be due to invalid IL or missing references)
		//IL_b634: Unknown result type (might be due to invalid IL or missing references)
		//IL_b639: Unknown result type (might be due to invalid IL or missing references)
		//IL_b63a: Unknown result type (might be due to invalid IL or missing references)
		//IL_b652: Unknown result type (might be due to invalid IL or missing references)
		//IL_b654: Unknown result type (might be due to invalid IL or missing references)
		//IL_b655: Unknown result type (might be due to invalid IL or missing references)
		//IL_b65c: Unknown result type (might be due to invalid IL or missing references)
		//IL_b665: Unknown result type (might be due to invalid IL or missing references)
		//IL_b66a: Unknown result type (might be due to invalid IL or missing references)
		//IL_b5c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_b5c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_b5c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_b5e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_b5e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_b5e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_b5eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_b2e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_b2ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_aba9: Unknown result type (might be due to invalid IL or missing references)
		//IL_aacf: Unknown result type (might be due to invalid IL or missing references)
		//IL_aada: Unknown result type (might be due to invalid IL or missing references)
		//IL_9fca: Unknown result type (might be due to invalid IL or missing references)
		//IL_a04e: Unknown result type (might be due to invalid IL or missing references)
		//IL_9855: Unknown result type (might be due to invalid IL or missing references)
		//IL_98d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_7cd4: Unknown result type (might be due to invalid IL or missing references)
		//IL_b77a: Unknown result type (might be due to invalid IL or missing references)
		//IL_b77f: Unknown result type (might be due to invalid IL or missing references)
		//IL_b6d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_b6d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_b6f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_b6fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_b6fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_b715: Unknown result type (might be due to invalid IL or missing references)
		//IL_b717: Unknown result type (might be due to invalid IL or missing references)
		//IL_b718: Unknown result type (might be due to invalid IL or missing references)
		//IL_b71f: Unknown result type (might be due to invalid IL or missing references)
		//IL_b728: Unknown result type (might be due to invalid IL or missing references)
		//IL_b72d: Unknown result type (might be due to invalid IL or missing references)
		//IL_b685: Unknown result type (might be due to invalid IL or missing references)
		//IL_b68a: Unknown result type (might be due to invalid IL or missing references)
		//IL_b68b: Unknown result type (might be due to invalid IL or missing references)
		//IL_b6a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_b6a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_b6a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_b6ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_b341: Unknown result type (might be due to invalid IL or missing references)
		//IL_b346: Unknown result type (might be due to invalid IL or missing references)
		//IL_b310: Unknown result type (might be due to invalid IL or missing references)
		//IL_b315: Unknown result type (might be due to invalid IL or missing references)
		//IL_b316: Unknown result type (might be due to invalid IL or missing references)
		//IL_b32e: Unknown result type (might be due to invalid IL or missing references)
		//IL_b330: Unknown result type (might be due to invalid IL or missing references)
		//IL_b331: Unknown result type (might be due to invalid IL or missing references)
		//IL_b338: Unknown result type (might be due to invalid IL or missing references)
		//IL_708f: Unknown result type (might be due to invalid IL or missing references)
		//IL_70b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_70be: Unknown result type (might be due to invalid IL or missing references)
		//IL_70c4: Expected O, but got Unknown
		//IL_70c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_70d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_b795: Unknown result type (might be due to invalid IL or missing references)
		//IL_b79a: Unknown result type (might be due to invalid IL or missing references)
		//IL_b7ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_b7bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_b7c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_b7d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_b7da: Unknown result type (might be due to invalid IL or missing references)
		//IL_b7db: Unknown result type (might be due to invalid IL or missing references)
		//IL_b7e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_b7eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_b7f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_b748: Unknown result type (might be due to invalid IL or missing references)
		//IL_b74d: Unknown result type (might be due to invalid IL or missing references)
		//IL_b74e: Unknown result type (might be due to invalid IL or missing references)
		//IL_b767: Unknown result type (might be due to invalid IL or missing references)
		//IL_b769: Unknown result type (might be due to invalid IL or missing references)
		//IL_b76a: Unknown result type (might be due to invalid IL or missing references)
		//IL_b771: Unknown result type (might be due to invalid IL or missing references)
		//IL_b36c: Unknown result type (might be due to invalid IL or missing references)
		//IL_b371: Unknown result type (might be due to invalid IL or missing references)
		//IL_b372: Unknown result type (might be due to invalid IL or missing references)
		//IL_b38a: Unknown result type (might be due to invalid IL or missing references)
		//IL_b38c: Unknown result type (might be due to invalid IL or missing references)
		//IL_b38d: Unknown result type (might be due to invalid IL or missing references)
		//IL_b394: Unknown result type (might be due to invalid IL or missing references)
		//IL_74e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_750d: Unknown result type (might be due to invalid IL or missing references)
		//IL_7518: Unknown result type (might be due to invalid IL or missing references)
		//IL_751e: Expected O, but got Unknown
		//IL_b8ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_b8b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_b8b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_b80b: Unknown result type (might be due to invalid IL or missing references)
		//IL_b810: Unknown result type (might be due to invalid IL or missing references)
		//IL_b811: Unknown result type (might be due to invalid IL or missing references)
		//IL_b82a: Unknown result type (might be due to invalid IL or missing references)
		//IL_b82c: Unknown result type (might be due to invalid IL or missing references)
		//IL_b82d: Unknown result type (might be due to invalid IL or missing references)
		//IL_b834: Unknown result type (might be due to invalid IL or missing references)
		//IL_b417: Unknown result type (might be due to invalid IL or missing references)
		//IL_b41c: Unknown result type (might be due to invalid IL or missing references)
		//IL_b44a: Unknown result type (might be due to invalid IL or missing references)
		//IL_b44f: Unknown result type (might be due to invalid IL or missing references)
		//IL_b450: Unknown result type (might be due to invalid IL or missing references)
		//IL_b468: Unknown result type (might be due to invalid IL or missing references)
		//IL_b46a: Unknown result type (might be due to invalid IL or missing references)
		//IL_b46b: Unknown result type (might be due to invalid IL or missing references)
		//IL_b472: Unknown result type (might be due to invalid IL or missing references)
		//IL_b3a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_b3ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_b3e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_b3e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_b3e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_b3ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_b401: Unknown result type (might be due to invalid IL or missing references)
		//IL_b402: Unknown result type (might be due to invalid IL or missing references)
		//IL_b409: Unknown result type (might be due to invalid IL or missing references)
		//IL_8ab6: Unknown result type (might be due to invalid IL or missing references)
		//IL_715e: Unknown result type (might be due to invalid IL or missing references)
		//IL_716d: Unknown result type (might be due to invalid IL or missing references)
		//IL_b92d: Unknown result type (might be due to invalid IL or missing references)
		//IL_b8d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_b8de: Unknown result type (might be due to invalid IL or missing references)
		//IL_b8ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_b47b: Unknown result type (might be due to invalid IL or missing references)
		//IL_b480: Unknown result type (might be due to invalid IL or missing references)
		//IL_8bb0: Unknown result type (might be due to invalid IL or missing references)
		//IL_7699: Unknown result type (might be due to invalid IL or missing references)
		//IL_76e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_b4d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_b4da: Unknown result type (might be due to invalid IL or missing references)
		//IL_b4a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_b4a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_b4aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_b4c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_b4c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_b4c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_b4cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_b979: Unknown result type (might be due to invalid IL or missing references)
		//IL_b97e: Unknown result type (might be due to invalid IL or missing references)
		//IL_b9c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_b500: Unknown result type (might be due to invalid IL or missing references)
		//IL_b505: Unknown result type (might be due to invalid IL or missing references)
		//IL_b506: Unknown result type (might be due to invalid IL or missing references)
		//IL_b51e: Unknown result type (might be due to invalid IL or missing references)
		//IL_b520: Unknown result type (might be due to invalid IL or missing references)
		//IL_b521: Unknown result type (might be due to invalid IL or missing references)
		//IL_b528: Unknown result type (might be due to invalid IL or missing references)
		//IL_77ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_c0ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_c0b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_baf6: Unknown result type (might be due to invalid IL or missing references)
		//IL_bb01: Unknown result type (might be due to invalid IL or missing references)
		//IL_bb0c: Unknown result type (might be due to invalid IL or missing references)
		//IL_bb12: Expected O, but got Unknown
		//IL_c1b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_c1bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_c01a: Unknown result type (might be due to invalid IL or missing references)
		//IL_c02a: Unknown result type (might be due to invalid IL or missing references)
		//IL_bdbd: Unknown result type (might be due to invalid IL or missing references)
		//IL_bdc2: Unknown result type (might be due to invalid IL or missing references)
		//IL_beb8: Unknown result type (might be due to invalid IL or missing references)
		//IL_bec0: Unknown result type (might be due to invalid IL or missing references)
		//IL_be51: Unknown result type (might be due to invalid IL or missing references)
		//IL_be61: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		Transform val = null;
		RaycastHit val2 = default(RaycastHit);
		LayerMask val3 = default(LayerMask);
		bool flag = false;
		bool flag2 = false;
		int num2 = 0;
		int num3 = 0;
		movimento = Vector3.zero;
		float num4 = 0f;
		Vector3 val4 = default(Vector3);
		stuntef = Mathf.Lerp(stuntef, 0f, 5f * Time.deltaTime);
		if (!(stuntef >= 0.05f))
		{
			stuntef = 0f;
		}
		stuntefcont += Time.deltaTime;
		if (!(stuntefcont <= 0.03f))
		{
			headang += Random.Range(0f - stuntef, stuntef);
			((Component)this).transform.Rotate(Vector3.up * Random.Range(0f - stuntef, stuntef));
			stuntefcont = 0f;
		}
		if (!(hitM_conter <= 0f))
		{
			hitM_conter -= Time.deltaTime * 5f;
			if (!(hitM_conter >= 0f))
			{
				hitM_conter = 0f;
			}
		}
		if ((Object)(object)solaris == (Object)null)
		{
			solaris = GameObject.Find("sol1");
		}
		if ((Object)(object)solaris != (Object)null)
		{
			solaris.transform.rotation = ((Component)this).transform.rotation;
			solaris.transform.Rotate(Vector3.right * 45f);
		}
		((Component)luminassao).GetComponent<Light>().intensity = 0.2f + timecontroler.timecoef * 0.1f;
		((Component)luminassao).GetComponent<Light>().range = 30f + timecontroler.timecoef * 250f;
		if (!(bleeding <= 0f))
		{
			red_a += 2f * Time.deltaTime;
			if (!(red_a <= 2f))
			{
				red_a -= 2f;
			}
			((sc_life)(object)((Component)this).GetComponent(typeof(sc_life))).quantia = ((sc_life)(object)((Component)this).GetComponent(typeof(sc_life))).quantia - bleeding * Time.deltaTime;
			bleedingconter -= Time.deltaTime;
			if (!(bleedingconter > 0f))
			{
				bleeding = 0f;
				((Component)this).NetView().RPC("setbleeding", (RPCMode)6, new object[1] { false });
			}
		}
		val3 = (256);
		isgrounded = Physics.Raycast(((Component)this).transform.position, Vector3.down, out val2, 1.35f, (val3));
		if ((Object)(object)cam.parent != (Object)null)
		{
			float x = Mathf.Lerp(cam.parent.parent.localPosition.x, eyey, 10f * Time.deltaTime);
			Vector3 localPosition = cam.parent.parent.localPosition;
			float num5 = (localPosition.x = x);
			Vector3 val5 = (cam.parent.parent.localPosition = localPosition);
		}
		else
		{
			senddata = false;
		}
		if (((Behaviour)((Component)flash).GetComponent<Light>()).enabled)
		{
			flashconter -= 5000f * Time.deltaTime;
			if (!(flashconter > 0f))
			{
				((Behaviour)((Component)flash).GetComponent<Light>()).enabled = false;
			}
		}
		if (legstate == 0)
		{
			barulho = 0f;
			if (controled)
			{
				if (Input.GetAxis("Sprint") <= 0f || !(sprintbar >= (float)mintosrpint))
				{
					movimento.z = Input.GetAxis("Vertical") * walkspeed * jumpmovement;
				}
				else
				{
					movimento.z = Input.GetAxis("Sprint") * sprintspeed * jumpmovement;
					sprintbar -= Input.GetAxis("Sprint") * sprintdecay * Time.deltaTime;
				}
				movimento.x = Input.GetAxis("Horizontal") * walkspeed * jumpmovement;
			}
			if (!(vy <= 0f))
			{
				val3 = (256);
				if (Physics.Raycast(((Component)this).transform.position, Vector3.up, out val2, 1.3f, (val3)))
				{
					vy = -0.01f;
				}
			}
			flag = !(vy < 0f);
			vy -= grav * Time.deltaTime;
			if (!(vy >= -16f))
			{
				vy = -16f;
			}
			if (flag && !(vy >= 0f))
			{
				startjump = ((Component)this).transform.position.y;
				((Component)legs).GetComponent<Animation>().CrossFade("fall", 0.2f);
				((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
				{
					"fall",
					0.2f,
					((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["fall"].speed
				});
			}
			if (isgrounded && !(vy >= 0f))
			{
				num4 = startjump - ((Component)this).transform.position.y;
				if (!(num4 <= 4.6f))
				{
					spillblood();
					((sc_life)(object)((Component)this).GetComponent(typeof(sc_life))).quantia = ((sc_life)(object)((Component)this).GetComponent(typeof(sc_life))).quantia - (num4 - 4.6f) * 10f;
				}
				legstate = 1;
				soundsources[1].clip = sounds[3];
				soundsources[1].Play();
				((Component)this).NetView().RPC("playasound", (RPCMode)1, new object[2] { 3, 1 });
				vy = 0f;
				((Component)legs).GetComponent<Animation>().CrossFade("standing", 0.2f);
				((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
				{
					"standing",
					0.2f,
					((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["standing"].speed
				});
			}
		}
		if (armstate == 1 && legstate != 6)
		{
			armstate = 0;
		}
		if (legstate == 1)
		{
			barulho = 0f;
			sprintbar += sprintregen * Time.deltaTime;
			if (!(sprintbar <= 100f))
			{
				sprintbar = 100f;
			}
			vy = 0f;
			if (controled)
			{
				if (!crouching)
				{
					if (Input.GetKey((KeyCode)306))
					{
						((Component)legs).GetComponent<Animation>().CrossFade("crouch_idle", 0.3f);
						crouching = true;
						((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
						{
							"crouch_idle",
							0.3f,
							((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["crouch_idle"].speed
						});
					}
				}
				else if (!Input.GetKey((KeyCode)306))
				{
					((Component)legs).GetComponent<Animation>().CrossFade("standing", 0.3f);
					crouching = false;
					((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
					{
						"standing",
						0.3f,
						((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["standing"].speed
					});
				}
				if (!(Input.GetAxis("Vertical") <= 0f))
				{
					legstate = 2;
					stepconter = 0f;
					if (!crouching)
					{
						((Component)legs).GetComponent<Animation>().CrossFade("walk", 0.3f);
						((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
						{
							"walk",
							0.3f,
							((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["walk"].speed
						});
					}
					else
					{
						((Component)legs).GetComponent<Animation>().CrossFade("crouch_forward", 0.3f);
						((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
						{
							"crouch_forward",
							0.3f,
							((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["crouch_forward"].speed
						});
					}
				}
				if (!(Input.GetAxis("Vertical") >= 0f))
				{
					legstate = 3;
					stepconter = 0f;
					if (!crouching)
					{
						((Component)legs).GetComponent<Animation>().CrossFade("walkback", 0.3f);
						((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
						{
							"walkback",
							0.3f,
							((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["walkback"].speed
						});
					}
					else
					{
						((Component)legs).GetComponent<Animation>().CrossFade("crouch_back", 0.3f);
						((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
						{
							"crouch_back",
							0.3f,
							((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["crouch_back"].speed
						});
					}
				}
				if (Input.GetAxis("Vertical") == 0f)
				{
					if (!(Input.GetAxis("Horizontal") >= 0f))
					{
						legstate = 4;
						stepconter = 0f;
						if (!crouching)
						{
							((Component)legs).GetComponent<Animation>().CrossFade("walkleft", 0.3f);
							((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
							{
								"walkleft",
								0.3f,
								((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["walkleft"].speed
							});
						}
						else
						{
							((Component)legs).GetComponent<Animation>().CrossFade("crouch_left", 0.3f);
							((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
							{
								"crouch_left",
								0.3f,
								((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["crouch_left"].speed
							});
						}
					}
					if (!(Input.GetAxis("Horizontal") <= 0f))
					{
						legstate = 5;
						stepconter = 0f;
						if (!crouching)
						{
							((Component)legs).GetComponent<Animation>().CrossFade("walkleft", 0.3f);
							((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
							{
								"walkleft",
								0.3f,
								((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["walkleft"].speed
							});
						}
						else
						{
							((Component)legs).GetComponent<Animation>().CrossFade("crouch_right", 0.3f);
							((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
							{
								"crouch_right",
								0.3f,
								((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["crouch_right"].speed
							});
						}
					}
				}
				if (!(Input.GetAxis("Sprint") <= 0f) && armstate == 0 && !(sprintbar < (float)mintosrpint))
				{
					legstate = 6;
					stepconter = 0f;
					((Component)legs).GetComponent<Animation>().CrossFade("sprint", 0.3f);
					((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
					{
						"sprint",
						0.3f,
						((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["sprint"].speed
					});
					armstate = 1;
					((Component)chest).GetComponent<Animation>().CrossFade("sprint", 0.3f);
					((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
					{
						"sprint",
						0.3f,
						((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["sprint"].speed
					});
				}
			}
		}
		if (crouching)
		{
			walkmulti = 0.5f;
		}
		else
		{
			walkmulti = 1f;
		}
		if (legstate == 2)
		{
			if (!crouching)
			{
				barulho = walknoise;
			}
			else
			{
				barulho = 0f;
			}
			sprintbar += sprintregen * 0.33f * Time.deltaTime;
			if (!(sprintbar <= 100f))
			{
				sprintbar = 100f;
			}
			vy = 0f;
			if (controled)
			{
				if (!crouching)
				{
					if (Input.GetKey((KeyCode)306))
					{
						((Component)legs).GetComponent<Animation>().CrossFade("crouch_forward", 0.3f);
						crouching = true;
						((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
						{
							"crouch_forward",
							0.3f,
							((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["crouch_forward"].speed
						});
					}
				}
				else if (!Input.GetKey((KeyCode)306))
				{
					((Component)legs).GetComponent<Animation>().CrossFade("walk", 0.3f);
					crouching = false;
					((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
					{
						"walk",
						0.3f,
						((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["walk"].speed
					});
				}
				stepconter += 45f * walkspeed * walkmulti * Time.deltaTime;
				if (!(stepconter <= 100f))
				{
					soundsources[1].clip = sounds[3];
					soundsources[1].Play();
					((Component)this).NetView().RPC("playasound", (RPCMode)1, new object[2] { 3, 1 });
					stepconter -= 100f;
				}
				movimento.z = Input.GetAxis("Vertical") * walkmulti * walkspeed;
				movimento.x = Input.GetAxis("Horizontal") * walkmulti * walkspeed;
				if (!(Input.GetAxis("Vertical") > 0f))
				{
					legstate = 1;
					if (!crouching)
					{
						((Component)legs).GetComponent<Animation>().CrossFade("standing", 0.2f);
						((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
						{
							"standing",
							0.2f,
							((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["standing"].speed
						});
					}
					else
					{
						((Component)legs).GetComponent<Animation>().CrossFade("crouch_idle", 0.2f);
						((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
						{
							"crouch_idle",
							0.2f,
							((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["crouch_idle"].speed
						});
					}
				}
				if (!(Input.GetAxis("Vertical") >= 0f))
				{
					legstate = 3;
					if (!crouching)
					{
						((Component)legs).GetComponent<Animation>().CrossFade("walkback", 0.3f);
						((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
						{
							"walkback",
							0.3f,
							((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["walkback"].speed
						});
					}
					else
					{
						((Component)legs).GetComponent<Animation>().CrossFade("crouch_back", 0.3f);
						((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
						{
							"crouch_back",
							0.3f,
							((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["crouch_back"].speed
						});
					}
				}
				if (!(Input.GetAxis("Sprint") <= 0f) && armstate == 0 && !(sprintbar < (float)mintosrpint))
				{
					legstate = 6;
					((Component)legs).GetComponent<Animation>().CrossFade("sprint", 0.3f);
					((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
					{
						"sprint",
						0.3f,
						((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["sprint"].speed
					});
					armstate = 1;
					((Component)chest).GetComponent<Animation>().CrossFade("sprint", 0.3f);
					((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
					{
						"sprint",
						0.3f,
						((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["sprint"].speed
					});
				}
			}
			else
			{
				legstate = 1;
				if (!crouching)
				{
					((Component)legs).GetComponent<Animation>().CrossFade("standing", 0.2f);
					((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
					{
						"standing",
						0.2f,
						((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["standing"].speed
					});
				}
				else
				{
					((Component)legs).GetComponent<Animation>().CrossFade("crouch_idle", 0.2f);
					((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
					{
						"crouch_idle",
						0.2f,
						((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["crouch_idle"].speed
					});
				}
			}
		}
		if (legstate == 3)
		{
			if (!crouching)
			{
				barulho = walknoise;
			}
			else
			{
				barulho = 0f;
			}
			sprintbar += sprintregen * 0.33f * Time.deltaTime;
			if (!(sprintbar <= 100f))
			{
				sprintbar = 100f;
			}
			vy = 0f;
			if (controled)
			{
				if (!crouching)
				{
					if (Input.GetKey((KeyCode)306))
					{
						((Component)legs).GetComponent<Animation>().CrossFade("crouch_back", 0.3f);
						crouching = true;
						((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
						{
							"crouch_back",
							0.3f,
							((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["crouch_back"].speed
						});
					}
				}
				else if (!crouching)
				{
					((Component)legs).GetComponent<Animation>().CrossFade("walkback", 0.3f);
					((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
					{
						"walkback",
						0.3f,
						((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["walkback"].speed
					});
				}
				else
				{
					((Component)legs).GetComponent<Animation>().CrossFade("crouch_back", 0.3f);
					((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
					{
						"crouch_back",
						0.3f,
						((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["crouch_back"].speed
					});
				}
				stepconter += 70f * walkbackmulti * walkmulti * walkspeed * Time.deltaTime;
				if (!(stepconter <= 100f))
				{
					soundsources[1].clip = sounds[3];
					soundsources[1].Play();
					((Component)this).NetView().RPC("playasound", (RPCMode)1, new object[2] { 3, 1 });
					stepconter -= 100f;
				}
				movimento.z = Input.GetAxis("Vertical") * walkmulti * walkspeed * walkbackmulti;
				movimento.x = Input.GetAxis("Horizontal") * walkmulti * walkspeed * walkbackmulti;
				if (!(Input.GetAxis("Vertical") < 0f))
				{
					legstate = 1;
					if (!crouching)
					{
						((Component)legs).GetComponent<Animation>().CrossFade("standing", 0.2f);
						((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
						{
							"standing",
							0.2f,
							((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["standing"].speed
						});
					}
					else
					{
						((Component)legs).GetComponent<Animation>().CrossFade("crouch_idle", 0.2f);
						((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
						{
							"crouch_idle",
							0.2f,
							((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["crouch_idle"].speed
						});
					}
				}
				if (!(Input.GetAxis("Vertical") <= 0f))
				{
					legstate = 2;
					if (!crouching)
					{
						((Component)legs).GetComponent<Animation>().CrossFade("walk", 0.3f);
						((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
						{
							"walk",
							0.3f,
							((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["walk"].speed
						});
					}
					else
					{
						((Component)legs).GetComponent<Animation>().CrossFade("crouch_forward", 0.3f);
						((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
						{
							"crouch_forward",
							0.3f,
							((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["crouch_forward"].speed
						});
					}
				}
				if (!(Input.GetAxis("Sprint") <= 0f) && armstate == 0 && !(sprintbar < (float)mintosrpint))
				{
					legstate = 6;
					((Component)legs).GetComponent<Animation>().CrossFade("sprint", 0.3f);
					((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
					{
						"sprint",
						0.3f,
						((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["sprint"].speed
					});
					armstate = 1;
					((Component)chest).GetComponent<Animation>().CrossFade("sprint", 0.3f);
					((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
					{
						"sprint",
						0.3f,
						((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["sprint"].speed
					});
				}
			}
			else
			{
				legstate = 1;
				if (!crouching)
				{
					((Component)legs).GetComponent<Animation>().CrossFade("standing", 0.2f);
					((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
					{
						"standing",
						0.2f,
						((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["standing"].speed
					});
				}
				else
				{
					((Component)legs).GetComponent<Animation>().CrossFade("crouch_idle", 0.2f);
					((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
					{
						"crouch_idle",
						0.2f,
						((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["crouch_idle"].speed
					});
				}
			}
		}
		if (legstate == 4)
		{
			if (!crouching)
			{
				barulho = walknoise;
			}
			else
			{
				barulho = 0f;
			}
			sprintbar += sprintregen * 0.33f * Time.deltaTime;
			if (!(sprintbar <= 100f))
			{
				sprintbar = 100f;
			}
			vy = 0f;
			if (controled)
			{
				if (!crouching)
				{
					if (Input.GetKey((KeyCode)306))
					{
						((Component)legs).GetComponent<Animation>().CrossFade("crouch_left", 0.3f);
						crouching = true;
						((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
						{
							"crouch_left",
							0.3f,
							((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["crouch_left"].speed
						});
					}
				}
				else if (!Input.GetKey((KeyCode)306))
				{
					((Component)legs).GetComponent<Animation>().CrossFade("walkleft", 0.3f);
					crouching = false;
					((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
					{
						"walkleft",
						0.3f,
						((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["walkleft"].speed
					});
				}
				stepconter += 45f * walkmulti * walkspeed * Time.deltaTime;
				if (!(stepconter <= 100f))
				{
					soundsources[1].clip = sounds[3];
					soundsources[1].Play();
					((Component)this).NetView().RPC("playasound", (RPCMode)1, new object[2] { 3, 1 });
					stepconter -= 100f;
				}
				movimento.x = Input.GetAxis("Horizontal") * walkmulti * walkspeed;
				if (Input.GetAxis("Horizontal") == 0f)
				{
					legstate = 1;
					if (!crouching)
					{
						((Component)legs).GetComponent<Animation>().CrossFade("standing", 0.2f);
						((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
						{
							"standing",
							0.2f,
							((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["standing"].speed
						});
					}
					else
					{
						((Component)legs).GetComponent<Animation>().CrossFade("crouch_idle", 0.2f);
						((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
						{
							"crouch_idle",
							0.2f,
							((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["crouch_idle"].speed
						});
					}
				}
				if (!(Input.GetAxis("Horizontal") <= 0f))
				{
					legstate = 5;
					((Component)legs).GetComponent<Animation>().CrossFade("walkleft", 0.2f);
					((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
					{
						"walkleft",
						0.2f,
						((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["walkleft"].speed
					});
				}
				if (!(Input.GetAxis("Vertical") <= 0f))
				{
					legstate = 2;
					if (!crouching)
					{
						((Component)legs).GetComponent<Animation>().CrossFade("walk", 0.3f);
						((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
						{
							"walk",
							0.3f,
							((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["walk"].speed
						});
					}
					else
					{
						((Component)legs).GetComponent<Animation>().CrossFade("crouch_forward", 0.3f);
						((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
						{
							"crouch_forward",
							0.3f,
							((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["crouch_forward"].speed
						});
					}
				}
				if (!(Input.GetAxis("Vertical") >= 0f))
				{
					legstate = 3;
					if (!crouching)
					{
						((Component)legs).GetComponent<Animation>().CrossFade("walkback", 0.3f);
						((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
						{
							"walkback",
							0.3f,
							((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["walkback"].speed
						});
					}
					else
					{
						((Component)legs).GetComponent<Animation>().CrossFade("crouch_back", 0.3f);
						((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
						{
							"crouch_back",
							0.3f,
							((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["crouch_back"].speed
						});
					}
				}
				if (!(Input.GetAxis("Sprint") <= 0f) && armstate == 0 && !(sprintbar < (float)mintosrpint))
				{
					legstate = 6;
					((Component)legs).GetComponent<Animation>().CrossFade("sprint", 0.3f);
					((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
					{
						"sprint",
						0.3f,
						((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["sprint"].speed
					});
					armstate = 1;
					((Component)chest).GetComponent<Animation>().CrossFade("sprint", 0.3f);
					((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
					{
						"sprint",
						0.3f,
						((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["sprint"].speed
					});
				}
			}
			else
			{
				legstate = 1;
				if (!crouching)
				{
					((Component)legs).GetComponent<Animation>().CrossFade("standing", 0.2f);
					((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
					{
						"standing",
						0.2f,
						((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["standing"].speed
					});
				}
				else
				{
					((Component)legs).GetComponent<Animation>().CrossFade("crouch_idle", 0.2f);
					((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
					{
						"crouch_idle",
						0.2f,
						((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["crouch_idle"].speed
					});
				}
			}
		}
		if (legstate == 5)
		{
			if (!crouching)
			{
				barulho = walknoise;
			}
			else
			{
				barulho = 0f;
			}
			sprintbar += sprintregen * 0.33f * Time.deltaTime;
			if (!(sprintbar <= 100f))
			{
				sprintbar = 100f;
			}
			vy = 0f;
			if (controled)
			{
				if (!crouching)
				{
					if (Input.GetKey((KeyCode)306))
					{
						((Component)legs).GetComponent<Animation>().CrossFade("crouch_right", 0.3f);
						crouching = true;
						((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
						{
							"crouch_right",
							0.3f,
							((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["crouch_right"].speed
						});
					}
				}
				else if (!Input.GetKey((KeyCode)306))
				{
					((Component)legs).GetComponent<Animation>().CrossFade("walkleft", 0.3f);
					crouching = false;
					((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
					{
						"walkleft",
						0.3f,
						((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["walkleft"].speed
					});
				}
				stepconter += 45f * walkmulti * walkspeed * Time.deltaTime;
				if (!(stepconter <= 100f))
				{
					soundsources[1].clip = sounds[3];
					soundsources[1].Play();
					((Component)this).NetView().RPC("playasound", (RPCMode)1, new object[2] { 3, 1 });
					stepconter -= 100f;
				}
				movimento.x = Input.GetAxis("Horizontal") * walkmulti * walkspeed;
				if (Input.GetAxis("Horizontal") == 0f)
				{
					legstate = 1;
					if (!crouching)
					{
						((Component)legs).GetComponent<Animation>().CrossFade("standing", 0.2f);
						((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
						{
							"standing",
							0.2f,
							((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["standing"].speed
						});
					}
					else
					{
						((Component)legs).GetComponent<Animation>().CrossFade("crouch_idle", 0.2f);
						((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
						{
							"crouch_idle",
							0.2f,
							((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["crouch_idle"].speed
						});
					}
				}
				if (!(Input.GetAxis("Horizontal") >= 0f))
				{
					legstate = 4;
					((Component)legs).GetComponent<Animation>().CrossFade("walkleft", 0.2f);
					((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
					{
						"walkleft",
						0.2f,
						((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["walkleft"].speed
					});
				}
				if (!(Input.GetAxis("Vertical") <= 0f))
				{
					legstate = 2;
					if (!crouching)
					{
						((Component)legs).GetComponent<Animation>().CrossFade("walk", 0.3f);
						((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
						{
							"walk",
							0.3f,
							((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["walk"].speed
						});
					}
					else
					{
						((Component)legs).GetComponent<Animation>().CrossFade("crouch_forward", 0.3f);
						((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
						{
							"crouch_forward",
							0.3f,
							((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["crouch_forward"].speed
						});
					}
				}
				if (!(Input.GetAxis("Vertical") >= 0f))
				{
					legstate = 3;
					if (!crouching)
					{
						((Component)legs).GetComponent<Animation>().CrossFade("walkback", 0.3f);
						((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
						{
							"walkback",
							0.3f,
							((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["walkback"].speed
						});
					}
					else
					{
						((Component)legs).GetComponent<Animation>().CrossFade("crouch_back", 0.3f);
						((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
						{
							"crouch_back",
							0.3f,
							((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["crouch_back"].speed
						});
					}
				}
				if (!(Input.GetAxis("Sprint") <= 0f) && armstate == 0 && !(sprintbar < (float)mintosrpint))
				{
					legstate = 6;
					((Component)legs).GetComponent<Animation>().CrossFade("sprint", 0.3f);
					((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
					{
						"sprint",
						0.3f,
						((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["sprint"].speed
					});
					armstate = 1;
					((Component)chest).GetComponent<Animation>().CrossFade("sprint", 0.3f);
					((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
					{
						"sprint",
						0.3f,
						((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["sprint"].speed
					});
				}
			}
			else
			{
				legstate = 1;
				if (!crouching)
				{
					((Component)legs).GetComponent<Animation>().CrossFade("standing", 0.2f);
					((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
					{
						"standing",
						0.2f,
						((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["standing"].speed
					});
				}
				else
				{
					((Component)legs).GetComponent<Animation>().CrossFade("crouch_idle", 0.2f);
					((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
					{
						"crouch_idle",
						0.2f,
						((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["crouch_idle"].speed
					});
				}
			}
		}
		if (crouching)
		{
			real_crouching = true;
		}
		if (legstate != 1 && legstate != 2 && legstate != 3 && legstate != 4 && legstate != 5)
		{
			crouching = false;
		}
		if (crouching)
		{
			crouch_cont = 0f;
		}
		else if (real_crouching)
		{
			crouch_cont += 1f * Time.deltaTime;
			if (!(crouch_cont <= 0.3f))
			{
				real_crouching = false;
			}
		}
		if (crouching)
		{
			spreadcoef = 0.5f;
		}
		else
		{
			spreadcoef = 1f;
		}
		if (real_crouching)
		{
			exposiback = 0f;
			exposi = 0.3f;
		}
		else
		{
			exposiback = 1f;
			exposi = 1f;
		}
		if (legstate == 6)
		{
			barulho = sprintnoise;
			vy = 0f;
			if (controled)
			{
				stepconter += 30f * sprintspeed * Time.deltaTime;
				if (!(stepconter <= 100f))
				{
					soundsources[1].clip = sounds[3];
					soundsources[1].Play();
					((Component)this).NetView().RPC("playasound", (RPCMode)1, new object[2] { 3, 1 });
					stepconter -= 100f;
				}
				movimento.z = Input.GetAxis("Sprint") * sprintspeed;
				sprintbar -= Input.GetAxis("Sprint") * sprintdecay * Time.deltaTime;
				if (!(sprintbar >= 0f))
				{
					sprintbar = 0f;
				}
				movimento.x = Input.GetAxis("Horizontal") * walkspeed;
				if (Input.GetAxis("Sprint") <= 0f || !(sprintbar > 0f))
				{
					legstate = 1;
					((Component)legs).GetComponent<Animation>().CrossFade("standing", 0.2f);
					((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
					{
						"standing",
						0.2f,
						((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["standing"].speed
					});
					armstate = 11;
					animationcont = 0f;
					futurearmstate = 0;
					waitingtime = 250;
					if (item[itemselected] >= 0)
					{
						animateinoptions();
					}
					else
					{
						((Component)chest).GetComponent<Animation>().CrossFade("nogunidle", 0.2f);
						((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
						{
							"nogunidle",
							0.2f,
							((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["nogunidle"].speed
						});
					}
				}
			}
			else
			{
				legstate = 1;
				((Component)legs).GetComponent<Animation>().CrossFade("standing", 0.2f);
				((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
				{
					"standing",
					0.2f,
					((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["standing"].speed
				});
				if (item[itemselected] >= 0)
				{
					animateinoptions();
				}
				else
				{
					armstate = 0;
					((Component)chest).GetComponent<Animation>().CrossFade("nogunidle", 0.2f);
					((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
					{
						"nogunidle",
						0.2f,
						((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["nogunidle"].speed
					});
				}
			}
		}
		if (legstate == 8)
		{
			barulho = 0f;
			sprintbar += sprintregen * Time.deltaTime;
			if (!(sprintbar <= 100f))
			{
				sprintbar = 100f;
			}
			((Component)this).transform.rotation = Quaternion.Lerp(((Component)this).transform.rotation, interact_obj.transform.rotation, 10f * Time.deltaTime);
			val4 = interact_obj.transform.position;
			val4.y = ((Component)this).transform.position.y;
			((Component)this).transform.position = Vector3.Lerp(((Component)this).transform.position, val4 - interact_obj.transform.forward * 0.65f, 10f * Time.deltaTime);
			float y = ((Component)this).transform.position.y;
			float y2 = interact_obj.transform.position.y;
			Bounds bounds = interact_obj.GetComponent<Collider>().bounds;
			if (!(y <= y2 + bounds.size.y / 2f - 0.8f))
			{
				((Component)this).transform.Translate(-Vector3.up * 4f * Time.deltaTime);
			}
			else
			{
				canmoveinladder = true;
			}
			if (Input.GetButtonDown("Jump") && controled)
			{
				armrottype = 0;
				((Component)this).NetView().RPC("setrot", (RPCMode)1, new object[1] { armrottype });
				canspin = true;
				legstate = 0;
				((Component)legs).GetComponent<Animation>().CrossFade("jump", 0.3f);
				((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
				{
					"jump",
					0.3f,
					((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["jump"].speed
				});
				vy = jumpspeed;
				armstate = 11;
				animationcont = 0f;
				futurearmstate = 0;
				waitingtime = 250;
				if (item[itemselected] >= 0)
				{
					animateinoptions();
				}
				else
				{
					((Component)chest).GetComponent<Animation>().CrossFade("nogunidle", 0.2f);
					((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
					{
						"nogunidle",
						0.2f,
						((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["nogunidle"].speed
					});
				}
			}
			if (controled && Input.GetAxis("Vertical") != 0f && canmoveinladder)
			{
				((Component)legs).GetComponent<Animation>().CrossFade("move_ladder", 0.2f);
				((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
				{
					"move_ladder",
					0.2f,
					((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["move_ladder"].speed
				});
				((Component)chest).GetComponent<Animation>().CrossFade("move_ladder", 0.2f);
				((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
				{
					"move_ladder",
					0.2f,
					((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["move_ladder"].speed
				});
				legstate = 9;
			}
		}
		if (legstate == 9)
		{
			barulho = 0f;
			sprintbar += sprintregen * Time.deltaTime;
			if (!(sprintbar <= 100f))
			{
				sprintbar = 100f;
			}
			((Component)this).transform.Translate(Vector3.up * Input.GetAxis("Vertical") * 2f * Time.deltaTime);
			((Component)this).transform.rotation = Quaternion.Lerp(((Component)this).transform.rotation, interact_obj.transform.rotation, 10f * Time.deltaTime);
			val4 = interact_obj.transform.position;
			val4.y = ((Component)this).transform.position.y;
			((Component)this).transform.position = Vector3.Lerp(((Component)this).transform.position, val4 - interact_obj.transform.forward * 0.65f, 10f * Time.deltaTime);
			if (Input.GetAxis("Vertical") == 0f || !controled)
			{
				((Component)legs).GetComponent<Animation>().CrossFade("hold_ladder", 0.2f);
				((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
				{
					"hold_ladder",
					0.2f,
					((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["hold_ladder"].speed
				});
				((Component)chest).GetComponent<Animation>().CrossFade("hold_ladder", 0.2f);
				((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
				{
					"hold_ladder",
					0.2f,
					((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["hold_ladder"].speed
				});
				legstate = 8;
			}
			if (!(Input.GetAxis("Vertical") <= 0f))
			{
				float y3 = ((Component)this).transform.position.y;
				float y4 = interact_obj.transform.position.y;
				Bounds bounds2 = interact_obj.GetComponent<Collider>().bounds;
				if (!(y3 <= y4 + bounds2.size.y / 2f - 0.8f))
				{
					animationcont = 0f;
					legstate = 10;
					((Component)chest).GetComponent<Animation>().CrossFade("climb_from_ladder", 0.2f);
					((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
					{
						"climb_from_ladder",
						0.2f,
						((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["climb_from_ladder"].speed
					});
					((Component)legs).GetComponent<Animation>().CrossFade("climb_from_ladder", 0.2f);
					((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
					{
						"climb_from_ladder",
						0.2f,
						((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["climb_from_ladder"].speed
					});
				}
			}
			if (!(Input.GetAxis("Vertical") >= 0f))
			{
				if (!isgrounded)
				{
					float y5 = ((Component)this).transform.position.y;
					float y6 = interact_obj.transform.position.y;
					Bounds bounds3 = interact_obj.GetComponent<Collider>().bounds;
					if (y5 >= y6 - bounds3.size.y / 2f)
					{
						goto IL_3d0c;
					}
				}
				armrottype = 0;
				((Component)this).NetView().RPC("setrot", (RPCMode)1, new object[1] { armrottype });
				canspin = true;
				legstate = 1;
				((Component)legs).GetComponent<Animation>().CrossFade("standing", 0.2f);
				((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
				{
					"standing",
					0.2f,
					((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["standing"].speed
				});
				armstate = 11;
				animationcont = 0f;
				futurearmstate = 0;
				waitingtime = 250;
				if (item[itemselected] >= 0)
				{
					animateinoptions();
				}
				else
				{
					((Component)chest).GetComponent<Animation>().CrossFade("nogunidle", 0.2f);
					((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
					{
						"nogunidle",
						0.2f,
						((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["nogunidle"].speed
					});
				}
			}
			goto IL_3d0c;
		}
		goto IL_3ea4;
		IL_3ea4:
		if (legstate == 10)
		{
			barulho = 0f;
			sprintbar += sprintregen * Time.deltaTime;
			if (!(sprintbar <= 100f))
			{
				sprintbar = 100f;
			}
			((Component)this).transform.rotation = Quaternion.Lerp(((Component)this).transform.rotation, interact_obj.transform.rotation, 10f * Time.deltaTime);
			float y7 = ((Component)this).transform.position.y;
			float y8 = interact_obj.transform.position.y;
			Bounds bounds4 = interact_obj.GetComponent<Collider>().bounds;
			if (!(y7 <= y8 + bounds4.size.y / 2f + 0.5f))
			{
				((Component)this).transform.Translate(Vector3.forward * 2f * Time.deltaTime);
			}
			float y9 = ((Component)this).transform.position.y;
			float y10 = interact_obj.transform.position.y;
			Bounds bounds5 = interact_obj.GetComponent<Collider>().bounds;
			if (!(y9 >= y10 + bounds5.size.y / 2f + 0.7f))
			{
				((Component)this).transform.Translate(Vector3.up * 2f * Time.deltaTime);
			}
			animationcont += 100f * Time.deltaTime;
			if (!(animationcont <= 150f))
			{
				armrottype = 0;
				((Component)this).NetView().RPC("setrot", (RPCMode)1, new object[1] { armrottype });
				canspin = true;
				legstate = 1;
				((Component)legs).GetComponent<Animation>().CrossFade("standing", 0.2f);
				((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
				{
					"standing",
					0.2f,
					((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["standing"].speed
				});
				armstate = 11;
				animationcont = 0f;
				futurearmstate = 0;
				waitingtime = 250;
				if (item[itemselected] >= 0)
				{
					animateinoptions();
				}
				else
				{
					((Component)chest).GetComponent<Animation>().CrossFade("nogunidle", 0.2f);
					((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
					{
						"nogunidle",
						0.2f,
						((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["nogunidle"].speed
					});
				}
			}
		}
		if (legstate == 1 || legstate == 2 || legstate == 3 || legstate == 4 || legstate == 5 || legstate == 6)
		{
			if (controled && Input.GetButton("Jump"))
			{
				if (legstate == 6)
				{
					armstate = 0;
					if (item[itemselected] >= 0)
					{
						animateinoptions();
					}
					else
					{
						((Component)chest).GetComponent<Animation>().CrossFade("nogunidle", 0.2f);
						((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
						{
							"nogunidle",
							0.2f,
							((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["nogunidle"].speed
						});
					}
				}
				legstate = 0;
				((Component)legs).GetComponent<Animation>().CrossFade("jump", 0.3f);
				((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
				{
					"jump",
					0.3f,
					((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["jump"].speed
				});
				barulho = 80f;
				vy = jumpspeed;
			}
			if (!isgrounded)
			{
				if (legstate == 6)
				{
					armstate = 0;
					if (item[itemselected] >= 0)
					{
						animateinoptions();
					}
					else
					{
						((Component)chest).GetComponent<Animation>().CrossFade("nogunidle", 0.2f);
						((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
						{
							"nogunidle",
							0.2f,
							((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["nogunidle"].speed
						});
					}
				}
				legstate = 0;
				((Component)legs).GetComponent<Animation>().CrossFade("fall", 0.5f);
				((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
				{
					"fall",
					0.3f,
					((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["fall"].speed
				});
				vy = 0f;
			}
		}
		if (controled)
		{
			headang += Input.GetAxis("Mouse Y") * mouse_sense;
		}
		if (!(headang <= (float)headanglim))
		{
			headang = headanglim;
		}
		if (!(headang >= (float)(-headanglim)))
		{
			headang = -headanglim;
		}
		if (controled && canspin)
		{
			((Component)this).transform.Rotate(new Vector3(0f, Input.GetAxis("Mouse X") * mouse_sense, 0f));
		}
		if (armrottype == 0)
		{
			((Component)head).transform.Rotate(new Vector3(0f, 0f, headang));
			for (num = 0; num < arms.Length; num++)
			{
				arms[num].RotateAround(pivo.position, pivo.right, 0f - headang);
			}
		}
		for (num = 0; num < bendingbones.Length; num++)
		{
			bendingbones[num].Rotate(((Component)this).transform.forward * bending, (Space)0);
		}
		if (armrottype == 1)
		{
			((Component)head).transform.Rotate(new Vector3(0f, 0f, headang));
		}
		interacttype = -1;
		val3 = (149979392);
		if (Physics.Raycast(cam.position, cam.forward, out val2, userange, (val3)))
		{
			if (((Component)val2.collider).gameObject.layer == 15)
			{
				interacttype = 0;
			}
			if (((Component)val2.collider).gameObject.layer == 20 && !((sc_deadplayer)(object)((Component)((sc_deadbodyparts)(object)((Component)val2.collider).gameObject.GetComponent(typeof(sc_deadbodyparts))).owner).GetComponent(typeof(sc_deadplayer))).died)
			{
				interacttype = 1;
			}
			if (((Component)val2.collider).gameObject.layer == 22)
			{
				interacttype = 2;
			}
			if (((Component)val2.collider).gameObject.layer == 23)
			{
				interacttype = 3;
			}
			if (((Component)val2.collider).gameObject.layer == 27)
			{
				if (!(((Component)val2.collider).gameObject.transform.InverseTransformPoint(((Component)this).transform.position).z >= 0f))
				{
					if (!(val2.distance >= userange / 2f))
					{
						float y11 = ((Component)this).transform.position.y;
						float y12 = ((Component)val2.collider).gameObject.transform.position.y;
						Bounds bounds6 = val2.collider.bounds;
						if (!(y11 <= y12 - bounds6.size.y / 2f + 0.7f))
						{
							float y13 = ((Component)this).transform.position.y;
							float y14 = ((Component)val2.collider).gameObject.transform.position.y;
							Bounds bounds7 = val2.collider.bounds;
							if (!(y13 >= y14 + bounds7.size.y / 2f - 1f))
							{
								interacttype = 4;
							}
						}
					}
				}
				else
				{
					float y15 = ((Component)this).transform.position.y;
					float y16 = ((Component)val2.collider).gameObject.transform.position.y;
					Bounds bounds8 = val2.collider.bounds;
					if (!(y15 <= y16 + bounds8.size.y / 2f))
					{
						interacttype = 4;
					}
				}
			}
		}
		num2 = 0;
		if (controled && item[itemselected] >= 0 && armstate == 0 && ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[itemselected]].tipo == 0)
		{
			num2 = 1;
			if (autoaim)
			{
				aiming = Input.GetButton("Aim");
			}
			else if (Input.GetButtonDown("Aim"))
			{
				aiming = !aiming;
			}
		}
		if (num2 == 0)
		{
			aiming = false;
		}
		if (armstate == 15)
		{
			animationcont += 100f * Time.deltaTime;
			if (!(animationcont <= 30f))
			{
				throw_charge += 0.8f * Time.deltaTime;
				if (!(throw_charge <= 1f))
				{
					throw_charge = 1f;
				}
				if (!Input.GetButton("Shoot"))
				{
					((Component)chest).GetComponent<Animation>().CrossFade("throw_lance", 0.1f);
					((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
					{
						"throw_lance",
						0.1f,
						((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["throw_lance"].speed
					});
					armstate = 16;
					animationcont = 0f;
					reloadact1 = false;
				}
			}
		}
		if (armstate == 16)
		{
			animationcont += 100f * Time.deltaTime;
			if (!(animationcont <= 20f) && !reloadact1)
			{
				val = (Transform)Network.Instantiate((Object)(object)((sc_throwable)(object)((Component)handitem).GetComponent(typeof(sc_throwable))).throw_prefab, handitem.position, handitem.rotation, 3);
				if (((Object)(object)(sc_granada)(object)((Component)((sc_throwable)(object)((Component)handitem).GetComponent(typeof(sc_throwable))).throw_prefab).GetComponent(typeof(sc_granada)) != null))
				{
					((sc_granada)(object)((Component)((sc_throwable)(object)((Component)handitem).GetComponent(typeof(sc_throwable))).throw_prefab).GetComponent(typeof(sc_granada))).shooter = myid;
					((sc_granada)(object)((Component)((sc_throwable)(object)((Component)handitem).GetComponent(typeof(sc_throwable))).throw_prefab).GetComponent(typeof(sc_granada))).shooterobj = ((Component)this).transform;
				}
				((Behaviour)(sc_sendpositionupdate)(object)((Component)val).GetComponent(typeof(sc_sendpositionupdate))).enabled = true;
				((Behaviour)(sc_updateposition)(object)((Component)val).GetComponent(typeof(sc_updateposition))).enabled = false;
				if (((Object)(object)((Component)val).GetComponent<Rigidbody>() != null))
				{
					((Component)val).GetComponent<Rigidbody>().AddForce(throw_charge * cam.forward * ((sc_throwable)(object)((Component)handitem).GetComponent(typeof(sc_throwable))).throw_force);
				}
				ammo[itemselected]--;
				Object.Destroy((Object)(object)((Component)handitem).gameObject);
				reloadact1 = true;
			}
			if (!(animationcont <= 50f))
			{
				if (ammo[itemselected] > 0)
				{
					animationcont = 0f;
					armstate = 2;
					((Component)chest).GetComponent<Animation>().CrossFade("changeitem", 0.2f);
					((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
					{
						"changeitem",
						0.2f,
						((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["changeitem"].speed
					});
				}
				else
				{
					armstate = 0;
					item[itemselected] = -1;
					quant[itemselected] = 0;
					ammo[itemselected] = 0;
					((Component)this).NetView().RPC("changeitem", (RPCMode)1, new object[1] { item[itemselected] });
					((Component)chest).GetComponent<Animation>().CrossFade("nogunidle", 0.2f);
					((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
					{
						"nogunidle",
						0.2f,
						((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["nogunidle"].speed
					});
				}
			}
		}
		if ((armstate == 0 || armstate == 11 || armstate == 12 || armstate == 14 || armstate == 2 || armstate == 3 || armstate == 4 || armstate == 5) && controled)
		{
			num2 = 0;
			if (Input.GetKey((KeyCode)122))
			{
				val3 = (256);
				if (!Physics.Raycast(((Component)this).transform.position + Vector3.up * 0.5f, -((Component)this).transform.right, out val2, 0.7f, (val3)))
				{
					bending = Mathf.Lerp(bending, 25f, 3f * Time.deltaTime);
				}
				else
				{
					num2 = 1;
				}
			}
			if (Input.GetKey((KeyCode)120))
			{
				if (!Physics.Raycast(((Component)this).transform.position + Vector3.up * 0.5f, ((Component)this).transform.right, out val2, 0.7f, (val3)))
				{
					bending = Mathf.Lerp(bending, -25f, 3f * Time.deltaTime);
				}
				else
				{
					num2 = 1;
				}
			}
			if (!Input.GetKey((KeyCode)122) && !Input.GetKey((KeyCode)120))
			{
				num2 = 1;
			}
			if (num2 == 1)
			{
				bending = Mathf.Lerp(bending, 0f, 4f * Time.deltaTime);
			}
		}
		else
		{
			bending = Mathf.Lerp(bending, 0f, 4f * Time.deltaTime);
		}
		if ((Object)(object)cintoitem != (Object)null)
		{
			if ((Object)(object)(sc_throwable)(object)((Component)cintoitem).GetComponent(typeof(sc_throwable)) != (Object)null && ((sc_throwable)(object)((Component)cintoitem).GetComponent(typeof(sc_throwable))).islight)
			{
				((Behaviour)((Component)luminassao).GetComponent<Light>()).enabled = false;
			}
			cinto_timer += 1f * Time.deltaTime;
			if (!(cinto_timer <= cinto_timerend))
			{
				Object.Destroy((Object)(object)((Component)cintoitem).gameObject);
				((Component)this).NetView().RPC("changecintoitem", (RPCMode)1, new object[1] { -1 });
				cinto_timer = 0f;
			}
		}
		else
		{
			((Behaviour)((Component)luminassao).GetComponent<Light>()).enabled = true;
		}
		if (armstate == 0 && controled && item[itemselected] >= 0 && ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[itemselected]].tipo == 4 && (Object)(object)handitem != (Object)null && ((sc_throwable)(object)((Component)handitem).GetComponent(typeof(sc_throwable))).cinturavel && Input.GetKey((KeyCode)325))
		{
			cinto_timer = 0f;
			cinto_timerend = ((sc_throwable)(object)((Component)handitem).GetComponent(typeof(sc_throwable))).decayend;
			ammo[itemselected]--;
			if ((Object)(object)cintoitem != (Object)null)
			{
				Object.Destroy((Object)(object)((Component)cintoitem).gameObject);
			}
			cintoitem = handitem;
			handitem = null;
			cintoitem.parent = cintopos;
			cintoitem.localPosition = Vector3.zero;
			cintoitem.localEulerAngles = Vector3.zero;
			((Component)this).NetView().RPC("changeitem", (RPCMode)1, new object[1] { item[itemselected] });
			reloadact1 = true;
			((Component)this).NetView().RPC("changecintoitem", (RPCMode)1, new object[1] { item[itemselected] });
			if (ammo[itemselected] > 0)
			{
				animationcont = 0f;
				armstate = 2;
				((Component)chest).GetComponent<Animation>().CrossFade("changeitem", 0.2f);
				((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
				{
					"changeitem",
					0.2f,
					((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["changeitem"].speed
				});
			}
			else
			{
				armstate = 0;
				item[itemselected] = -1;
				quant[itemselected] = 0;
				ammo[itemselected] = 0;
				((Component)chest).GetComponent<Animation>().CrossFade("nogunidle", 0.2f);
				((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
				{
					"nogunidle",
					0.2f,
					((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["nogunidle"].speed
				});
			}
			((Component)this).NetView().RPC("changeitem", (RPCMode)1, new object[1] { item[itemselected] });
		}
		if (armstate != 0 && (Object)(object)placer != (Object)null)
		{
			Object.Destroy((Object)(object)((Component)placer).gameObject);
		}
		int num6 = default(int);
		if (armstate == 0)
		{
			if ((Object)(object)lhandmag != (Object)null)
			{
				Object.Destroy((Object)(object)((Component)lhandmag).gameObject);
			}
			if (controled)
			{
				itemtoselect = (int)((float)itemtoselect - Input.GetAxis("MouseWheel"));
				if (itemtoselect < 0)
				{
					itemtoselect = 9;
				}
				if (itemtoselect > 9)
				{
					itemtoselect = 0;
				}
				if (Input.GetKey("1"))
				{
					itemtoselect = 0;
				}
				if (Input.GetKey("2"))
				{
					itemtoselect = 1;
				}
				if (Input.GetKey("3"))
				{
					itemtoselect = 2;
				}
				if (Input.GetKey("4"))
				{
					itemtoselect = 3;
				}
				if (Input.GetKey("5"))
				{
					itemtoselect = 4;
				}
				if (Input.GetKey("6"))
				{
					itemtoselect = 5;
				}
				if (Input.GetKey("7"))
				{
					itemtoselect = 6;
				}
				if (Input.GetKey("8"))
				{
					itemtoselect = 7;
				}
				if (Input.GetKey("9"))
				{
					itemtoselect = 8;
				}
				if (Input.GetKey("0"))
				{
					itemtoselect = 9;
				}
				if (Input.GetButtonDown("Interact"))
				{
					reloadact1 = false;
					armstate = 6;
					animationcont = 0f;
					((Component)chest).GetComponent<Animation>().CrossFade("interact", 0.2f);
					((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
					{
						"interact",
						0.4f,
						((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["interact"].speed
					});
				}
			}
			if (item[itemselected] >= 0 && controled && Input.GetButtonDown("Drop"))
			{
				num2 = ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[itemselected]].tipo;
				if (Input.GetKey((KeyCode)308) && (num2 == 3 || num2 == 4))
				{
					if (Network.isClient)
					{
						((Component)myspawner).NetView().RPC("dropitem", (RPCMode)0, new object[5]
						{
							item[itemselected],
							((Component)this).transform.position + ((Component)this).transform.forward * 0.4f,
							((Component)this).transform.rotation,
							quant[itemselected],
							1
						});
					}
					else
					{
						((sc_spawner)(object)((Component)myspawner).GetComponent(typeof(sc_spawner))).dropitem(item[itemselected], ((Component)this).transform.position + ((Component)this).transform.forward * 0.4f, ((Component)this).transform.rotation, quant[itemselected], 1);
					}
					ammo[itemselected]--;
					if (ammo[itemselected] <= 0)
					{
						item[itemselected] = -1;
						quant[itemselected] = 0;
						ammo[itemselected] = 0;
						Object.Destroy((Object)(object)((Component)handitem).gameObject);
						((Component)this).NetView().RPC("changeitem", (RPCMode)1, new object[1] { item[itemselected] });
						((Component)chest).GetComponent<Animation>().CrossFade("nogunidle", 0.2f);
						((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
						{
							"nogunidle",
							0.2f,
							((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["nogunidle"].speed
						});
						if ((Object)(object)placer != (Object)null)
						{
							Object.Destroy((Object)(object)((Component)placer).gameObject);
						}
					}
				}
				else
				{
					if (Network.isClient)
					{
						((Component)myspawner).NetView().RPC("dropitem", (RPCMode)0, new object[5]
						{
							item[itemselected],
							((Component)this).transform.position + ((Component)this).transform.forward * 0.4f,
							((Component)this).transform.rotation,
							quant[itemselected],
							ammo[itemselected]
						});
					}
					else
					{
						((sc_spawner)(object)((Component)myspawner).GetComponent(typeof(sc_spawner))).dropitem(item[itemselected], ((Component)this).transform.position + ((Component)this).transform.forward * 0.4f, ((Component)this).transform.rotation, quant[itemselected], ammo[itemselected]);
					}
					item[itemselected] = -1;
					quant[itemselected] = 0;
					ammo[itemselected] = 0;
					Object.Destroy((Object)(object)((Component)handitem).gameObject);
					((Component)this).NetView().RPC("changeitem", (RPCMode)1, new object[1] { item[itemselected] });
					((Component)chest).GetComponent<Animation>().CrossFade("nogunidle", 0.2f);
					((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
					{
						"nogunidle",
						0.2f,
						((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["nogunidle"].speed
					});
					if ((Object)(object)placer != (Object)null)
					{
						Object.Destroy((Object)(object)((Component)placer).gameObject);
					}
				}
			}
			if (item[itemselected] >= 0 && controled && ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[itemselected]].tipo == 5)
			{
				if (!(place_conter < 1f))
				{
					place_conter += Time.deltaTime;
				}
				if ((Object)(object)placer == (Object)null)
				{
					placer = (Transform)Object.Instantiate((Object)(object)((sc_fortification)(object)((Component)handitem).GetComponent(typeof(sc_fortification))).ghost_fab, ((Component)this).transform.position, ((Component)this).transform.rotation);
				}
				if ((Object)(object)placer != (Object)null)
				{
					placer.rotation = ((Component)this).transform.rotation;
					val3 = (257);
					if (Physics.Raycast(cam.position, cam.forward, out val2, 5f, (val3)))
					{
						placer.position = val2.point + Vector3.up * ((sc_fortification)(object)((Component)handitem).GetComponent(typeof(sc_fortification))).upintheair;
					}
					else
					{
						val4 = cam.position + cam.forward * 5f;
						if (Physics.Raycast(val4, Vector3.down, out val2, 3f, (val3)))
						{
							placer.position = val2.point + Vector3.up * ((sc_fortification)(object)((Component)handitem).GetComponent(typeof(sc_fortification))).upintheair;
						}
						else
						{
							((sc_canplace)(object)((Component)placer).GetComponent(typeof(sc_canplace))).willcan = 0f;
							((sc_canplace)(object)((Component)placer).GetComponent(typeof(sc_canplace))).can = false;
							placer.position = val4 + Vector3.up * ((sc_fortification)(object)((Component)handitem).GetComponent(typeof(sc_fortification))).downintheground;
						}
					}
					if (((sc_canplace)(object)((Component)placer).GetComponent(typeof(sc_canplace))).can)
					{
						if (Input.GetButtonDown("Shoot"))
						{
							place_conter = 1f;
						}
					}
					else
					{
						place_conter = 0f;
					}
					if (!(place_conter <= 1.1f))
					{
						if (Network.isClient)
						{
							((Component)this).NetView().RPC("call_for_fort", (RPCMode)0, new object[4]
							{
								((sc_fortification)(object)((Component)handitem).GetComponent(typeof(sc_fortification))).fabid,
								myid,
								placer.position,
								placer.rotation
							});
						}
						else
						{
							call_for_fort(((sc_fortification)(object)((Component)handitem).GetComponent(typeof(sc_fortification))).fabid, myid, placer.position, placer.rotation);
						}
						Object.Destroy((Object)(object)((Component)placer).gameObject);
						ammo[itemselected]--;
						if (ammo[itemselected] <= 0)
						{
							item[itemselected] = -1;
							quant[itemselected] = 0;
							ammo[itemselected] = 0;
							Object.Destroy((Object)(object)((Component)handitem).gameObject);
							((Component)this).NetView().RPC("changeitem", (RPCMode)1, new object[1] { item[itemselected] });
							((Component)chest).GetComponent<Animation>().CrossFade("nogunidle", 0.2f);
							((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
							{
								"nogunidle",
								0.2f,
								((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["nogunidle"].speed
							});
						}
						place_conter = 0f;
					}
				}
			}
			if (item[itemselected] >= 0)
			{
				if (controled)
				{
					if (((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[itemselected]].tipo == 1 && Input.GetButtonDown("Aim"))
					{
						reloadact1 = false;
						armstate = 14;
						animationcont = 0f;
						((Component)chest).GetComponent<Animation>().CrossFade("push", 0.2f);
						((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
						{
							"push",
							0.2f,
							((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["push"].speed
						});
					}
					if (((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[itemselected]].tipo == 4)
					{
						if (Input.GetButton("Shoot"))
						{
							armstate = 15;
							animationcont = 0f;
							throw_charge = throw_charge_min;
							((Component)chest).GetComponent<Animation>().CrossFade("throwhold", 0.3f);
							((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
							{
								"throwhold",
								0.3f,
								((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["throwhold"].speed
							});
							reloadact1 = false;
						}
						if (Input.GetButtonDown("Aim"))
						{
							reloadact1 = false;
							armstate = 14;
							animationcont = 0f;
							((Component)chest).GetComponent<Animation>().CrossFade("push", 0.2f);
							((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
							{
								"push",
								0.2f,
								((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["push"].speed
							});
						}
					}
					if (((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[itemselected]].tipo == 2)
					{
						if (Input.GetButton("Shoot"))
						{
							armstate = 8;
							animationcont = 0f;
							num6 = 0;
							((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()[((sc_meelegun)(object)((Component)handitem).GetComponent(typeof(sc_meelegun))).atkanim[num6]].speed = ((sc_meelegun)(object)((Component)handitem).GetComponent(typeof(sc_meelegun))).atkspeed[num6];
							((Component)chest).GetComponent<Animation>().CrossFade(((sc_meelegun)(object)((Component)handitem).GetComponent(typeof(sc_meelegun))).atkanim[num6], 0.1f);
							((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
							{
								((sc_meelegun)(object)((Component)handitem).GetComponent(typeof(sc_meelegun))).atkanim[num6],
								0.1f,
								((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()[((sc_meelegun)(object)((Component)handitem).GetComponent(typeof(sc_meelegun))).atkanim[num6]].speed
							});
							reloadact1 = false;
						}
						if (Input.GetButtonDown("Aim"))
						{
							reloadact1 = false;
							armstate = 14;
							animationcont = 0f;
							((Component)chest).GetComponent<Animation>().CrossFade("push", 0.2f);
							((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
							{
								"push",
								0.2f,
								((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["push"].speed
							});
						}
					}
					if (((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[itemselected]].tipo == 3)
					{
						if (Input.GetButton("Shoot"))
						{
							armstate = 13;
							animationcont = 0f;
							if (!((sc_consumable)(object)((Component)handitem).GetComponent(typeof(sc_consumable))).rotarm)
							{
								armrottype = 1;
								((Component)this).NetView().RPC("setrot", (RPCMode)1, new object[1] { armrottype });
							}
							((Component)chest).GetComponent<Animation>().CrossFade(((sc_consumable)(object)((Component)handitem).GetComponent(typeof(sc_consumable))).consumeanim, 0.1f);
							((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
							{
								((sc_consumable)(object)((Component)handitem).GetComponent(typeof(sc_consumable))).consumeanim,
								0.1f,
								((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()[((sc_consumable)(object)((Component)handitem).GetComponent(typeof(sc_consumable))).consumeanim].speed
							});
							reloadact1 = false;
						}
						if (Input.GetButtonDown("Aim"))
						{
							reloadact1 = false;
							armstate = 14;
							animationcont = 0f;
							((Component)chest).GetComponent<Animation>().CrossFade("push", 0.2f);
							((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
							{
								"push",
								0.2f,
								((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["push"].speed
							});
						}
					}
				}
				if (((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[itemselected]].tipo == 0)
				{
					if (armstate == 0)
					{
						if (zooming)
						{
							if (((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).hidemesh && !(hideconter >= ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).timetohide))
							{
								hideconter += 10f * Time.deltaTime;
								if (!(hideconter < ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).timetohide))
								{
									for (num = 0; num < ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).meshtohide.Length; num++)
									{
										((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).meshtohide[num].enabled = false;
									}
									for (num = 0; num < handtohide.Length; num++)
									{
										handtohide[num].enabled = false;
									}
									showscope = true;
								}
							}
							if (!aiming)
							{
								if (((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).hidemesh)
								{
									for (num = 0; num < ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).meshtohide.Length; num++)
									{
										((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).meshtohide[num].enabled = true;
									}
								}
								for (num = 0; num < handtohide.Length; num++)
								{
									handtohide[num].enabled = true;
								}
								showscope = false;
								((sc_camcript)(object)((Component)cam).GetComponent(typeof(sc_camcript))).to_fov = def_fov;
								eyey = start_eyey;
								zooming = false;
								((Component)chest).GetComponent<Animation>().CrossFade(((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).idleanim, 0.2f);
								((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
								{
									((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).idleanim,
									0.2f,
									((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()[((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).idleanim].speed
								});
							}
						}
						else if (aiming)
						{
							hideconter = 0f;
							((sc_camcript)(object)((Component)cam).GetComponent(typeof(sc_camcript))).to_fov = (int)((float)def_fov - ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).add_fov);
							eyey = start_eyey + ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).add_ypos;
							zooming = true;
							((Component)chest).GetComponent<Animation>().CrossFade(((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).aimanim, 0.2f);
							((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
							{
								((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).aimanim,
								0.2f,
								((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()[((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).aimanim].speed
							});
						}
					}
					outpos.position = ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).output.position;
					val3 = (2099456);
					if (Physics.Raycast(backpos.position, outpos.position - backpos.position, out val2, Vector3.Distance(outpos.position, backpos.position) * 1.1f, (val3)))
					{
						armstate = 12;
						((Component)chest).GetComponent<Animation>().CrossFade("handsback", 0.2f);
						((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
						{
							"handsback",
							0.2f,
							((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["handsback"].speed
						});
					}
					flag = false;
					if (controled)
					{
						if (((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).auto && Input.GetButton("Shoot"))
						{
							flag = true;
						}
						if (!((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).auto && Input.GetButtonDown("Shoot"))
						{
							flag = true;
						}
					}
					if (flag && ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).RFS && quant[itemselected] > 0 && armstate == 0)
					{
						if (zooming)
						{
							((Component)chest).GetComponent<Animation>().Rewind(((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).zoomshotanim);
							((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()[((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).zoomshotanim].speed = ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).zoomshotanimspeed;
							((Component)chest).GetComponent<Animation>().Play(((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).zoomshotanim);
							((Component)this).NetView().RPC("playarmanim", (RPCMode)1, new object[2]
							{
								((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).zoomshotanim,
								((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()[((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).zoomshotanim].speed
							});
						}
						else
						{
							((Component)chest).GetComponent<Animation>().Rewind(((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).shotanim);
							((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()[((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).shotanim].speed = ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).shotanimspeed;
							((Component)chest).GetComponent<Animation>().Play(((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).shotanim);
							((Component)this).NetView().RPC("playarmanim", (RPCMode)1, new object[2]
							{
								((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).shotanim,
								((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()[((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).shotanim].speed
							});
						}
						if ((Object)(object)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).flyerpos != (Object)null)
						{
							val = (Transform)Object.Instantiate((Object)(object)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).flyngbullet_fab, ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).flyerpos.position, ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).flyerpos.rotation);
							((Component)val).GetComponent<Rigidbody>().AddForce(((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).flyerpos.forward * 300f / Time.timeScale);
							((Component)val).GetComponent<Rigidbody>().AddTorque(new Vector3((float)Random.Range(-100, 100), (float)Random.Range(-100, 100), 0f));
						}
						for (num = 0; num < ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).bullets; num++)
						{
							val = (Transform)Network.Instantiate((Object)(object)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).bulletprefab, ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).output.position, ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).output.rotation, 1);
							val.Rotate(Vector3.right * (float)Random.Range(0, 360));
							num4 = (zooming ? ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).spread : (((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).spread + ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).addspread));
							num4 *= spreadcoef;
							val.Rotate(Vector3.forward * Random.Range(0f - num4, num4));
							if ((Object)(object)(sc_shot)(object)((Component)val).GetComponent(typeof(sc_shot)) != (Object)null)
							{
								((sc_shot)(object)((Component)val).GetComponent(typeof(sc_shot))).canmove = true;
								((sc_shot)(object)((Component)val).GetComponent(typeof(sc_shot))).shooter = myid;
								((sc_shot)(object)((Component)val).GetComponent(typeof(sc_shot))).shooterobj = ((Component)this).transform;
								((Component)val).NetView().RPC("setcanmove", (RPCMode)1, new object[0]);
								((sc_shot)(object)((Component)val).GetComponent(typeof(sc_shot))).dano = ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).dano;
								((sc_shot)(object)((Component)val).GetComponent(typeof(sc_shot))).push = ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).pushforce;
								((sc_shot)(object)((Component)val).GetComponent(typeof(sc_shot))).speed = ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).force;
							}
							if ((Object)(object)(SC_MISSILE)(object)((Component)val).GetComponent(typeof(SC_MISSILE)) != (Object)null)
							{
								((SC_MISSILE)(object)((Component)val).GetComponent(typeof(SC_MISSILE))).canmove = true;
								((SC_MISSILE)(object)((Component)val).GetComponent(typeof(SC_MISSILE))).shooter = myid;
								((SC_MISSILE)(object)((Component)val).GetComponent(typeof(SC_MISSILE))).shooterobj = ((Component)this).transform;
								((Component)val).NetView().RPC("setcanmove", (RPCMode)1, new object[0]);
								((SC_MISSILE)(object)((Component)val).GetComponent(typeof(SC_MISSILE))).dano = ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).dano;
								((SC_MISSILE)(object)((Component)val).GetComponent(typeof(SC_MISSILE))).exp_range = ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).rangeofexp;
								((SC_MISSILE)(object)((Component)val).GetComponent(typeof(SC_MISSILE))).push = ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).pushforce;
								((SC_MISSILE)(object)((Component)val).GetComponent(typeof(SC_MISSILE))).speed = ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).force;
							}
						}
						barulho = ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).noisemaker * noisemultiplier;
						if ((Object)(object)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).muzleprefab != (Object)null)
						{
							val = (Transform)Network.Instantiate((Object)(object)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).muzleprefab, ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).output.position, ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).output.rotation, 7);
							val.parent = ((Component)this).transform;
						}
						if (((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).doflash)
						{
							((Behaviour)((Component)flash).GetComponent<Light>()).enabled = true;
							flashconter = 100f;
							((Component)this).NetView().RPC("activelight", (RPCMode)1, new object[0]);
						}
						((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).RFS = false;
						quant[itemselected]--;
						if (((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).hidemagonoutammo && quant[itemtoselect] <= 0 && (Object)(object)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).magmesh != (Object)null)
						{
							((Component)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).magmesh).GetComponent<Renderer>().enabled = false;
						}
						((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).conter = 0f;
						headang += ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).V_rec;
						((Component)this).transform.Rotate(Vector3.up * Random.Range(0f - ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).H_rec, ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).H_rec));
						if ((Object)(object)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).animatedmesh != (Object)null && ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).gunshotanim != "null")
						{
							((Component)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).animatedmesh).GetComponent<Animation>().Rewind(((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).gunshotanim);
							((Component)(Animation)((Component)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).animatedmesh).GetComponent(typeof(Animation))).GetComponent<Animation>()[((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).gunshotanim].speed = ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).gunshotanimspeed;
							((Component)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).animatedmesh).GetComponent<Animation>().Play(((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).gunshotanim);
						}
						if ((Object)(object)((Component)handitem).GetComponent<AudioSource>() != (Object)null)
						{
							((Component)handitem).GetComponent<AudioSource>().Play();
						}
						((Component)this).NetView().RPC("playitemsound", (RPCMode)1, new object[0]);
					}
					if (quant[itemselected] < ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).maxammo && ammo[itemselected] > 0 && Input.GetButton("Reload") && controled)
					{
						armstate = 4;
						animationcont = 0f;
						reloadact1 = false;
						reloadact2 = false;
						reloadact3 = false;
						reloadact4 = false;
						reloadact5 = false;
						((Component)chest).GetComponent<Animation>().CrossFade(((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).reloadgrabanim, 0.2f);
						((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
						{
							((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).reloadgrabanim,
							0.2f,
							((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()[((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).reloadgrabanim].speed
						});
						if ((Object)(object)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).animatedmesh != (Object)null && ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).reloadanimST != "null")
						{
							((Component)(Animation)((Component)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).animatedmesh).GetComponent(typeof(Animation))).GetComponent<Animation>()[((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).reloadanimST].speed = ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).reloadanimSTspeed;
							((Component)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).animatedmesh).GetComponent<Animation>().CrossFade(((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).reloadanimST, 0.05f);
						}
					}
					if (Input.GetButton("Shoot") && quant[itemselected] <= 0 && autoreload && ammo[itemselected] > 0)
					{
						armstate = 4;
						animationcont = 0f;
						reloadact1 = false;
						reloadact2 = false;
						reloadact3 = false;
						reloadact4 = false;
						reloadact5 = false;
						((Component)chest).GetComponent<Animation>().CrossFade(((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).reloadgrabanim, 0.2f);
						((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
						{
							((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).reloadgrabanim,
							0.2f,
							((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()[((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).reloadgrabanim].speed
						});
						if ((Object)(object)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).animatedmesh != (Object)null && ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).reloadanimST != "null")
						{
							((Component)(Animation)((Component)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).animatedmesh).GetComponent(typeof(Animation))).GetComponent<Animation>()[((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).reloadanimST].speed = ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).reloadanimSTspeed;
							((Component)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).animatedmesh).GetComponent<Animation>().CrossFade(((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).reloadanimST, 0.05f);
						}
					}
				}
			}
			else if (Input.GetButtonDown("Aim"))
			{
				reloadact1 = false;
				armstate = 14;
				animationcont = 0f;
				((Component)chest).GetComponent<Animation>().CrossFade("push", 0.2f);
				((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
				{
					"push",
					0.2f,
					((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["push"].speed
				});
			}
			if (itemtoselect != itemselected)
			{
				if (item[itemtoselect] >= 0 || item[itemselected] >= 0)
				{
					animationcont = 0f;
					armstate = 2;
					((Component)chest).GetComponent<Animation>().CrossFade("changeitem", 0.2f);
					((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
					{
						"changeitem",
						0.2f,
						((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["changeitem"].speed
					});
				}
				else if (item[itemtoselect] == -1 || item[itemselected] == -1)
				{
					itemselected = itemtoselect;
				}
			}
		}
		if (armstate != 0)
		{
			zooming = false;
			((sc_camcript)(object)((Component)cam).GetComponent(typeof(sc_camcript))).to_fov = def_fov;
			showscope = false;
			eyey = start_eyey;
			if (item[itemselected] >= 0 && ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[itemselected]].tipo == 0 && ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).hidemesh)
			{
				for (num = 0; num < ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).meshtohide.Length; num++)
				{
					((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).meshtohide[num].enabled = true;
				}
				for (num = 0; num < handtohide.Length; num++)
				{
					handtohide[num].enabled = true;
				}
			}
		}
		if (armstate == 14)
		{
			animationcont += 100f * pushspeed * Time.deltaTime;
			if (!reloadact1 && !(animationcont <= 30f))
			{
				val3 = (2048);
				if (Physics.Raycast(cam.position, cam.forward, out val2, pushrange, (val3)))
				{
					soundsources[2].clip = sounds[4];
					soundsources[2].Play();
					((Component)val2.collider).gameObject.NetView().RPC("pushme", (RPCMode)2, new object[0]);
					reloadact1 = true;
				}
			}
			if (!(animationcont <= 60f))
			{
				armstate = 11;
				animationcont = 0f;
				futurearmstate = 0;
				waitingtime = 250;
				if (item[itemselected] >= 0)
				{
					animateinoptions();
				}
				else
				{
					((Component)chest).GetComponent<Animation>().CrossFade("nogunidle", 0.2f);
					((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
					{
						"nogunidle",
						0.2f,
						((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["nogunidle"].speed
					});
				}
			}
		}
		if (armstate == 13)
		{
			animationcont += 100f * Time.deltaTime;
			if (!(animationcont <= (float)((sc_consumable)(object)((Component)handitem).GetComponent(typeof(sc_consumable))).consumetime))
			{
				armrottype = 0;
				((Component)this).NetView().RPC("setrot", (RPCMode)1, new object[1] { armrottype });
				Object.Destroy((Object)(object)((Component)handitem).gameObject);
				((Component)this).NetView().RPC("changeitem", (RPCMode)1, new object[1] { item[itemselected] });
				if (((sc_consumable)(object)((Component)handitem).GetComponent(typeof(sc_consumable))).stopbleeding)
				{
					bleeding = 0f;
					((Component)this).NetView().RPC("setbleeding", (RPCMode)6, new object[1] { false });
				}
				num2 = (int)((sc_consumable)(object)((Component)handitem).GetComponent(typeof(sc_consumable))).life;
				if (!(((sc_consumable)(object)((Component)handitem).GetComponent(typeof(sc_consumable))).life <= 0f))
				{
					the_message = "+" + num2;
				}
				if (((sc_consumable)(object)((Component)handitem).GetComponent(typeof(sc_consumable))).callsuply)
				{
					the_message = "S.O.S";
					if (Network.isClient)
					{
						((Component)myspawner).NetView().RPC("asksuplly", (RPCMode)0, new object[0]);
					}
					else
					{
						((sc_spawner)(object)((Component)myspawner).GetComponent(typeof(sc_spawner))).asksuplly();
					}
				}
				message_a = 1f;
				message_col = Color.red;
				((sc_life)(object)((Component)this).GetComponent(typeof(sc_life))).quantia = ((sc_life)(object)((Component)this).GetComponent(typeof(sc_life))).quantia + ((sc_consumable)(object)((Component)handitem).GetComponent(typeof(sc_consumable))).life;
				if (!(((sc_life)(object)((Component)this).GetComponent(typeof(sc_life))).quantia <= 100f))
				{
					((sc_life)(object)((Component)this).GetComponent(typeof(sc_life))).quantia = 100f;
				}
				ammo[itemselected]--;
				if (ammo[itemselected] > 0)
				{
					animationcont = 0f;
					armstate = 2;
					((Component)chest).GetComponent<Animation>().CrossFade("changeitem", 0.2f);
					((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
					{
						"changeitem",
						0.2f,
						((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["changeitem"].speed
					});
				}
				else
				{
					armstate = 0;
					item[itemselected] = -1;
					quant[itemselected] = 0;
					ammo[itemselected] = 0;
					Object.Destroy((Object)(object)((Component)handitem).gameObject);
					((Component)this).NetView().RPC("changeitem", (RPCMode)1, new object[1] { item[itemselected] });
					((Component)chest).GetComponent<Animation>().CrossFade("nogunidle", 0.2f);
					((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
					{
						"nogunidle",
						0.2f,
						((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["nogunidle"].speed
					});
				}
			}
		}
		if (armstate == 12)
		{
			val3 = (2099456);
			flag = Physics.Raycast(backpos.position, outpos.position - backpos.position, out val2, Vector3.Distance(outpos.position, backpos.position) * 1.1f, (val3));
			if (Input.GetButtonDown("Interact"))
			{
				reloadact1 = false;
				armstate = 6;
				animationcont = 0f;
				((Component)chest).GetComponent<Animation>().CrossFade("interact", 0.2f);
				((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
				{
					"interact",
					0.4f,
					((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["interact"].speed
				});
			}
			if ((Input.GetButtonDown("Shoot") || Input.GetButtonDown("Aim")) && flag && (Object)(object)(sc_zumbi)(object)((Component)val2.collider).gameObject.GetComponent(typeof(sc_zumbi)) != (Object)null)
			{
				reloadact1 = false;
				armstate = 14;
				animationcont = 0f;
				num2 = 0;
				if (item[itemselected] >= 0 && ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[itemselected]].tipo == 0 && ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).basher)
				{
					num2 = 1;
				}
				if (num2 != 1)
				{
					((Component)chest).GetComponent<Animation>().CrossFade("push", 0.2f);
					((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
					{
						"push",
						0.2f,
						((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["push"].speed
					});
				}
				else
				{
					((Component)chest).GetComponent<Animation>().CrossFade("gunbash", 0.2f);
					((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
					{
						"gunbash",
						0.2f,
						((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["gunbash"].speed
					});
				}
			}
			itemtoselect = (int)((float)itemtoselect - Input.GetAxis("MouseWheel"));
			if (itemtoselect < 0)
			{
				itemtoselect = 9;
			}
			if (itemtoselect > 9)
			{
				itemtoselect = 0;
			}
			if (Input.GetKey("1"))
			{
				itemtoselect = 0;
			}
			if (Input.GetKey("2"))
			{
				itemtoselect = 1;
			}
			if (Input.GetKey("3"))
			{
				itemtoselect = 2;
			}
			if (Input.GetKey("4"))
			{
				itemtoselect = 3;
			}
			if (Input.GetKey("5"))
			{
				itemtoselect = 4;
			}
			if (Input.GetKey("6"))
			{
				itemtoselect = 5;
			}
			if (Input.GetKey("7"))
			{
				itemtoselect = 6;
			}
			if (Input.GetKey("8"))
			{
				itemtoselect = 7;
			}
			if (Input.GetKey("9"))
			{
				itemtoselect = 8;
			}
			if (Input.GetKey("0"))
			{
				itemtoselect = 9;
			}
			if (itemtoselect != itemselected)
			{
				if (item[itemtoselect] >= 0 || item[itemselected] >= 0)
				{
					animationcont = 0f;
					armstate = 2;
					((Component)chest).GetComponent<Animation>().CrossFade("changeitem", 0.2f);
					((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
					{
						"changeitem",
						0.2f,
						((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["changeitem"].speed
					});
				}
				else if (item[itemtoselect] == -1 || item[itemselected] == -1)
				{
					itemselected = itemtoselect;
				}
			}
			if (!flag)
			{
				armstate = 11;
				animationcont = 0f;
				futurearmstate = 0;
				waitingtime = 250;
				if (item[itemselected] >= 0)
				{
					animateinoptions();
				}
				else
				{
					((Component)chest).GetComponent<Animation>().CrossFade("nogunidle", 0.2f);
					((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
					{
						"nogunidle",
						0.2f,
						((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["nogunidle"].speed
					});
				}
			}
		}
		if (armstate == 8)
		{
			animationcont += 100f * Time.deltaTime;
			if (!reloadact1 && !(animationcont <= (float)((sc_meelegun)(object)((Component)handitem).GetComponent(typeof(sc_meelegun))).endtime[num6] * ((sc_meelegun)(object)((Component)handitem).GetComponent(typeof(sc_meelegun))).hittime[num6]))
			{
				val3 = (2098432);
				if (Physics.Raycast(cam.position, cam.forward, out val2, ((sc_meelegun)(object)((Component)handitem).GetComponent(typeof(sc_meelegun))).range, (val3)))
				{
					reloadact1 = true;
					if (((Component)val2.collider).gameObject.layer == 8)
					{
						soundsources[2].clip = sounds[6];
						soundsources[2].Play();
					}
					if ((Object)(object)(sc_dmgtaker)(object)((Component)val2.collider).gameObject.GetComponent(typeof(sc_dmgtaker)) != (Object)null)
					{
						if ((Object)(object)(sc_zumbi)(object)((Component)((sc_dmgtaker)(object)((Component)val2.collider).gameObject.GetComponent(typeof(sc_dmgtaker))).owner).GetComponent(typeof(sc_zumbi)) != (Object)null)
						{
							if (((sc_zumbi)(object)((Component)((sc_dmgtaker)(object)((Component)val2.collider).gameObject.GetComponent(typeof(sc_dmgtaker))).owner).GetComponent(typeof(sc_zumbi))).state != -1)
							{
								if (((sc_meelegun)(object)((Component)handitem).GetComponent(typeof(sc_meelegun))).blunt)
								{
									soundsources[2].clip = sounds[4];
									soundsources[2].Play();
								}
								else
								{
									soundsources[2].clip = sounds[5];
									soundsources[2].Play();
								}
								num4 = ((sc_meelegun)(object)((Component)handitem).GetComponent(typeof(sc_meelegun))).dmg + Random.Range(0, ((sc_meelegun)(object)((Component)handitem).GetComponent(typeof(sc_meelegun))).dmgrnd);
								((Component)((sc_dmgtaker)(object)((Component)val2.collider).gameObject.GetComponent(typeof(sc_dmgtaker))).owner).NetView().RPC("setkillerid", (RPCMode)2, new object[1] { myid });
								((sc_life)(object)((Component)((sc_dmgtaker)(object)((Component)val2.collider).gameObject.GetComponent(typeof(sc_dmgtaker))).owner).GetComponent(typeof(sc_life))).hurtme(num4);
								if (((sc_meelegun)(object)((Component)handitem).GetComponent(typeof(sc_meelegun))).blunt)
								{
									((Component)((sc_dmgtaker)(object)((Component)val2.collider).gameObject.GetComponent(typeof(sc_dmgtaker))).owner).NetView().RPC("pushme", (RPCMode)2, new object[0]);
								}
								((Component)((sc_dmgtaker)(object)((Component)val2.collider).gameObject.GetComponent(typeof(sc_dmgtaker))).owner).NetView().RPC("hurtme", (RPCMode)1, new object[1] { num4 });
								((Component)((sc_dmgtaker)(object)((Component)val2.collider).gameObject.GetComponent(typeof(sc_dmgtaker))).owner).NetView().RPC("forcar", (RPCMode)2, new object[1] { ((Component)this).transform.TransformDirection(movimento) * 5f * (float)(((sc_meelegun)(object)((Component)handitem).GetComponent(typeof(sc_meelegun))).dmg + Random.Range(0, ((sc_meelegun)(object)((Component)handitem).GetComponent(typeof(sc_meelegun))).dmgrnd)) });
							}
						}
						else
						{
							num4 = ((sc_meelegun)(object)((Component)handitem).GetComponent(typeof(sc_meelegun))).dmg + Random.Range(0, ((sc_meelegun)(object)((Component)handitem).GetComponent(typeof(sc_meelegun))).dmgrnd);
							((Component)((sc_dmgtaker)(object)((Component)val2.collider).gameObject.GetComponent(typeof(sc_dmgtaker))).owner).NetView().RPC("hurtme", (RPCMode)1, new object[1] { num4 });
							((sc_life)(object)((Component)((sc_dmgtaker)(object)((Component)val2.collider).gameObject.GetComponent(typeof(sc_dmgtaker))).owner).GetComponent(typeof(sc_life))).hurtme(num4);
						}
					}
					if ((Object)(object)(sc_particula)(object)((Component)val2.collider).gameObject.GetComponent(typeof(sc_particula)) != (Object)null)
					{
						Network.Instantiate((Object)(object)((sc_particula)(object)((Component)val2.collider).gameObject.GetComponent(typeof(sc_particula))).prefab, val2.point, Quaternion.LookRotation(val2.normal), 3);
						if (((Object)((Component)((sc_particula)(object)((Component)val2.collider).gameObject.GetComponent(typeof(sc_particula))).prefab).gameObject).name == "spark")
						{
							soundsources[2].clip = sounds[7];
							soundsources[2].Play();
						}
					}
					else
					{
						Network.Instantiate((Object)(object)std_particle, val2.point, Quaternion.LookRotation(val2.normal), 3);
					}
				}
			}
			if (!(animationcont <= (float)((sc_meelegun)(object)((Component)handitem).GetComponent(typeof(sc_meelegun))).endtime[num6]))
			{
				armstate = 9;
				animationcont = 0f;
				((Component)chest).GetComponent<Animation>().CrossFade(((sc_meelegun)(object)((Component)handitem).GetComponent(typeof(sc_meelegun))).holdanim, 0.2f);
				((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
				{
					((sc_meelegun)(object)((Component)handitem).GetComponent(typeof(sc_meelegun))).holdanim,
					0.2f,
					((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()[((sc_meelegun)(object)((Component)handitem).GetComponent(typeof(sc_meelegun))).holdanim].speed
				});
			}
		}
		if (armstate == 9)
		{
			animationcont += 1000f * Time.deltaTime;
			if (!(animationcont <= 210f))
			{
				armstate = 0;
				animationcont = 0f;
			}
		}
		if (armstate == 11)
		{
			animationcont += 1000f * Time.deltaTime;
			if (!(animationcont <= (float)waitingtime))
			{
				armstate = futurearmstate;
				animationcont = 0f;
			}
		}
		if (armstate == 10)
		{
			rpcbar -= 150f * Time.deltaTime;
			if (Input.GetButtonDown("Shoot"))
			{
				rpcbar += 45f;
			}
			if (!(rpcbar >= 0f))
			{
				rpcbar = 0f;
			}
			flag = false;
			if (!(rpcbar <= 500f))
			{
				rpcconter = 0f;
				rpcbar = 500f;
				((Component)revivetarget).gameObject.NetView().RPC("revive", (RPCMode)1, new object[0]);
				((Component)myspawner).NetView().RPC("addmoney", (RPCMode)2, new object[1] { 50 });
				flag = true;
			}
			if (Input.GetButtonDown("Interact") || flag)
			{
				if (!flag)
				{
					((Component)revivetarget).gameObject.NetView().RPC("setreviving", (RPCMode)1, new object[1] { false });
				}
				legstate = 1;
				((Component)legs).GetComponent<Animation>().CrossFade("standing", 0.2f);
				((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
				{
					"standing",
					0.2f,
					((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["standing"].speed
				});
				armstate = 11;
				animationcont = 0f;
				futurearmstate = 0;
				waitingtime = 250;
				if (item[itemselected] >= 0)
				{
					animateinoptions();
				}
				else
				{
					((Component)chest).GetComponent<Animation>().CrossFade("nogunidle", 0.2f);
					((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
					{
						"nogunidle",
						0.2f,
						((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["nogunidle"].speed
					});
				}
				armrottype = 0;
				((Component)this).NetView().RPC("setrot", (RPCMode)1, new object[1] { armrottype });
				canspin = true;
			}
		}
		if (armstate == 6)
		{
			animationcont += 1000f * Time.deltaTime;
			if (!(animationcont <= 300f) && !reloadact1)
			{
				val3 = (149979392);
				if (Physics.Raycast(cam.position, cam.forward, out val2, userange, (val3)))
				{
					if (((Component)val2.collider).gameObject.layer == 23)
					{
						((Component)val2.collider).gameObject.NetView().RPC("openbag", (RPCMode)2, new object[0]);
						reloadact1 = true;
					}
					if (((Component)val2.collider).gameObject.layer == 27)
					{
						if (!(((Component)val2.collider).gameObject.transform.InverseTransformPoint(((Component)this).transform.position).z >= 0f))
						{
							if (!(val2.distance >= userange / 2f))
							{
								float y17 = ((Component)this).transform.position.y;
								float y18 = ((Component)val2.collider).gameObject.transform.position.y;
								Bounds bounds9 = val2.collider.bounds;
								if (!(y17 <= y18 - bounds9.size.y / 2f + 0.7f))
								{
									float y19 = ((Component)this).transform.position.y;
									float y20 = ((Component)val2.collider).gameObject.transform.position.y;
									Bounds bounds10 = val2.collider.bounds;
									if (!(y19 >= y20 + bounds10.size.y / 2f - 1f))
									{
										interact_obj = ((Component)val2.collider).gameObject;
										legstate = 8;
										canmoveinladder = true;
										((Component)legs).GetComponent<Animation>().CrossFade("hold_ladder", 0.3f);
										((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
										{
											"hold_ladder",
											0.3f,
											((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["hold_ladder"].speed
										});
										armstate = 17;
										((Component)chest).GetComponent<Animation>().CrossFade("hold_ladder", 0.3f);
										((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
										{
											"hold_ladder",
											0.3f,
											((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["hold_ladder"].speed
										});
										armrottype = 1;
										((Component)this).NetView().RPC("setrot", (RPCMode)1, new object[1] { armrottype });
										canspin = false;
									}
								}
							}
						}
						else
						{
							float y21 = ((Component)this).transform.position.y;
							float y22 = ((Component)val2.collider).gameObject.transform.position.y;
							Bounds bounds11 = val2.collider.bounds;
							if (!(y21 <= y22 + bounds11.size.y / 2f))
							{
								interact_obj = ((Component)val2.collider).gameObject;
								legstate = 8;
								canmoveinladder = false;
								((Component)legs).GetComponent<Animation>().CrossFade("hold_ladder", 0.3f);
								((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
								{
									"hold_ladder",
									0.3f,
									((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["hold_ladder"].speed
								});
								armstate = 17;
								((Component)chest).GetComponent<Animation>().CrossFade("hold_ladder", 0.3f);
								((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
								{
									"hold_ladder",
									0.3f,
									((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["hold_ladder"].speed
								});
								armrottype = 1;
								((Component)this).NetView().RPC("setrot", (RPCMode)1, new object[1] { armrottype });
								canspin = false;
							}
						}
					}
					if (((Component)val2.collider).gameObject.layer == 22)
					{
						if (item[itemselected] >= 0)
						{
							if (((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[itemselected]].tipo == 0)
							{
								itemtoload = itemselected;
								if (Network.isClient)
								{
									((Component)val2.collider).gameObject.NetView().RPC("grabammocrate", (RPCMode)0, new object[1] { myid });
								}
								else
								{
									((sc_ammocrate)(object)((Component)val2.collider).gameObject.GetComponent(typeof(sc_ammocrate))).grabammocrate(myid);
								}
								reloadact1 = true;
								soundsources[0].clip = sounds[1];
								soundsources[0].Play();
								((Component)this).NetView().RPC("playasound", (RPCMode)1, new object[2] { 1, 0 });
							}
							else
							{
								itemtoload = -1;
								for (num = 0; num < item.Length; num++)
								{
									if (item[num] >= 0 && ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[num]].tipo == 0)
									{
										itemtoload = num;
										num = item.Length;
									}
								}
								if (itemtoload >= 0)
								{
									if (Network.isClient)
									{
										((Component)val2.collider).gameObject.NetView().RPC("grabammocrate", (RPCMode)0, new object[1] { myid });
									}
									else
									{
										((sc_ammocrate)(object)((Component)val2.collider).gameObject.GetComponent(typeof(sc_ammocrate))).grabammocrate(myid);
									}
									reloadact1 = true;
									soundsources[0].clip = sounds[1];
									soundsources[0].Play();
									((Component)this).NetView().RPC("playasound", (RPCMode)1, new object[2] { 1, 0 });
								}
							}
						}
						else
						{
							itemtoload = -1;
							for (num = 0; num < item.Length; num++)
							{
								if (item[num] >= 0 && ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[num]].tipo == 0)
								{
									itemtoload = num;
									num = item.Length;
								}
							}
							if (itemtoload >= 0)
							{
								if (Network.isClient)
								{
									((Component)val2.collider).gameObject.NetView().RPC("grabammocrate", (RPCMode)0, new object[1] { myid });
								}
								else
								{
									((sc_ammocrate)(object)((Component)val2.collider).gameObject.GetComponent(typeof(sc_ammocrate))).grabammocrate(myid);
								}
								reloadact1 = true;
								soundsources[0].clip = sounds[1];
								soundsources[0].Play();
								((Component)this).NetView().RPC("playasound", (RPCMode)1, new object[2] { 1, 0 });
							}
						}
					}
					if (((Component)val2.collider).gameObject.layer == 20 && !((sc_deadplayer)(object)((Component)((sc_deadbodyparts)(object)((Component)val2.collider).gameObject.GetComponent(typeof(sc_deadbodyparts))).owner).GetComponent(typeof(sc_deadplayer))).died)
					{
						revivetarget = ((sc_deadbodyparts)(object)((Component)val2.collider).gameObject.GetComponent(typeof(sc_deadbodyparts))).owner;
						if (!((sc_deadplayer)(object)((Component)revivetarget).gameObject.GetComponent(typeof(sc_deadplayer))).reviving)
						{
							((Component)revivetarget).gameObject.NetView().RPC("setreviving", (RPCMode)1, new object[1] { true });
							legstate = 7;
							((Component)legs).GetComponent<Animation>().CrossFade("cprleg", 0.3f);
							((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
							{
								"cprleg",
								0.3f,
								((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["cprleg"].speed
							});
							armstate = 10;
							((Component)chest).GetComponent<Animation>().CrossFade("cprhand", 0.3f);
							((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
							{
								"cprhand",
								0.3f,
								((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["cprhand"].speed
							});
							armrottype = 1;
							((Component)this).NetView().RPC("setrot", (RPCMode)1, new object[1] { armrottype });
							canspin = false;
							reloadact1 = true;
							rpcbar = 0f;
							rpcconter = 0f;
						}
					}
					if (((Component)val2.collider).gameObject.layer == 15)
					{
						if ((Object)(object)(sc_ammo)(object)((Component)val2.collider).gameObject.GetComponent(typeof(sc_ammo)) == (Object)null)
						{
							flag = false;
							for (num = 0; num < item.Length; num++)
							{
								if (item[num] < 0)
								{
									flag = true;
									num = item.Length;
								}
							}
							if (flag)
							{
								if (Network.isClient)
								{
									((Component)val2.collider).gameObject.NetView().RPC("grabitem", (RPCMode)0, new object[1] { myid });
								}
								else
								{
									((sc_canbetaken)(object)((Component)val2.collider).gameObject.GetComponent(typeof(sc_canbetaken))).grabitem(myid);
								}
								soundsources[0].clip = sounds[1];
								soundsources[0].Play();
								((Component)this).NetView().RPC("playasound", (RPCMode)1, new object[2] { 1, 0 });
							}
						}
						else
						{
							flag = false;
							for (num = 0; num < item.Length; num++)
							{
								if (item[num] == ((sc_ammo)(object)((Component)val2.collider).gameObject.GetComponent(typeof(sc_ammo))).gunid)
								{
									flag = true;
									num = item.Length;
								}
							}
							if (flag)
							{
								if (Network.isClient)
								{
									((Component)val2.collider).gameObject.NetView().RPC("grabammo", (RPCMode)0, new object[1] { myid });
								}
								else
								{
									((sc_ammo)(object)((Component)val2.collider).gameObject.GetComponent(typeof(sc_ammo))).grabammo(myid);
								}
								soundsources[0].clip = sounds[1];
								soundsources[0].Play();
								((Component)this).NetView().RPC("playasound", (RPCMode)1, new object[2] { 1, 0 });
							}
							else
							{
								flag2 = false;
								for (num = 0; num < item.Length; num++)
								{
									if (item[num] < 0)
									{
										flag2 = true;
										num = item.Length;
									}
								}
								if (flag2)
								{
									if (Network.isClient)
									{
										((Component)val2.collider).gameObject.NetView().RPC("grabitem", (RPCMode)0, new object[1] { myid });
									}
									else
									{
										((sc_canbetaken)(object)((Component)val2.collider).gameObject.GetComponent(typeof(sc_canbetaken))).grabitem(myid);
									}
									soundsources[0].clip = sounds[1];
									soundsources[0].Play();
									((Component)this).NetView().RPC("playasound", (RPCMode)1, new object[2] { 1, 0 });
								}
							}
						}
						reloadact1 = true;
					}
				}
			}
			if (!(animationcont <= 500f))
			{
				armstate = 7;
				animationcont = 0f;
				if (item[itemselected] >= 0)
				{
					animateinoptions();
				}
				else
				{
					((Component)chest).GetComponent<Animation>().CrossFade("nogunidle", 0.2f);
					((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
					{
						"nogunidle",
						0.2f,
						((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["nogunidle"].speed
					});
				}
			}
		}
		if (armstate == 7)
		{
			animationcont += 1000f * Time.deltaTime;
			if (!(animationcont <= 200f))
			{
				armstate = 0;
				animationcont = 0f;
			}
		}
		if (armstate == 4)
		{
			animationcont += 100f * Time.deltaTime;
			if ((Object)(object)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).magmesh != (Object)null)
			{
				if (!reloadact1 && !(animationcont <= (float)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).releasemoment))
				{
					((Component)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).magmesh).GetComponent<Renderer>().enabled = false;
				}
				if (!reloadact4 && !(animationcont <= (float)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).releasemoment))
				{
					if ((Object)(object)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).clipoutsound != (Object)null)
					{
						Network.Instantiate((Object)(object)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).clipoutsound, handitem.position, handitem.rotation, 6);
					}
					reloadact4 = true;
				}
				if (!reloadact2 && !(animationcont <= (float)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).grabmoment))
				{
					if ((Object)(object)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).magprefab != (Object)null)
					{
						lhandmag = (Transform)Object.Instantiate((Object)(object)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).magprefab);
						lhandmag.parent = lhandpos;
						lhandmag.localPosition = Vector3.zero;
						lhandmag.localEulerAngles = Vector3.zero;
					}
					reloadact2 = true;
				}
			}
			if ((Object)(object)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).magprefab != (Object)null && !reloadact2 && !(animationcont <= (float)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).grabmoment))
			{
				if ((Object)(object)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).magprefab != (Object)null)
				{
					lhandmag = (Transform)Object.Instantiate((Object)(object)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).magprefab);
					lhandmag.parent = lhandpos;
					lhandmag.localPosition = Vector3.zero;
					lhandmag.localEulerAngles = Vector3.zero;
				}
				reloadact2 = true;
			}
			if (!(animationcont <= (float)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).grabtime))
			{
				reloadact1 = false;
				reloadact2 = false;
				armstate = 5;
				animationcont = 0f;
				((Component)chest).GetComponent<Animation>().CrossFade(((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).reloadputanim, 0.2f);
				((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
				{
					((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).reloadputanim,
					0.2f,
					((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()[((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).reloadputanim].speed
				});
			}
		}
		if (armstate == 5)
		{
			animationcont += 100f * Time.deltaTime;
			if ((Object)(object)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).magmesh != (Object)null && !(animationcont >= (float)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).putmoment))
			{
				((Component)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).magmesh).GetComponent<Renderer>().enabled = false;
			}
			if (!reloadact1 && !(animationcont <= (float)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).putmoment))
			{
				if ((Object)(object)lhandmag != (Object)null)
				{
					Object.Destroy((Object)(object)((Component)lhandmag).gameObject);
				}
				if ((Object)(object)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).magmesh != (Object)null)
				{
					((Component)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).magmesh).GetComponent<Renderer>().enabled = true;
				}
				if ((Object)(object)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).clipinsound != (Object)null)
				{
					Network.Instantiate((Object)(object)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).clipinsound, handitem.position, handitem.rotation, 6);
				}
				reloadact1 = true;
			}
			if (!reloadact3 && !(animationcont <= (float)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).reloadendtime))
			{
				if ((Object)(object)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).animatedmesh != (Object)null && ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).reloadanimEND != "null")
				{
					((Component)(Animation)((Component)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).animatedmesh).GetComponent(typeof(Animation))).GetComponent<Animation>()[((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).reloadanimEND].speed = ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).reloadanimENDspeed;
					((Component)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).animatedmesh).GetComponent<Animation>().CrossFade(((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).reloadanimEND, 0.05f);
				}
				reloadact3 = true;
			}
			if (!(animationcont <= (float)((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).puttime))
			{
				reloadact1 = false;
				reloadact2 = false;
				armstate = 0;
				animationcont = 0f;
				((Component)chest).GetComponent<Animation>().CrossFade(((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).idleanim, 0.2f);
				((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
				{
					((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).idleanim,
					0.2f,
					((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()[((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).idleanim].speed
				});
				if (ammo[itemselected] >= ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).reloadamount)
				{
					ammo[itemselected] -= ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).reloadamount;
					quant[itemselected] += ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).reloadamount;
				}
				else
				{
					quant[itemselected] += ammo[itemselected];
					ammo[itemselected] = 0;
				}
				if (quant[itemselected] > ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).maxammo)
				{
					ammo[itemselected] += quant[itemselected] - ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).maxammo;
					quant[itemselected] = ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).maxammo;
				}
			}
		}
		if (armstate == 1)
		{
		}
		if (armstate == 2)
		{
			if (!(animationcont <= 0f))
			{
				itemtoselect = (int)((float)itemtoselect - Input.GetAxis("MouseWheel"));
				if (itemtoselect < 0)
				{
					itemtoselect = 9;
				}
				if (itemtoselect > 9)
				{
					itemtoselect = 0;
				}
				if (Input.GetKey("1"))
				{
					itemtoselect = 0;
				}
				if (Input.GetKey("2"))
				{
					itemtoselect = 1;
				}
				if (Input.GetKey("3"))
				{
					itemtoselect = 2;
				}
				if (Input.GetKey("4"))
				{
					itemtoselect = 3;
				}
				if (Input.GetKey("5"))
				{
					itemtoselect = 4;
				}
				if (Input.GetKey("6"))
				{
					itemtoselect = 5;
				}
				if (Input.GetKey("7"))
				{
					itemtoselect = 6;
				}
				if (Input.GetKey("8"))
				{
					itemtoselect = 7;
				}
				if (Input.GetKey("9"))
				{
					itemtoselect = 8;
				}
				if (Input.GetKey("0"))
				{
					itemtoselect = 9;
				}
			}
			animationcont += 1000f * Time.deltaTime;
			if (!(animationcont <= 220f))
			{
				armstate = 3;
				animationcont = 0f;
				itemselected = itemtoselect;
				if ((Object)(object)handitem != (Object)null)
				{
					Object.Destroy((Object)(object)((Component)handitem).gameObject);
				}
				generatehanditem();
			}
		}
		if (armstate == 3)
		{
			if (!(animationcont <= 0f))
			{
				itemtoselect = (int)((float)itemtoselect - Input.GetAxis("MouseWheel"));
				if (itemtoselect < 0)
				{
					itemtoselect = 9;
				}
				if (itemtoselect > 9)
				{
					itemtoselect = 0;
				}
				if (Input.GetKey("1"))
				{
					itemtoselect = 0;
				}
				if (Input.GetKey("2"))
				{
					itemtoselect = 1;
				}
				if (Input.GetKey("3"))
				{
					itemtoselect = 2;
				}
				if (Input.GetKey("4"))
				{
					itemtoselect = 3;
				}
				if (Input.GetKey("5"))
				{
					itemtoselect = 4;
				}
				if (Input.GetKey("6"))
				{
					itemtoselect = 5;
				}
				if (Input.GetKey("7"))
				{
					itemtoselect = 6;
				}
				if (Input.GetKey("8"))
				{
					itemtoselect = 7;
				}
				if (Input.GetKey("9"))
				{
					itemtoselect = 8;
				}
				if (Input.GetKey("0"))
				{
					itemtoselect = 9;
				}
			}
			animationcont += 1000f * Time.deltaTime;
			if (!(animationcont <= 220f))
			{
				armstate = 0;
			}
		}
		if (controled)
		{
			if (zooming)
			{
				float x2 = cam.localPosition.x + Input.GetAxis("Mouse X") * Time.deltaTime * camxmax * 0.4f;
				Vector3 localPosition2 = cam.localPosition;
				float num7 = (localPosition2.x = x2);
				Vector3 val7 = (cam.localPosition = localPosition2);
			}
			else
			{
				float x3 = cam.localPosition.x + Input.GetAxis("Mouse X") * Time.deltaTime * camxmax;
				Vector3 localPosition3 = cam.localPosition;
				float num8 = (localPosition3.x = x3);
				Vector3 val9 = (cam.localPosition = localPosition3);
			}
			if (!(cam.localPosition.x <= camxmax))
			{
				float x4 = camxmax;
				Vector3 localPosition4 = cam.localPosition;
				float num9 = (localPosition4.x = x4);
				Vector3 val11 = (cam.localPosition = localPosition4);
			}
			if (!(cam.localPosition.x >= 0f - camxmax))
			{
				float x5 = 0f - camxmax;
				Vector3 localPosition5 = cam.localPosition;
				float num10 = (localPosition5.x = x5);
				Vector3 val13 = (cam.localPosition = localPosition5);
			}
			if (zooming)
			{
				float y23 = cam.localPosition.y + Input.GetAxis("Mouse Y") * Time.deltaTime * camymax * 2f * 0.4f;
				Vector3 localPosition6 = cam.localPosition;
				float num11 = (localPosition6.y = y23);
				Vector3 val15 = (cam.localPosition = localPosition6);
			}
			else
			{
				float y24 = cam.localPosition.y + Input.GetAxis("Mouse Y") * Time.deltaTime * camymax * 2f;
				Vector3 localPosition7 = cam.localPosition;
				float num12 = (localPosition7.y = y24);
				Vector3 val17 = (cam.localPosition = localPosition7);
			}
			if (!(cam.localPosition.y <= camymax))
			{
				float y25 = camymax;
				Vector3 localPosition8 = cam.localPosition;
				float num13 = (localPosition8.y = y25);
				Vector3 val19 = (cam.localPosition = localPosition8);
			}
			if (!(cam.localPosition.y >= 0f - camymax))
			{
				float y26 = 0f - camymax;
				Vector3 localPosition9 = cam.localPosition;
				float num14 = (localPosition9.y = y26);
				Vector3 val21 = (cam.localPosition = localPosition9);
			}
		}
		if (!(cam.localPosition.x <= 0f))
		{
			float x6 = cam.localPosition.x - Time.deltaTime * camxmax;
			Vector3 localPosition10 = cam.localPosition;
			float num15 = (localPosition10.x = x6);
			Vector3 val23 = (cam.localPosition = localPosition10);
			if (!(cam.localPosition.x >= 0f))
			{
				int num16 = 0;
				Vector3 localPosition11 = cam.localPosition;
				float num17 = (localPosition11.x = num16);
				Vector3 val25 = (cam.localPosition = localPosition11);
			}
		}
		if (!(cam.localPosition.x >= 0f))
		{
			float x7 = cam.localPosition.x + Time.deltaTime * camxmax;
			Vector3 localPosition12 = cam.localPosition;
			float num18 = (localPosition12.x = x7);
			Vector3 val27 = (cam.localPosition = localPosition12);
			if (!(cam.localPosition.x <= 0f))
			{
				int num19 = 0;
				Vector3 localPosition13 = cam.localPosition;
				float num20 = (localPosition13.x = num19);
				Vector3 val29 = (cam.localPosition = localPosition13);
			}
		}
		if (!(cam.localPosition.y <= 0f))
		{
			float y27 = cam.localPosition.y - Time.deltaTime * camymax;
			Vector3 localPosition14 = cam.localPosition;
			float num21 = (localPosition14.y = y27);
			Vector3 val31 = (cam.localPosition = localPosition14);
			if (!(cam.localPosition.y >= 0f))
			{
				int num22 = 0;
				Vector3 localPosition15 = cam.localPosition;
				float num23 = (localPosition15.y = num22);
				Vector3 val33 = (cam.localPosition = localPosition15);
			}
		}
		if (!(cam.localPosition.y >= 0f))
		{
			float y28 = cam.localPosition.y + Time.deltaTime * camymax;
			Vector3 localPosition16 = cam.localPosition;
			float num24 = (localPosition16.y = y28);
			Vector3 val35 = (cam.localPosition = localPosition16);
			if (!(cam.localPosition.y <= 0f))
			{
				int num25 = 0;
				Vector3 localPosition17 = cam.localPosition;
				float num26 = (localPosition17.y = num25);
				Vector3 val37 = (cam.localPosition = localPosition17);
			}
		}
		movimento.y = vy;
		if (movimento.x != 0f && movimento.z != 0f)
		{
			movimento.z *= 0.707f;
			movimento.x *= 0.707f;
		}
		movimento = ((Component)this).transform.TransformDirection(movimento);
		if (legstate != 0)
		{
			movimento.y = 0f;
			if (Physics.Raycast(((Component)this).transform.position, Vector3.down, out val2, 1.35f, (val3)) && !(1.16f - val2.distance >= 0f))
			{
				movimento.y = 1.16f - val2.distance;
			}
		}
		((Component)this).GetComponent<Rigidbody>().velocity = movimento;
		if (senddata)
		{
			dataconter += 1000f * sendrate * Time.deltaTime;
			if (!(dataconter <= 1000f))
			{
				num2 = (int)((Component)this).transform.eulerAngles.y;
				num3 = (int)headang;
				((Component)this).NetView().RPC("setposstuff", (RPCMode)1, new object[6]
				{
					num2,
					num3,
					((Component)this).transform.position,
					exposi,
					exposiback,
					bending
				});
				dataconter = 0f;
			}
		}
		if (!(((sc_life)(object)((Component)this).GetComponent(typeof(sc_life))).quantia > 0f))
		{
			((sc_life)(object)((Component)this).GetComponent(typeof(sc_life))).quantia = 0f;
			flag = true;
			if (((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).gamestate == 3 && !((sc_matchcontroller)(object)((Component)((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).matchcontroller).GetComponent(typeof(sc_matchcontroller))).gamestarted)
			{
				flag = false;
			}
			if (flag)
			{
				if ((Object)(object)revivetarget != (Object)null)
				{
					((Component)revivetarget).gameObject.NetView().RPC("setreviving", (RPCMode)1, new object[1] { false });
				}
				val = (Transform)Network.Instantiate((Object)(object)deadbody, ((Component)this).transform.position, ((Component)this).transform.rotation, 0);
				((sc_deadplayer)(object)((Component)val).GetComponent(typeof(sc_deadplayer))).beardnum = beardnum;
				((sc_deadplayer)(object)((Component)val).GetComponent(typeof(sc_deadplayer))).beardcolor = beardcolor;
				((sc_deadplayer)(object)((Component)val).GetComponent(typeof(sc_deadplayer))).shirtcolor = shirtcolor;
				((sc_deadplayer)(object)((Component)val).GetComponent(typeof(sc_deadplayer))).myskin = myskin;
				((sc_deadplayer)(object)((Component)val).GetComponent(typeof(sc_deadplayer))).dead_sync_aparence(-1, myskin, beardcolor, shirtcolor);
				if (Network.isClient)
				{
					((Component)myspawner).NetView().RPC("sendchat", (RPCMode)0, new object[2]
					{
						playername + " is dying!",
						2
					});
				}
				else
				{
					((sc_spawner)(object)((Component)myspawner).GetComponent(typeof(sc_spawner))).sendchat(playername + " is dying!", 2);
				}
				for (num = 0; num < 10; num++)
				{
					((sc_deadplayer)(object)((Component)val).GetComponent(typeof(sc_deadplayer))).invent[num] = item[num];
					((sc_deadplayer)(object)((Component)val).GetComponent(typeof(sc_deadplayer))).quant[num] = quant[num];
					((sc_deadplayer)(object)((Component)val).GetComponent(typeof(sc_deadplayer))).ammo[num] = ammo[num];
				}
				((Component)val).gameObject.NetView().RPC("senddeadname", (RPCMode)1, new object[1] { playername });
				((Behaviour)(sc_onlinedeadplayer)(object)((Component)val).GetComponent(typeof(sc_onlinedeadplayer))).enabled = false;
				((sc_spawner)(object)((Component)myspawner).GetComponent(typeof(sc_spawner))).playerobj = val;
				((sc_deadplayer)(object)((Component)val).GetComponent(typeof(sc_deadplayer))).myspawner = myspawner;
				((sc_deadplayer)(object)((Component)val).GetComponent(typeof(sc_deadplayer))).lifes = lifes - 1;
				if (lifes <= 0)
				{
					((sc_deadplayer)(object)((Component)val).GetComponent(typeof(sc_deadplayer))).state = 1;
					((sc_deadplayer)(object)((Component)val).GetComponent(typeof(sc_deadplayer))).died = true;
					((Component)val).NetView().RPC("setdied", (RPCMode)6, new object[0]);
					for (num = 0; num < 10; num++)
					{
						val4 = ((Component)this).transform.position;
						val4.x += Random.Range(-1f, 1f);
						val4.z += Random.Range(-1f, 1f);
						val4.y += Random.Range(-0.5f, 0.5f);
						if (Network.isClient)
						{
							((Component)myspawner).NetView().RPC("dropitem", (RPCMode)0, new object[5]
							{
								item[num],
								val4,
								((Component)this).transform.rotation,
								quant[num],
								ammo[num]
							});
						}
						else
						{
							((sc_spawner)(object)((Component)myspawner).GetComponent(typeof(sc_spawner))).dropitem(item[num], val4, ((Component)this).transform.rotation, quant[num], ammo[num]);
						}
					}
				}
				((sc_deadplayer)(object)((Component)val).GetComponent(typeof(sc_deadplayer))).gamecontrol = gamecontrol;
				((sc_deadplayer)(object)((Component)val).GetComponent(typeof(sc_deadplayer))).cam = cam;
				((sc_deadplayer)(object)((Component)val).GetComponent(typeof(sc_deadplayer))).playername = playername;
				((sc_deadplayer)(object)((Component)val).GetComponent(typeof(sc_deadplayer))).mouse_sense = mouse_sense;
				((sc_deadplayer)(object)((Component)val).GetComponent(typeof(sc_deadplayer))).myid = myid;
				((sc_deadplayer)(object)((Component)val).GetComponent(typeof(sc_deadplayer))).autoreload = autoreload;
				Object.Destroy((Object)(object)((Component)((sc_deadplayer)(object)((Component)val).GetComponent(typeof(sc_deadplayer))).headmesh).gameObject);
				((Behaviour)((Component)((sc_deadplayer)(object)((Component)val).GetComponent(typeof(sc_deadplayer))).luzi).GetComponent<Light>()).enabled = true;
				cam.parent = ((sc_deadplayer)(object)((Component)val).GetComponent(typeof(sc_deadplayer))).head;
				cam.localPosition = Vector3.zero;
				cam.localEulerAngles = Vector3.zero;
				((sc_spawner)(object)((Component)myspawner).GetComponent(typeof(sc_spawner))).canspawn = false;
				((Component)this).NetView().RPC("killmebuffered", (RPCMode)6, new object[0]);
			}
		}
		if (!(barulho <= lastbarulho))
		{
			fakebarulho = barulho;
			zbs = GameObject.FindGameObjectsWithTag("zumbi");
			for (num = 0; num < zbs.Length; num++)
			{
				if (!(Vector3.Distance(zbs[num].transform.position, ((Component)this).transform.position) >= barulho / 4f))
				{
					if (Network.isServer)
					{
						((sc_zumbi)(object)zbs[num].GetComponent(typeof(sc_zumbi))).hearnoise(myid);
						continue;
					}
					zbs[num].NetView().RPC("hearnoise", (RPCMode)0, new object[1] { myid });
				}
			}
		}
		else
		{
			fakebarulho = Mathf.Lerp(fakebarulho, barulho, 4f * Time.deltaTime);
			soundwarningcont += 1f * Time.deltaTime;
			if (!(soundwarningcont <= 1f))
			{
				zbs = GameObject.FindGameObjectsWithTag("zumbi");
				for (num = 0; num < zbs.Length; num++)
				{
					if (!(Vector3.Distance(zbs[num].transform.position, ((Component)this).transform.position) >= barulho / 4f))
					{
						if (Network.isServer)
						{
							((sc_zumbi)(object)zbs[num].GetComponent(typeof(sc_zumbi))).hearnoise(myid);
							continue;
						}
						zbs[num].NetView().RPC("hearnoise", (RPCMode)0, new object[1] { myid });
					}
				}
				soundwarningcont = 0f;
			}
		}
		exposi *= 0.5f + timecontroler.timecoef / 2f;
		exposiback *= 0.5f + timecontroler.timecoef / 2f;
		if ((Object)(object)cintoitem != (Object)null && ((sc_throwable)(object)((Component)cintoitem).GetComponent(typeof(sc_throwable))).islight)
		{
			exposi = 1f;
			exposiback = 1f;
		}
		lastbarulho = barulho;
		fake_exposi = Mathf.Lerp(fake_exposi, exposi, 4f * Time.deltaTime);
		currentlife = Mathf.Lerp(currentlife, ((sc_life)(object)((Component)this).GetComponent(typeof(sc_life))).quantia, 10f * Time.deltaTime);
		if (!(currentlife <= ((sc_life)(object)((Component)this).GetComponent(typeof(sc_life))).quantia - 0.5f) && !(currentlife >= ((sc_life)(object)((Component)this).GetComponent(typeof(sc_life))).quantia + 0.5f))
		{
			currentlife = ((sc_life)(object)((Component)this).GetComponent(typeof(sc_life))).quantia;
		}
		return;
		IL_3d0c:
		if (Input.GetButtonDown("Jump") && controled)
		{
			armrottype = 0;
			((Component)this).NetView().RPC("setrot", (RPCMode)1, new object[1] { armrottype });
			canspin = true;
			legstate = 0;
			((Component)legs).GetComponent<Animation>().CrossFade("jump", 0.3f);
			((Component)this).NetView().RPC("fadeleganim", (RPCMode)1, new object[3]
			{
				"jump",
				0.3f,
				((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()["jump"].speed
			});
			vy = jumpspeed;
			armstate = 11;
			animationcont = 0f;
			futurearmstate = 0;
			waitingtime = 250;
			if (item[itemselected] >= 0)
			{
				animateinoptions();
			}
			else
			{
				((Component)chest).GetComponent<Animation>().CrossFade("nogunidle", 0.2f);
				((Component)this).NetView().RPC("fadearmanim", (RPCMode)1, new object[3]
				{
					"nogunidle",
					0.2f,
					((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()["nogunidle"].speed
				});
			}
		}
		goto IL_3ea4;
	}

	public void OnGUI()
	{
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0376: Unknown result type (might be due to invalid IL or missing references)
		//IL_037b: Unknown result type (might be due to invalid IL or missing references)
		//IL_037c: Unknown result type (might be due to invalid IL or missing references)
		//IL_038f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0391: Unknown result type (might be due to invalid IL or missing references)
		//IL_0392: Unknown result type (might be due to invalid IL or missing references)
		//IL_0399: Unknown result type (might be due to invalid IL or missing references)
		//IL_0311: Unknown result type (might be due to invalid IL or missing references)
		//IL_0316: Unknown result type (might be due to invalid IL or missing references)
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		//IL_0329: Unknown result type (might be due to invalid IL or missing references)
		//IL_032b: Unknown result type (might be due to invalid IL or missing references)
		//IL_032c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0333: Unknown result type (might be due to invalid IL or missing references)
		//IL_0362: Unknown result type (might be due to invalid IL or missing references)
		//IL_042c: Unknown result type (might be due to invalid IL or missing references)
		//IL_03dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0478: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0513: Unknown result type (might be due to invalid IL or missing references)
		//IL_0549: Unknown result type (might be due to invalid IL or missing references)
		//IL_057c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0581: Unknown result type (might be due to invalid IL or missing references)
		//IL_0582: Unknown result type (might be due to invalid IL or missing references)
		//IL_0594: Unknown result type (might be due to invalid IL or missing references)
		//IL_0596: Unknown result type (might be due to invalid IL or missing references)
		//IL_0597: Unknown result type (might be due to invalid IL or missing references)
		//IL_059e: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_05cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0613: Unknown result type (might be due to invalid IL or missing references)
		//IL_064c: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_06cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_067b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0680: Unknown result type (might be due to invalid IL or missing references)
		//IL_0681: Unknown result type (might be due to invalid IL or missing references)
		//IL_0693: Unknown result type (might be due to invalid IL or missing references)
		//IL_0695: Unknown result type (might be due to invalid IL or missing references)
		//IL_0696: Unknown result type (might be due to invalid IL or missing references)
		//IL_069d: Unknown result type (might be due to invalid IL or missing references)
		//IL_09e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_09ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_09f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a0a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a0f: Unknown result type (might be due to invalid IL or missing references)
		//IL_072b: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_080a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0896: Unknown result type (might be due to invalid IL or missing references)
		//IL_0922: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e90: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a7c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a81: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aa2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ac6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ad0: Unknown result type (might be due to invalid IL or missing references)
		//IL_09ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_0af4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0af9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0afe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b1b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b20: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b25: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fea: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ffe: Unknown result type (might be due to invalid IL or missing references)
		//IL_1003: Unknown result type (might be due to invalid IL or missing references)
		//IL_1004: Unknown result type (might be due to invalid IL or missing references)
		//IL_1016: Unknown result type (might be due to invalid IL or missing references)
		//IL_1018: Unknown result type (might be due to invalid IL or missing references)
		//IL_1019: Unknown result type (might be due to invalid IL or missing references)
		//IL_1020: Unknown result type (might be due to invalid IL or missing references)
		//IL_1045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0eea: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f2d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f69: Unknown result type (might be due to invalid IL or missing references)
		//IL_1341: Unknown result type (might be due to invalid IL or missing references)
		//IL_1346: Unknown result type (might be due to invalid IL or missing references)
		//IL_1347: Unknown result type (might be due to invalid IL or missing references)
		//IL_1359: Unknown result type (might be due to invalid IL or missing references)
		//IL_135b: Unknown result type (might be due to invalid IL or missing references)
		//IL_135c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1363: Unknown result type (might be due to invalid IL or missing references)
		//IL_130e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1313: Unknown result type (might be due to invalid IL or missing references)
		//IL_1314: Unknown result type (might be due to invalid IL or missing references)
		//IL_1326: Unknown result type (might be due to invalid IL or missing references)
		//IL_1328: Unknown result type (might be due to invalid IL or missing references)
		//IL_1329: Unknown result type (might be due to invalid IL or missing references)
		//IL_1330: Unknown result type (might be due to invalid IL or missing references)
		//IL_12a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_1157: Unknown result type (might be due to invalid IL or missing references)
		//IL_1082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d45: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d58: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d5d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d5e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d70: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d72: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d73: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d7a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d97: Unknown result type (might be due to invalid IL or missing references)
		//IL_0db0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0db5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0db6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dc9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dcb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dcc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dd3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0df2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e06: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e1a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e1f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e20: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e32: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e34: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e35: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e3c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e5b: Unknown result type (might be due to invalid IL or missing references)
		//IL_1388: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c16: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c29: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c2e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c2f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c41: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c43: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c44: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c4b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c68: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c81: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c86: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c87: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c9a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c9c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c9d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ca4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cc3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cd7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ceb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cf0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cf1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d03: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d05: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d06: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d0d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d2c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1440: Unknown result type (might be due to invalid IL or missing references)
		//IL_1445: Unknown result type (might be due to invalid IL or missing references)
		//IL_1446: Unknown result type (might be due to invalid IL or missing references)
		//IL_1458: Unknown result type (might be due to invalid IL or missing references)
		//IL_145a: Unknown result type (might be due to invalid IL or missing references)
		//IL_145b: Unknown result type (might be due to invalid IL or missing references)
		//IL_1462: Unknown result type (might be due to invalid IL or missing references)
		//IL_140c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1411: Unknown result type (might be due to invalid IL or missing references)
		//IL_1412: Unknown result type (might be due to invalid IL or missing references)
		//IL_1425: Unknown result type (might be due to invalid IL or missing references)
		//IL_1427: Unknown result type (might be due to invalid IL or missing references)
		//IL_1428: Unknown result type (might be due to invalid IL or missing references)
		//IL_142f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1487: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		float num4 = 0f;
		if (item[itemselected] >= 0 && ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[itemselected]].tipo == 0 && showscope)
		{
			GUI.DrawTexture(new Rect(0f, 0f, (float)Screen.width, (float)Screen.height), ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).scope, (ScaleMode)1, true, 0f);
		}
		if (!(bleeding <= 0f))
		{
			GUI.color = Color.red;
			num4 = Mathf.Cos(red_a * (float)Math.PI) * 0.2f;
			if (!(num4 >= 0f))
			{
				float a = 0f - num4;
				Color color = GUI.color;
				float num5 = (color.a = a);
				Color val = (GUI.color = color);
			}
			else
			{
				float a2 = num4;
				Color color2 = GUI.color;
				float num6 = (color2.a = a2);
				Color val3 = (GUI.color = color2);
			}
			GUI.DrawTexture(new Rect(0f, 0f, (float)Screen.width, (float)Screen.height), whitecube, (ScaleMode)0, true, 0f);
		}
		GUI.color = Color.white;
		for (num3 = 0; num3 < bloods; num3++)
		{
			float a3 = bloodalpha[num3];
			Color color3 = GUI.color;
			float num7 = (color3.a = a3);
			Color val5 = (GUI.color = color3);
			GUI.DrawTexture(new Rect(bloodpos[num3].x - (float)(bloodspill[bloodtype[num3]].width * 4), bloodpos[num3].y - (float)(bloodspill[bloodtype[num3]].height * 4), (float)(bloodspill[bloodtype[num3]].width * 8), (float)(bloodspill[bloodtype[num3]].height * 8)), bloodspill[bloodtype[num3]], (ScaleMode)0, true, 0f);
		}
		for (num3 = 0; num3 < bloods; num3++)
		{
			bloodalpha[num3] -= 0.1f * Time.deltaTime;
			if (!(bloodalpha[num3] > 0f))
			{
				bloodalpha[num3] = 0f;
				bloodalpha[num3] = bloodalpha[bloods - 1];
				bloodpos[num3] = bloodpos[bloods - 1];
				bloodtype[num3] = bloodtype[bloods - 1];
				bloods--;
			}
		}
		if (armstate == 15)
		{
			GUI.skin = guiskin_WB;
			float a4 = 0.4f;
			Color color4 = GUI.color;
			float num8 = (color4.a = a4);
			Color val7 = (GUI.color = color4);
			GUI.Box(new Rect((float)(Screen.width / 2) - throw_charge * 90f / 2f, (float)(Screen.height - 45), throw_charge * 90f, 9f), string.Empty);
		}
		int num9 = 1;
		Color color5 = GUI.color;
		float num10 = (color5.a = num9);
		Color val9 = (GUI.color = color5);
		if (real_crouching)
		{
			GUI.DrawTexture(new Rect(5f, (float)(Screen.height - 42 - crouch_stand_icons[1].height), (float)crouch_stand_icons[1].width, (float)crouch_stand_icons[1].height), crouch_stand_icons[1], (ScaleMode)0, true, 0f);
		}
		else
		{
			GUI.DrawTexture(new Rect(5f, (float)(Screen.height - 42 - crouch_stand_icons[0].height), (float)crouch_stand_icons[0].width, (float)crouch_stand_icons[0].height), crouch_stand_icons[0], (ScaleMode)0, true, 0f);
		}
		GUI.DrawTexture(new Rect(55f, (float)(Screen.height - 65 - detection_icons[0].height), (float)detection_icons[0].width, (float)detection_icons[0].height), detection_icons[0], (ScaleMode)0, true, 0f);
		GUI.DrawTexture(new Rect(55f, (float)(Screen.height - 45 - detection_icons[1].height), (float)detection_icons[1].width, (float)detection_icons[1].height), detection_icons[1], (ScaleMode)0, true, 0f);
		GUI.skin = whitebarskin;
		num4 = fakebarulho / 120f;
		if (!(num4 <= 1f))
		{
			num4 = 1f;
		}
		GUI.Box(new Rect(85f, (float)(Screen.height - 79), 10f + 75f * num4, 10f), string.Empty);
		GUI.Box(new Rect(85f, (float)(Screen.height - 56), 10f + 75f * ((fake_exposi + exposiback) / 2f), 10f), string.Empty);
		num = (int)currentlife;
		num2 = (int)sprintbar;
		GUI.skin = guiskinammoL;
		float a5 = 0.6f;
		Color color6 = GUI.color;
		float num11 = (color6.a = a5);
		Color val11 = (GUI.color = color6);
		GUI.Box(new Rect(5f, (float)(Screen.height - 30), 210f, 20f), string.Empty);
		int num12 = 1;
		Color color7 = GUI.color;
		float num13 = (color7.a = num12);
		Color val13 = (GUI.color = color7);
		GUI.skin = guiskin;
		GUI.Label(new Rect((float)(15 + num * 2), (float)(Screen.height - 35), 400f, 25f), string.Empty + num + "%");
		GUI.Box(new Rect(5f, (float)(Screen.height - 30), (float)(10 + num * 2), 20f), string.Empty);
		GUI.skin = guiskin2;
		if (num2 < mintosrpint)
		{
			float a6 = 0.2f;
			Color color8 = GUI.color;
			float num14 = (color8.a = a6);
			Color val15 = (GUI.color = color8);
		}
		GUI.Box(new Rect(5f, (float)(Screen.height - 40), (float)(10 + num2 * 2), 10f), string.Empty);
		int num15 = 1;
		Color color9 = GUI.color;
		float num16 = (color9.a = num15);
		Color val17 = (GUI.color = color9);
		if (item[itemselected] >= 0)
		{
			GUI.skin = guiskinammoR;
			GUI.Label(new Rect((float)(Screen.width - 210), (float)(Screen.height - 30), 200f, 30f), ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[itemselected]].nome);
			if (((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[itemselected]].tipo == 0)
			{
				GUI.skin = guiskinammoR;
				GUI.Label(new Rect((float)(Screen.width - 105), (float)(Screen.height - 60), 50f, 30f), string.Empty + quant[itemselected]);
				GUI.skin = guiskinammoL;
				GUI.Label(new Rect((float)(Screen.width - 50), (float)(Screen.height - 60), 100f, 30f), "/ " + ammo[itemselected]);
				GUI.skin = guiskin;
			}
			if (((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[itemselected]].tipo == 3)
			{
				GUI.skin = guiskinammoL;
				GUI.Label(new Rect((float)(Screen.width - 50), (float)(Screen.height - 60), 100f, 30f), "x" + ammo[itemselected]);
				GUI.skin = guiskin;
			}
			if (((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[itemselected]].tipo == 4)
			{
				GUI.skin = guiskinammoL;
				GUI.Label(new Rect((float)(Screen.width - 50), (float)(Screen.height - 60), 100f, 30f), "x" + ammo[itemselected]);
				GUI.skin = guiskin;
			}
			if (((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[itemselected]].tipo == 5)
			{
				GUI.skin = guiskinammoL;
				GUI.Label(new Rect((float)(Screen.width - 50), (float)(Screen.height - 60), 100f, 30f), "x" + ammo[itemselected]);
				GUI.skin = guiskin;
			}
		}
		LayerMask val19 = default(LayerMask);
		bool flag = false;
		RaycastHit val20 = default(RaycastHit);
		Vector2 val21 = default(Vector2);
		val21 = new Vector2((float)(Screen.width / 2), (float)(Screen.height / 2));
		if (item[itemselected] >= 0 && armstate == 0 && ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[itemselected]].tipo == 0 && (Object)(object)handitem != (Object)null)
		{
			val19 = (2099456);
			if (Physics.Raycast(((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).output.position, ((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).output.right, out val20, 80f, (val19)))
			{
				val21.x = ((Component)cam).GetComponent<Camera>().WorldToScreenPoint(val20.point).x;
				val21.y = ((Component)cam).GetComponent<Camera>().WorldToScreenPoint(val20.point).y;
				val21.y = (float)Screen.height - val21.y;
			}
		}
		if (((sc_spawner)(object)((Component)myspawner).GetComponent(typeof(sc_spawner))).state == 1)
		{
			if (armstate == 0 || armstate == 12 || armstate == 6)
			{
				if (interacttype < 0)
				{
					if (!zooming && item[itemselected] >= 0)
					{
						if (((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[itemselected]].tipo == 0)
						{
							if (((sc_gunscript)(object)((Component)handitem).GetComponent(typeof(sc_gunscript))).crosshair)
							{
								GUI.color = Color.white;
								float a7 = 0.4f;
								Color color10 = GUI.color;
								float num17 = (color10.a = a7);
								Color val22 = (GUI.color = color10);
								GUI.DrawTexture(new Rect((float)(Screen.width / 2 - 8), (float)(Screen.height / 2 - 8), 16f, 16f), pointcrosshair, (ScaleMode)0, true, 0f);
								int num18 = 1;
								Color color11 = GUI.color;
								float num19 = (color11.a = num18);
								Color val24 = (GUI.color = color11);
								GUI.DrawTexture(new Rect(val21.x - 16f, val21.y - 16f, 32f, 32f), crosshair, (ScaleMode)0, true, 0f);
								GUI.color = Color.red;
								float a8 = hitM_conter;
								Color color12 = GUI.color;
								float num20 = (color12.a = a8);
								Color val26 = (GUI.color = color12);
								GUI.DrawTexture(new Rect(val21.x - 16f, val21.y - 16f, 32f, 32f), hitmarker, (ScaleMode)0, true, 0f);
							}
						}
						else
						{
							GUI.color = Color.white;
							float a9 = 0.4f;
							Color color13 = GUI.color;
							float num21 = (color13.a = a9);
							Color val28 = (GUI.color = color13);
							GUI.DrawTexture(new Rect((float)(Screen.width / 2 - 8), (float)(Screen.height / 2 - 8), 16f, 16f), pointcrosshair, (ScaleMode)0, true, 0f);
							int num22 = 1;
							Color color14 = GUI.color;
							float num23 = (color14.a = num22);
							Color val30 = (GUI.color = color14);
							GUI.DrawTexture(new Rect(val21.x - 16f, val21.y - 16f, 32f, 32f), crosshair, (ScaleMode)0, true, 0f);
							GUI.color = Color.red;
							float a10 = hitM_conter;
							Color color15 = GUI.color;
							float num24 = (color15.a = a10);
							Color val32 = (GUI.color = color15);
							GUI.DrawTexture(new Rect(val21.x - 16f, val21.y - 16f, 32f, 32f), hitmarker, (ScaleMode)0, true, 0f);
						}
					}
				}
				else
				{
					GUI.DrawTexture(new Rect((float)(Screen.width / 2 - 16), (float)(Screen.height / 2 - 16), 32f, 32f), interactsign[interacttype], (ScaleMode)0, true, 0f);
				}
			}
			if (armstate == 10)
			{
				GUI.skin = guiskinammoL;
				num = 500;
				GUI.Box(new Rect((float)(Screen.width / 2 - 250), (float)(Screen.height / 2 - 10), (float)(num + 10), 20f), string.Empty);
				GUI.skin = guiskinammoR;
				num = (int)rpcbar;
				GUI.Box(new Rect((float)(Screen.width / 2 - 250), (float)(Screen.height / 2 - 10), (float)(num + 10), 20f), string.Empty);
				GUI.skin = guiskin2;
				GUI.Label(new Rect((float)(Screen.width / 2 - 300), (float)(Screen.height / 2 - 50), 600f, 50f), ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).language[((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).languageset].frase[51] + ((sc_deadplayer)(object)((Component)revivetarget).GetComponent(typeof(sc_deadplayer))).playername);
			}
			GUI.skin = guiskin2;
			GUI.color = message_col;
			float a11 = message_a;
			Color color16 = GUI.color;
			float num25 = (color16.a = a11);
			Color val34 = (GUI.color = color16);
			GUI.Label(new Rect((float)(Screen.width / 2 - 300), (float)(Screen.height / 2 - 25), 600f, 50f), the_message);
			message_a -= 0.5f * Time.deltaTime;
			if (!(message_a >= 0f))
			{
				message_a = 0f;
			}
			GUI.color = Color.white;
		}
		if (((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).gamestate == 3 && !((sc_matchcontroller)(object)((Component)((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).matchcontroller).GetComponent(typeof(sc_matchcontroller))).gamestarted && ((sc_spawner)(object)((Component)myspawner).GetComponent(typeof(sc_spawner))).state == 1 && !Input.GetKey((KeyCode)9) && controled)
		{
			GUI.skin = guiskin2;
			GUI.Label(new Rect((float)(Screen.width / 2 - 300), (float)(Screen.height / 2 - 125), 600f, 100f), ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).language[((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).languageset].frase[35]);
			if (Input.GetKeyDown((KeyCode)13))
			{
				((sc_matchcontroller)(object)((Component)((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).matchcontroller).GetComponent(typeof(sc_matchcontroller))).gamestarted = true;
			}
		}
		if (((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).gamestate == 6 && (Object)(object)GameObject.Find("matchdata(Clone)") != (Object)null && !((sc_matchdata)(object)GameObject.Find("matchdata(Clone)").GetComponent(typeof(sc_matchdata))).isstarted && ((sc_spawner)(object)((Component)myspawner).GetComponent(typeof(sc_spawner))).state == 1)
		{
			GUI.skin = guiskin2;
			GUI.Label(new Rect((float)(Screen.width / 2 - 300), (float)(Screen.height / 2 - 125), 600f, 100f), ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).language[((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).languageset].frase[34]);
		}
		for (num3 = 0; num3 < item.Length; num3++)
		{
			if (num3 == itemtoselect)
			{
				float a12 = 0.3f;
				Color color17 = GUI.color;
				float num26 = (color17.a = a12);
				Color val36 = (GUI.color = color17);
			}
			else
			{
				float a13 = 0.1f;
				Color color18 = GUI.color;
				float num27 = (color18.a = a13);
				Color val38 = (GUI.color = color18);
			}
			GUI.DrawTexture(new Rect((float)(Screen.width - 100), (float)(Screen.height / 2 - 175 + 35 * num3), 96f, 33f), ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).backsprite, (ScaleMode)0, true, 0f);
			if (item[num3] >= 0 && (Object)(object)((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[num3]].sprite != (Object)null)
			{
				if (num3 == itemselected)
				{
					int num28 = 1;
					Color color19 = GUI.color;
					float num29 = (color19.a = num28);
					Color val40 = (GUI.color = color19);
				}
				else
				{
					float a14 = 0.6f;
					Color color20 = GUI.color;
					float num30 = (color20.a = a14);
					Color val42 = (GUI.color = color20);
				}
				GUI.DrawTexture(new Rect((float)(Screen.width - 100), (float)(Screen.height / 2 - 175 + 35 * num3), 96f, 33f), ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[item[num3]].sprite, (ScaleMode)0, true, 0f);
			}
		}
	}

	public void add_hitmarker()
	{
		hitM_conter = 2f;
	}

	public void OnDestroy()
	{
		if ((Object)(object)placer != (Object)null)
		{
			Object.Destroy((Object)(object)((Component)placer).gameObject);
		}
	}

	[RPC]
	public void activelight()
	{
		flashconter = 100f;
		((Behaviour)((Component)flash).GetComponent<Light>()).enabled = true;
	}

	[RPC]
	public void spillblood()
	{
		int num = 0;
		if (bloods < bloodalpha.Length)
		{
			num = bloods;
			bloodalpha[num] = 0.4f;
			bloodpos[num].x = Random.Range(0, Screen.width);
			bloodpos[num].y = Random.Range(0, Screen.height);
			bloodtype[num] = Random.Range(0, bloodspill.Length);
			bloods++;
		}
		else
		{
			num = 0;
			bloodalpha[num] = 0.4f;
			bloodpos[num].x = Random.Range(0, Screen.width);
			bloodpos[num].y = Random.Range(0, Screen.height);
			bloodtype[num] = Random.Range(0, bloodspill.Length);
		}
	}

	[RPC]
	public void changecintoitem(int itemid)
	{
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Expected O, but got Unknown
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		gamecontrol = GameObject.Find("gamecontroler").transform;
		if ((Object)(object)gamecontrol != (Object)null)
		{
			if ((Object)(object)cintoitem != (Object)null)
			{
				Object.Destroy((Object)(object)((Component)cintoitem).gameObject);
			}
			if (itemid >= 0)
			{
				cintoitem = (Transform)Object.Instantiate((Object)(object)((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[itemid].prefab);
				cintoitem.parent = cintopos;
				cintoitem.localPosition = Vector3.zero;
				cintoitem.localEulerAngles = Vector3.zero;
			}
		}
	}

	[RPC]
	public void changeitem(int itemid)
	{
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Expected O, but got Unknown
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		gamecontrol = GameObject.Find("gamecontroler").transform;
		if ((Object)(object)gamecontrol != (Object)null)
		{
			if ((Object)(object)handitem != (Object)null)
			{
				Object.Destroy((Object)(object)((Component)handitem).gameObject);
			}
			if (itemid >= 0)
			{
				handitem = (Transform)Object.Instantiate((Object)(object)((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[itemid].prefab);
				handitem.parent = handpos;
				handitem.localPosition = Vector3.zero;
				handitem.localEulerAngles = Vector3.zero;
			}
		}
	}

	[RPC]
	public void setmyname(string RPCplayername)
	{
		playername = RPCplayername;
	}

	[RPC]
	public void playitemsound()
	{
		if ((Object)(object)((Component)handitem).GetComponent<AudioSource>() != (Object)null)
		{
			((Component)handitem).GetComponent<AudioSource>().Play();
		}
	}

	[RPC]
	public void playarmanim(string anim_name, float animspeed)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>().Rewind(anim_name);
		((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()[anim_name].speed = animspeed;
		((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>().Play(anim_name);
	}

	[RPC]
	public void fadearmanim(string anim_name, float time, float animspeed)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>()[anim_name].speed = animspeed;
		((Component)(Animation)((Component)chest).GetComponent(typeof(Animation))).GetComponent<Animation>().CrossFade(anim_name, time);
	}

	[RPC]
	public void playleganim(string anim_name, float animspeed)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>().Rewind(anim_name);
		((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()[anim_name].speed = animspeed;
		((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>().Play(anim_name);
	}

	[RPC]
	public void fadeleganim(string anim_name, float time, float animspeed)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>()[anim_name].speed = animspeed;
		((Component)(Animation)((Component)legs).GetComponent(typeof(Animation))).GetComponent<Animation>().CrossFade(anim_name, time);
	}

	[RPC]
	public void setid(int id)
	{
		myid = id;
	}

	[RPC]
	public void receiveammo(int RPCid, int RPCammo)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		if (!((Component)this).NetView().isMine)
		{
			return;
		}
		message_col = Color.white;
		the_message = ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).language[((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).languageset].frase[53] + ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[RPCid].nome;
		message_a = 1f;
		if (item[itemselected] == RPCid)
		{
			ammo[itemselected] += RPCammo;
			return;
		}
		int num = -1;
		for (int i = 0; i < item.Length; i++)
		{
			if (item[i] == RPCid)
			{
				num = i;
				i = item.Length;
			}
		}
		if (num >= 0)
		{
			ammo[num] += RPCammo;
		}
	}

	[RPC]
	public void receiveitem(int RPCid, int RPCquant, int RPCammo)
	{
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		if (!((Component)this).NetView().isMine)
		{
			return;
		}
		int num = -1;
		for (int i = 0; i < item.Length; i++)
		{
			if (item[i] == -1)
			{
				num = i;
				i = item.Length;
			}
		}
		if (num >= 0)
		{
			item[num] = RPCid;
			quant[num] = RPCquant;
			ammo[num] = RPCammo;
			message_col = Color.white;
			the_message = ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[RPCid].nome;
			message_a = 1f;
			if (num == itemselected)
			{
				generatehanditem();
			}
		}
	}

	public void Main()
	{
	}
}
