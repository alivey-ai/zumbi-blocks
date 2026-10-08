using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_onlinedeadplayer : MonoBehaviour
{
	public float distmax;

	public GUISkin guiskin;

	public int tmpint;

	public int dist;

	public float tmpfloat;

	public sc_onlinedeadplayer()
	{
		distmax = 0.8f;
	}

	[RPC]
	public void senddeadname(string name)
	{
		((sc_deadplayer)(object)((Component)this).GetComponent(typeof(sc_deadplayer))).playername = name;
	}

	public void Update()
	{
		if (!((sc_deadplayer)(object)((Component)this).GetComponent(typeof(sc_deadplayer))).ap_synced)
		{
			((Component)this).NetView().RPC("dead_ask_aparence", (RPCMode)2, new object[0]);
		}
	}

	public void OnGUI()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = default(Vector3);
		GUI.skin = guiskin;
		GUI.color = Color.red;
		if (!((Object)(object)GameObject.Find("Main Camera") != (Object)null) || ((sc_deadplayer)(object)((Component)this).GetComponent(typeof(sc_deadplayer))).died)
		{
			return;
		}
		val = GameObject.Find("Main Camera").GetComponent<Camera>().WorldToScreenPoint(new Vector3(((Component)this).transform.position.x, ((Component)this).transform.position.y + 1.6f, ((Component)this).transform.position.z));
		if (!(val.z <= 0f))
		{
			tmpint = (int)Vector2.Distance(new Vector2(val.x, val.y), new Vector2((float)(Screen.width / 2), (float)(Screen.height / 2)));
			dist = (int)(Vector2.Distance(new Vector2(0f, 0f), new Vector2((float)(Screen.width / 2), (float)(Screen.height / 2))) * distmax);
			if (tmpint > dist)
			{
				tmpint = dist;
			}
			tmpint = dist - tmpint;
			tmpfloat = tmpint;
			tmpfloat /= dist;
			float a = tmpfloat;
			Color color = GUI.color;
			float num = (color.a = a);
			Color val2 = (GUI.color = color);
			GUI.Label(new Rect(val.x - 300f, (float)Screen.height - val.y, 600f, 40f), ((sc_deadplayer)(object)((Component)this).GetComponent(typeof(sc_deadplayer))).playername);
			int num2 = 1;
			Color color2 = GUI.color;
			float num3 = (color2.a = num2);
			Color val4 = (GUI.color = color2);
		}
	}

	public void Main()
	{
	}
}
