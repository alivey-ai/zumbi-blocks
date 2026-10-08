using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_granada : MonoBehaviour
{
	public float dano;

	public float exp_range;

	public float push;

	public float time;

	public Transform std_particle;

	public Transform blood_particle;

	public int shooter;

	public float conter;

	public Transform shooterobj;

	public sc_granada()
	{
		dano = 200f;
		exp_range = 5f;
		push = 5f;
		time = 50f;
		shooter = -1;
	}

	public void Start()
	{
		if (!((Component)this).NetView().isMine)
		{
			((Behaviour)this).enabled = false;
		}
	}

	public void Update()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		LayerMask val = default(LayerMask);
		float num = 0f;
		float num2 = 0f;
		RaycastHit val2 = default(RaycastHit);
		GameObject[] array = null;
		int num3 = 0;
		conter += 10f * Time.deltaTime;
		if (!(conter <= time))
		{
			if (((sc_explosao)(object)((Component)this).GetComponent(typeof(sc_explosao))).blow_up("Player", exp_range, shooter, (int)dano, push) && (Object)(object)shooterobj != (Object)null)
			{
				((sc_player)(object)((Component)shooterobj).GetComponent(typeof(sc_player))).add_hitmarker();
			}
			if (((sc_explosao)(object)((Component)this).GetComponent(typeof(sc_explosao))).blow_up("zumbi", exp_range, shooter, (int)dano, push) && (Object)(object)shooterobj != (Object)null)
			{
				((sc_player)(object)((Component)shooterobj).GetComponent(typeof(sc_player))).add_hitmarker();
			}
			Network.Instantiate((Object)(object)std_particle, ((Component)this).transform.position, std_particle.rotation, 3);
			((Component)this).NetView().RPC("killmebuffered", (RPCMode)6, new object[0]);
		}
	}

	public void Main()
	{
	}
}
