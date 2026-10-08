using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_zumbionlinecontroller : MonoBehaviour
{
	public float posmulti;

	public int yang;

	public Vector3 posissao;

	public sc_zumbionlinecontroller()
	{
		posmulti = 10f;
	}

	public void Start()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		posissao = ((Component)this).transform.position;
		if (((Component)this).NetView().isMine)
		{
			((Behaviour)this).enabled = true;
		}
	}

	public void LateUpdate()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		int num2 = yang;
		Vector3 eulerAngles = ((Component)this).transform.eulerAngles;
		float num3 = (eulerAngles.y = num2);
		Vector3 val = (((Component)this).transform.eulerAngles = eulerAngles);
		((Component)this).transform.Translate((posissao - ((Component)this).transform.position) * Time.deltaTime * posmulti, (Space)0);
	}

	[RPC]
	public void setzumbistuff(int RPCyang, Vector3 RPCpos)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		yang = RPCyang;
		posissao = RPCpos;
	}

	public void Main()
	{
	}
}
