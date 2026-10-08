using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_vehicle : MonoBehaviour
{
	public bool freecontrols;

	public float vert;

	public float sides;

	public bool a;

	public bool d;

	public Transform[] wheels;

	public sc_vehicle()
	{
		freecontrols = true;
	}

	public void Start()
	{
	}

	public void Update()
	{
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		if (freecontrols)
		{
			vert = Input.GetAxis("Vertical");
			sides = Input.GetAxis("Horizontal");
		}
		for (num = 0; num < wheels.Length; num++)
		{
			if (((sc_wheel)(object)((Component)wheels[num]).GetComponent(typeof(sc_wheel))).motorized)
			{
				if (((sc_roda)(object)((Component)((sc_wheel)(object)((Component)wheels[num]).GetComponent(typeof(sc_wheel))).thewheel).GetComponent(typeof(sc_roda))).touching)
				{
					((Component)this).GetComponent<Rigidbody>().AddForceAtPosition(wheels[num].right * ((sc_wheel)(object)((Component)wheels[num]).GetComponent(typeof(sc_wheel))).motorforce * vert * Time.deltaTime, wheels[num].position);
				}
				float y = sides * 45f;
				Vector3 localEulerAngles = wheels[num].localEulerAngles;
				float num2 = (localEulerAngles.y = y);
				Vector3 val = (wheels[num].localEulerAngles = localEulerAngles);
			}
		}
	}

	public void Main()
	{
	}
}
