using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_explosao : MonoBehaviour
{
	public Transform blood_particle;

	public bool blow_up(string tegui, float ranger, int shooterid, int dano, float push)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_030e: Unknown result type (might be due to invalid IL or missing references)
		//IL_031c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0327: Unknown result type (might be due to invalid IL or missing references)
		//IL_032c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0331: Unknown result type (might be due to invalid IL or missing references)
		//IL_028a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		//IL_029a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b2: Unknown result type (might be due to invalid IL or missing references)
		GameObject[] array = GameObject.FindGameObjectsWithTag(tegui);
		float num = default(float);
		float num2 = default(float);
		LayerMask val = default(LayerMask);
		RaycastHit val2 = default(RaycastHit);
		bool result = false;
		for (int i = 0; i < array.Length; i++)
		{
			num = Vector3.Distance(array[i].transform.position, ((Component)this).transform.position);
			val = (256);
			bool flag = Physics.Raycast(((Component)this).transform.position, array[i].transform.position - ((Component)this).transform.position, out val2, num + 0.05f, (val));
			num2 = ranger * 2.5f - num;
			num2 /= ranger * 2.5f;
			if (!(num2 >= 0f))
			{
				num2 = 0f;
			}
			if (!(num2 <= 0f) && (Object)(object)(sc_player)(object)array[i].GetComponent(typeof(sc_player)) != (Object)null)
			{
				((sc_player)(object)array[i].GetComponent(typeof(sc_player))).stun_me(20f * num2);
				array[i].NetView().RPC("stun_me", (RPCMode)1, new object[1] { 20f * num2 });
			}
			num2 = ranger - num;
			num2 /= ranger;
			if (!(num2 >= 0f))
			{
				num2 = 0f;
			}
			if (num2 <= 0f)
			{
				continue;
			}
			result = true;
			if (!flag)
			{
				if ((Object)(object)(sc_life)(object)array[i].GetComponent(typeof(sc_life)) != (Object)null)
				{
					((sc_life)(object)array[i].GetComponent(typeof(sc_life))).quantia = ((sc_life)(object)array[i].GetComponent(typeof(sc_life))).quantia - (float)dano * num2;
					array[i].NetView().RPC("hurtme", (RPCMode)1, new object[1] { (float)dano * num2 });
				}
				if ((Object)(object)(sc_zumbi)(object)array[i].GetComponent(typeof(sc_zumbi)) != (Object)null && ((sc_zumbi)(object)array[i].GetComponent(typeof(sc_zumbi))).state != -1)
				{
					((sc_zumbi)(object)array[i].GetComponent(typeof(sc_zumbi))).setkillerid(shooterid);
					array[i].NetView().RPC("setkillerid", (RPCMode)0, new object[1] { shooterid });
					array[i].NetView().RPC("forcar", (RPCMode)2, new object[1] { (array[i].transform.position - ((Component)this).transform.position) * 60f * push * num2 });
				}
				if ((Object)(object)(sc_player)(object)array[i].GetComponent(typeof(sc_player)) != (Object)null)
				{
					array[i].NetView().RPC("spillblood", (RPCMode)2, new object[0]);
				}
				Network.Instantiate((Object)(object)blood_particle, array[i].transform.position, Quaternion.LookRotation(array[i].transform.position - ((Component)this).transform.position), 3);
			}
		}
		return result;
	}

	public void Main()
	{
	}
}
