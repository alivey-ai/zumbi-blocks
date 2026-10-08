using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class SC_MISSILE : MonoBehaviour
{
	public Transform[] smokeparticles;

	public float speed;

	public float exp_range;

	public float dano;

	public float push;

	public float grav;

	public float ray;

	public float vy;

	public float conter;

	public bool canmove;

	public Transform std_particle;

	public Transform blood_particle;

	public int shooter;

	public Transform shooterobj;

	public SC_MISSILE()
	{
		speed = 300f;
		exp_range = 5f;
		dano = 5f;
		push = 5f;
		ray = 0.07f;
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
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = default(Vector3);
		Vector3 val2 = default(Vector3);
		LayerMask val3 = default(LayerMask);
		bool flag = false;
		float num = 0f;
		float num2 = 0f;
		RaycastHit val4 = default(RaycastHit);
		val2.x = speed;
		val2 *= Time.deltaTime;
		GameObject[] array = null;
		int num3 = 0;
		if (((Component)this).NetView().isMine)
		{
			val3 = (2098432);
			val = ((Component)this).transform.TransformDirection(val2);
			if (Physics.SphereCast(((Component)this).transform.position, ray, val, out val4, speed * Time.deltaTime * 1.5f, (val3)))
			{
				if (((sc_explosao)(object)((Component)this).GetComponent(typeof(sc_explosao))).blow_up("Player", exp_range, shooter, (int)dano, push) && (Object)(object)shooterobj != (Object)null)
				{
					((sc_player)(object)((Component)shooterobj).GetComponent(typeof(sc_player))).add_hitmarker();
				}
				if (((sc_explosao)(object)((Component)this).GetComponent(typeof(sc_explosao))).blow_up("zumbi", exp_range, shooter, (int)dano, push) && (Object)(object)shooterobj != (Object)null)
				{
					((sc_player)(object)((Component)shooterobj).GetComponent(typeof(sc_player))).add_hitmarker();
				}
				Network.Instantiate((Object)(object)std_particle, val4.point, Quaternion.LookRotation(val4.normal), 3);
				((Component)this).NetView().RPC("endmissile", (RPCMode)6, new object[0]);
			}
			conter += 100f * Time.deltaTime;
			if (!(conter <= 500f))
			{
				((Component)this).NetView().RPC("endmissile", (RPCMode)6, new object[0]);
			}
		}
		if (canmove)
		{
			((Component)this).transform.Translate(val2);
		}
	}

	[RPC]
	public void endmissile()
	{
		for (int i = 0; i < smokeparticles.Length; i++)
		{
			smokeparticles[i].parent = null;
			{ var __em = ((Component)smokeparticles[i]).GetComponent<ParticleSystem>().emission; __em.rateOverTime = 0f; }
		}
		Object.Destroy((Object)(object)((Component)this).gameObject);
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
