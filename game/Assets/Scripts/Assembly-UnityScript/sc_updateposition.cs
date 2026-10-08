using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_updateposition : MonoBehaviour
{
	public Vector3 posissao;

	public float posmulti;

	public sc_updateposition()
	{
		posmulti = 10f;
	}

	public void Start()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		posissao = ((Component)this).transform.position;
	}

	public void Update()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		((Component)this).transform.Translate((posissao - ((Component)this).transform.position) * Time.deltaTime * posmulti, (Space)0);
	}

	[RPC]
	public void updateposition(Vector3 RPCposition)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		posissao = RPCposition;
	}

	public void Main()
	{
	}
}
