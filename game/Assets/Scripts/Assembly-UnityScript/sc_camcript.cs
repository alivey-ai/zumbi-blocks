using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_camcript : MonoBehaviour
{
	public int to_fov;

	public int zoomspeed;

	public sc_camcript()
	{
		to_fov = 100;
		zoomspeed = 75;
	}

	public void Start()
	{
	}

	public void LateUpdate()
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected O, but got Unknown
		Camera val = (Camera)((Component)this).GetComponent(typeof(Camera));
		if (!(val.fieldOfView >= (float)to_fov))
		{
			val.fieldOfView += (float)zoomspeed * Time.deltaTime;
			if (!(val.fieldOfView <= (float)to_fov))
			{
				val.fieldOfView = to_fov;
			}
		}
		if (!(val.fieldOfView <= (float)to_fov))
		{
			val.fieldOfView -= (float)zoomspeed * Time.deltaTime;
			if (!(val.fieldOfView >= (float)to_fov))
			{
				val.fieldOfView = to_fov;
			}
		}
	}

	public void Main()
	{
	}
}
