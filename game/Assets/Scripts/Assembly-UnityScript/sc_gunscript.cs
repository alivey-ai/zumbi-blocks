using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_gunscript : MonoBehaviour
{
	public bool hidemagonoutammo;

	public bool isbazuka;

	public bool crosshair;

	public bool basher;

	public Renderer[] meshtohide;

	public bool hidemesh;

	public float timetohide;

	public Transform clipinsound;

	public Transform clipoutsound;

	public Transform backaim;

	public Transform output;

	public bool RFS;

	public float conter;

	public float rangeofexp;

	public int force;

	public float add_fov;

	public float add_ypos;

	public float dano;

	public float noisemaker;

	public float pushforce;

	public float rate;

	public bool auto;

	public bool doflash;

	public int bullets;

	public float spread;

	public float addspread;

	public int adc_fov;

	public float grav;

	public Texture scope;

	public float V_rec;

	public float H_rec;

	public int maxammo;

	public int receiverammo;

	public string shotanim;

	public float shotanimspeed;

	public string zoomshotanim;

	public float zoomshotanimspeed;

	public Transform animatedmesh;

	public int reloadamount;

	public string gunshotanim;

	public float gunshotanimspeed;

	public string reloadanimST;

	public int reloadanimSTspeed;

	public string reloadanimEND;

	public int reloadanimENDspeed;

	public string aimanim;

	public string idleanim;

	public string reloadgrabanim;

	public string reloadputanim;

	public int reloadendtime;

	public int reloadrestarttime;

	public int grabtime;

	public int puttime;

	public int releasemoment;

	public int grabmoment;

	public int putmoment;

	public Transform backpos;

	public Transform magmesh;

	public Transform magprefab;

	public Transform bulletprefab;

	public Transform muzleprefab;

	public Transform flyerpos;

	public Transform flyngbullet_fab;

	public sc_gunscript()
	{
		crosshair = true;
		RFS = true;
		rangeofexp = 6f;
		force = 100;
		add_fov = 20f;
		dano = 5f;
		noisemaker = 100f;
		pushforce = 5f;
		rate = 4f;
		auto = true;
		doflash = true;
		bullets = 1;
		spread = 5f;
		adc_fov = 20;
		grav = 0.9f;
		gunshotanim = "null";
		gunshotanimspeed = 1f;
		reloadanimSTspeed = 1;
		reloadanimENDspeed = 1;
	}

	public void Start()
	{
	}

	public void Update()
	{
		if (!RFS)
		{
			conter += 1000f * Time.deltaTime;
			if (!(conter <= 1000f / rate))
			{
				RFS = true;
				conter = 0f;
			}
		}
	}

	public void Main()
	{
	}
}
