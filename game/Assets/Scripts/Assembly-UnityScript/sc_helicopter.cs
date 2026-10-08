using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_helicopter : MonoBehaviour
{
	public Transform helice;

	public Transform backpos;

	public float engine;

	public sc_veicle_control comp;

	public float eng_force;

	public sc_helicopter()
	{
		eng_force = 100f;
	}

	public void Start()
	{
		comp = (sc_veicle_control)(object)((Component)this).GetComponent(typeof(sc_veicle_control));
	}

	public void Update()
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		engine += comp.engine * Time.deltaTime;
		if (!(engine <= 1f))
		{
			engine = 1f;
		}
		if (!(engine >= 0f))
		{
			engine = 0f;
		}
		((Component)this).GetComponent<Rigidbody>().AddRelativeTorque(Vector3.up * comp.horizontal * 2000f * Time.deltaTime);
		((Component)this).GetComponent<Rigidbody>().AddRelativeTorque(Vector3.forward * (0f - comp.vertical) * 800f * Time.deltaTime);
		((Component)this).GetComponent<Rigidbody>().AddRelativeTorque(Vector3.right * (0f - comp.roll) * 200f * Time.deltaTime);
		((Component)this).GetComponent<Rigidbody>().AddForce(((Component)this).transform.up * engine * eng_force * Time.deltaTime);
	}

	public void Main()
	{
	}
}
