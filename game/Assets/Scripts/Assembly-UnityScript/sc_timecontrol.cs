using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_timecontrol : MonoBehaviour
{
	public float timeconter;

	public float timecoef;

	public float timepasser;

	public Light sol45;

	public float dir;

	public Material skymaterial;

	public float asktime;

	public sc_timecontrol()
	{
		timecoef = 0.5f;
		timepasser = 0.2f;
		dir = 1f;
	}

	[RPC]
	public void asktimer()
	{
		if (Network.isServer)
		{
			((Component)this).NetView().RPC("set_realtime", (RPCMode)2, new object[1] { timeconter });
		}
	}

	[RPC]
	public void set_realtime(float time)
	{
		timeconter = time;
	}

	public void Update()
	{
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_0275: Unknown result type (might be due to invalid IL or missing references)
		//IL_0276: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0290: Unknown result type (might be due to invalid IL or missing references)
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b2: Unknown result type (might be due to invalid IL or missing references)
		if (!(asktime >= 1f))
		{
			asktime += 1f * Time.deltaTime;
			if (!(asktime <= 0.2f))
			{
				((Component)this).NetView().RPC("asktimer", (RPCMode)2, new object[0]);
			}
		}
		timeconter += timepasser * Time.deltaTime;
		if (!(timeconter <= 240f))
		{
			timeconter -= 240f;
			if (Network.isServer)
			{
				((Component)this).NetView().RPC("set_realtime", (RPCMode)2, new object[1] { timeconter });
			}
		}
		if (!(timeconter < 0f) && !(timeconter >= 50f))
		{
			timecoef = 0f;
		}
		if (!(timeconter < 50f) && !(timeconter >= 80f))
		{
			timecoef = (timeconter - 50f) / 30f * 0.7f;
		}
		if (!(timeconter < 80f) && !(timeconter >= 130f))
		{
			timecoef = 0.7f + (timeconter - 80f) / 50f * 0.3f;
		}
		if (!(timeconter < 130f) && !(timeconter >= 180f))
		{
			timecoef = 1f;
		}
		if (!(timeconter < 175f) && !(timeconter >= 205f))
		{
			timecoef = 1f - (timeconter - 175f) / 30f;
		}
		if (!(timeconter < 205f) && !(timeconter > 240f))
		{
			timecoef = 0f;
		}
		if (!(timecoef >= 0f))
		{
			timecoef = 0f;
		}
		if (!(timecoef <= 1f))
		{
			timecoef = 1f;
		}
		sol45.intensity = 0.35f * timecoef;
		float r = timecoef * 0.15f;
		Color ambientLight = RenderSettings.ambientLight;
		float num = (ambientLight.r = r);
		Color val = (RenderSettings.ambientLight = ambientLight);
		float g = timecoef * 0.15f;
		Color ambientLight2 = RenderSettings.ambientLight;
		float num2 = (ambientLight2.g = g);
		Color val3 = (RenderSettings.ambientLight = ambientLight2);
		float b = timecoef * 0.15f;
		Color ambientLight3 = RenderSettings.ambientLight;
		float num3 = (ambientLight3.b = b);
		Color val5 = (RenderSettings.ambientLight = ambientLight3);
		skymaterial.SetFloat("_Blend", timecoef);
	}

	public void Main()
	{
	}
}
