using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_carro : MonoBehaviour
{
	public Transform[] leftright;

	public Transform[] wheel;

	public Transform[] suspension;

	public bool controled;

	public Vector3 startimpulse;

	public Vector3 startimpulseglobal;

	public float speedcoef;

	public float maxvel;

	public float fricperwheel;

	public PhysicsMaterial Fmaterial;

	public sc_carro()
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		controled = true;
		startimpulse = Vector3.zero;
		startimpulseglobal = Vector3.zero;
		maxvel = 100f;
		fricperwheel = 0.2f;
	}

	public void Start()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		((Component)this).GetComponent<Rigidbody>().AddForce(((Component)this).transform.TransformDirection(startimpulse), (ForceMode)1);
		((Component)this).GetComponent<Rigidbody>().AddForce(startimpulseglobal, (ForceMode)1);
	}

	public void Update()
	{
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		if (!controled)
		{
			return;
		}
		for (num = 0; num < wheel.Length; num++)
		{
			if (((sc_suspension)(object)((Component)wheel[num]).GetComponent(typeof(sc_suspension))).touching && ((sc_suspension)(object)((Component)wheel[num]).GetComponent(typeof(sc_suspension))).trassao)
			{
				((Component)this).GetComponent<Rigidbody>().AddForce(wheel[num].forward * ((sc_suspension)(object)((Component)wheel[num]).GetComponent(typeof(sc_suspension))).wheelforce * Input.GetAxis("Vertical"));
			}
		}
		for (num = 0; num < suspension.Length; num++)
		{
			if (!(Input.GetAxis("Horizontal") >= 0f))
			{
				suspension[num].rotation = Quaternion.Lerp(suspension[num].rotation, leftright[0].rotation, 3f * Time.deltaTime);
			}
			if (!(Input.GetAxis("Horizontal") <= 0f))
			{
				suspension[num].rotation = Quaternion.Lerp(suspension[num].rotation, leftright[1].rotation, 3f * Time.deltaTime);
			}
			if (Input.GetAxis("Horizontal") == 0f)
			{
				suspension[num].rotation = Quaternion.Lerp(suspension[num].rotation, leftright[2].rotation, 3f * Time.deltaTime);
			}
		}
	}

	public void Main()
	{
	}
}
