using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_suplly : MonoBehaviour
{
	public float fallspeed;

	public float conter;

	public float timetofall;

	public Transform bagprefab;

	public float height;

	public sc_suplly()
	{
		fallspeed = 1f;
		timetofall = 400f;
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
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Expected O, but got Unknown
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		Transform val = null;
		((Component)this).transform.Translate(Vector3.down * fallspeed * Time.deltaTime);
		conter += 100f * Time.deltaTime;
		if (!(conter <= timetofall))
		{
			val = (Transform)Network.Instantiate((Object)(object)bagprefab, ((Component)this).transform.position, ((Component)this).transform.rotation, 1);
			((Behaviour)(sc_sendpositionupdate)(object)((Component)val).GetComponent(typeof(sc_sendpositionupdate))).enabled = true;
			((Behaviour)(sc_updateposition)(object)((Component)val).GetComponent(typeof(sc_updateposition))).enabled = false;
			Network.Destroy(((Component)this).NetView().viewID);
		}
	}

	public void Main()
	{
	}
}
