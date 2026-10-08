using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_shot : MonoBehaviour
{
	public float speed;

	public float dano;

	public float push;

	public float grav;

	public float vy;

	public float conter;

	public bool canmove;

	public Transform std_particle;

	public int shooter;

	public Transform shooterobj;

	public int piercing;

	public Transform lasthit;

	public sc_shot()
	{
		speed = 300f;
		dano = 5f;
		push = 5f;
		shooter = -1;
	}

	public void Start()
	{
	}

	public void Update()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_059a: Unknown result type (might be due to invalid IL or missing references)
		//IL_051c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0523: Unknown result type (might be due to invalid IL or missing references)
		//IL_0528: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_04fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0503: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Unknown result type (might be due to invalid IL or missing references)
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		//IL_0294: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = default(Vector3);
		Vector3 val2 = default(Vector3);
		LayerMask val3 = default(LayerMask);
		bool flag = false;
		RaycastHit val4 = default(RaycastHit);
		bool flag2 = false;
		val2.x = speed;
		val2 *= Time.deltaTime;
		if (((Component)this).NetView().isMine)
		{
			val3 = (2098432);
			val = ((Component)this).transform.TransformDirection(val2);
			if (Physics.Raycast(((Component)this).transform.position, val, out val4, speed * Time.deltaTime * 1.5f, (val3)))
			{
				flag2 = true;
				if ((Object)(object)(sc_dmgtaker)(object)((Component)val4.collider).gameObject.GetComponent(typeof(sc_dmgtaker)) != (Object)null && (Object)(object)((Component)val4.collider).transform != (Object)(object)lasthit)
				{
					if ((Object)(object)(sc_zumbi)(object)((Component)((sc_dmgtaker)(object)((Component)val4.collider).gameObject.GetComponent(typeof(sc_dmgtaker))).owner).GetComponent(typeof(sc_zumbi)) != (Object)null)
					{
						flag2 = false;
						if (((sc_zumbi)(object)((Component)((sc_dmgtaker)(object)((Component)val4.collider).gameObject.GetComponent(typeof(sc_dmgtaker))).owner).GetComponent(typeof(sc_zumbi))).state != -1)
						{
							((Component)((sc_dmgtaker)(object)((Component)val4.collider).gameObject.GetComponent(typeof(sc_dmgtaker))).owner).NetView().RPC("setkillerid", (RPCMode)2, new object[1] { shooter });
							((sc_life)(object)((Component)((sc_dmgtaker)(object)((Component)val4.collider).gameObject.GetComponent(typeof(sc_dmgtaker))).owner).GetComponent(typeof(sc_life))).hurtme(dano);
							((Component)((sc_dmgtaker)(object)((Component)val4.collider).gameObject.GetComponent(typeof(sc_dmgtaker))).owner).NetView().RPC("hurtme", (RPCMode)1, new object[1] { dano });
							((Component)((sc_dmgtaker)(object)((Component)val4.collider).gameObject.GetComponent(typeof(sc_dmgtaker))).owner).NetView().RPC("forcar", (RPCMode)2, new object[1] { ((Component)this).transform.right * 60f * push });
							if (((sc_dmgtaker)(object)((Component)val4.collider).gameObject.GetComponent(typeof(sc_dmgtaker))).partid == 1)
							{
								((Component)((sc_dmgtaker)(object)((Component)val4.collider).gameObject.GetComponent(typeof(sc_dmgtaker))).owner).NetView().RPC("hurthead", (RPCMode)1, new object[1] { dano });
								((sc_zumbi)(object)((Component)((sc_dmgtaker)(object)((Component)val4.collider).gameObject.GetComponent(typeof(sc_dmgtaker))).owner).GetComponent(typeof(sc_zumbi))).hurthead(dano);
							}
						}
					}
					else if ((Object)(object)(sc_player)(object)((Component)((sc_dmgtaker)(object)((Component)val4.collider).gameObject.GetComponent(typeof(sc_dmgtaker))).owner).GetComponent(typeof(sc_player)) != (Object)null)
					{
						flag2 = false;
						((Component)((sc_dmgtaker)(object)((Component)val4.collider).gameObject.GetComponent(typeof(sc_dmgtaker))).owner).NetView().RPC("spillblood", (RPCMode)2, new object[0]);
					}
					else
					{
						((Component)((sc_dmgtaker)(object)((Component)val4.collider).gameObject.GetComponent(typeof(sc_dmgtaker))).owner).NetView().RPC("hurtme", (RPCMode)1, new object[1] { dano });
						((sc_life)(object)((Component)((sc_dmgtaker)(object)((Component)val4.collider).gameObject.GetComponent(typeof(sc_dmgtaker))).owner).GetComponent(typeof(sc_life))).hurtme(dano);
					}
					if ((Object)(object)shooterobj != (Object)null)
					{
						((sc_player)(object)((Component)shooterobj).GetComponent(typeof(sc_player))).add_hitmarker();
					}
				}
				if ((Object)(object)(sc_particula)(object)((Component)val4.collider).gameObject.GetComponent(typeof(sc_particula)) != (Object)null)
				{
					Network.Instantiate((Object)(object)((sc_particula)(object)((Component)val4.collider).gameObject.GetComponent(typeof(sc_particula))).prefab, val4.point, Quaternion.LookRotation(val4.normal), 3);
				}
				else
				{
					Network.Instantiate((Object)(object)std_particle, val4.point, Quaternion.LookRotation(val4.normal), 3);
				}
				((Component)this).NetView().RPC("killmebuffered", (RPCMode)6, new object[0]);
			}
			conter += 100f * Time.deltaTime;
			if (!(conter <= 350f))
			{
				((Component)this).NetView().RPC("killmebuffered", (RPCMode)6, new object[0]);
			}
		}
		if (canmove)
		{
			((Component)this).transform.Translate(val2);
		}
	}

	[RPC]
	public void setcanmove()
	{
		canmove = true;
	}

	public void Main()
	{
	}
}
