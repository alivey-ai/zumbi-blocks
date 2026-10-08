using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_onlinecontroller : MonoBehaviour
{
	public float posmulti;

	public float angmulti;

	public GUISkin guiskin;

	public float distmax;

	public int yang;

	public int headang;

	public Vector3 posissao;

	public float Rbending;

	public float anglerpcoef;

	public int tmpint;

	public int dist;

	public float tmpfloat;

	public int armrottype;

	public sc_onlinecontroller()
	{
		posmulti = 10f;
		angmulti = 5f;
		distmax = 1f;
		anglerpcoef = 20f;
	}

	public void Start()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		((Component)this).NetView().RPC("asktheplayeranim", (RPCMode)2, new object[0]);
		posissao = ((Component)this).transform.position;
		if (((Component)this).NetView().isMine)
		{
			((Behaviour)this).enabled = false;
		}
	}

	public void LateUpdate()
	{
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_031a: Unknown result type (might be due to invalid IL or missing references)
		if (!((sc_player)(object)((Component)this).GetComponent(typeof(sc_player))).ap_synced)
		{
			((Component)this).NetView().RPC("ask_aparence", (RPCMode)2, new object[0]);
		}
		if (((Behaviour)((Component)((sc_player)(object)((Component)this).GetComponent(typeof(sc_player))).flash).GetComponent<Light>()).enabled)
		{
			((sc_player)(object)((Component)this).GetComponent(typeof(sc_player))).flashconter = ((sc_player)(object)((Component)this).GetComponent(typeof(sc_player))).flashconter - 3000f * Time.deltaTime;
			if (!(((sc_player)(object)((Component)this).GetComponent(typeof(sc_player))).flashconter > 0f))
			{
				((Behaviour)((Component)((sc_player)(object)((Component)this).GetComponent(typeof(sc_player))).flash).GetComponent<Light>()).enabled = false;
			}
		}
		int num = 0;
		float num2 = 0f;
		((sc_player)(object)((Component)this).GetComponent(typeof(sc_player))).bending = Mathf.Lerp(((sc_player)(object)((Component)this).GetComponent(typeof(sc_player))).bending, Rbending, 15f * Time.deltaTime);
		num2 = anglerpcoef * Time.deltaTime;
		if (!(num2 <= 1f))
		{
			num2 = 1f;
		}
		((Component)this).transform.rotation = Quaternion.Lerp(((Component)this).transform.rotation, Quaternion.Euler(0f, (float)yang, 0f), num2);
		((Component)this).transform.Translate((posissao - ((Component)this).transform.position) * Time.deltaTime * posmulti, (Space)0);
		((sc_player)(object)((Component)this).GetComponent(typeof(sc_player))).headang = Mathf.Lerp(((sc_player)(object)((Component)this).GetComponent(typeof(sc_player))).headang, (float)headang, num2);
		for (num = 0; num < ((sc_player)(object)((Component)this).GetComponent(typeof(sc_player))).bendingbones.Length; num++)
		{
			((sc_player)(object)((Component)this).GetComponent(typeof(sc_player))).bendingbones[num].Rotate(((Component)this).transform.forward * ((sc_player)(object)((Component)this).GetComponent(typeof(sc_player))).bending, (Space)0);
		}
		if (armrottype == 0)
		{
			((Component)((sc_player)(object)((Component)this).GetComponent(typeof(sc_player))).head).transform.Rotate(new Vector3(0f, 0f, ((sc_player)(object)((Component)this).GetComponent(typeof(sc_player))).headang));
			for (num = 0; num < ((sc_player)(object)((Component)this).GetComponent(typeof(sc_player))).arms.Length; num++)
			{
				((sc_player)(object)((Component)this).GetComponent(typeof(sc_player))).arms[num].RotateAround(((sc_player)(object)((Component)this).GetComponent(typeof(sc_player))).pivo.position, ((sc_player)(object)((Component)this).GetComponent(typeof(sc_player))).pivo.right, 0f - ((sc_player)(object)((Component)this).GetComponent(typeof(sc_player))).headang);
			}
		}
		if (armrottype == 1)
		{
			((Component)((sc_player)(object)((Component)this).GetComponent(typeof(sc_player))).head).transform.Rotate(new Vector3(0f, 0f, ((sc_player)(object)((Component)this).GetComponent(typeof(sc_player))).headang));
		}
	}

	public void OnGUI()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = default(Vector3);
		GUI.skin = guiskin;
		GUI.color = Color.green;
		if (!((Object)(object)GameObject.Find("Main Camera") != (Object)null))
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
			GUI.Label(new Rect(val.x - 300f, (float)Screen.height - val.y, 600f, 40f), ((sc_player)(object)((Component)this).GetComponent(typeof(sc_player))).playername);
			int num2 = 1;
			Color color2 = GUI.color;
			float num3 = (color2.a = num2);
			Color val4 = (GUI.color = color2);
		}
	}

	[RPC]
	public void setrot(int type)
	{
		armrottype = type;
	}

	[RPC]
	public void setposstuff(int RPCyang, int RPCheadang, Vector3 RPCpos, float exp, float expback, float leanang)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		Rbending = leanang;
		yang = RPCyang;
		headang = RPCheadang;
		posissao = RPCpos;
		((sc_player)(object)((Component)this).GetComponent(typeof(sc_player))).exposi = exp;
		((sc_player)(object)((Component)this).GetComponent(typeof(sc_player))).exposiback = expback;
	}

	public void Main()
	{
	}
}
