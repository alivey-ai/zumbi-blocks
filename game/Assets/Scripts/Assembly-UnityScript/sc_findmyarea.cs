using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_findmyarea : MonoBehaviour
{
	public GameObject myarea;

	public GameObject realarea;

	public void Start()
	{
		if (!Network.isServer)
		{
			((Behaviour)this).enabled = false;
		}
	}

	public void Update()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		RaycastHit val = default(RaycastHit);
		LayerMask val2 = default(LayerMask);
		val2 = (33554432);
		if (Physics.Raycast(((Component)this).transform.position, Vector3.down, out val, 1.4f, (val2)))
		{
			myarea = ((Component)val.collider).gameObject;
			realarea = ((Component)val.collider).gameObject;
		}
		else
		{
			realarea = null;
		}
	}

	public void Main()
	{
	}
}
