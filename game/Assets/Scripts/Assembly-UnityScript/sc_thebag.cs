using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_thebag : MonoBehaviour
{
	public GameObject matchcontroller;

	public int myprobmin;

	public int myprobmax;

	public Transform[] myitemsrand;

	public int[] myitensprob;

	public sc_thebag()
	{
		myprobmin = 1;
		myprobmax = 8;
	}

	[RPC]
	public void openbag()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Expected O, but got Unknown
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_0271: Unknown result type (might be due to invalid IL or missing references)
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0282: Expected O, but got Unknown
		int num = 0;
		int num2 = 0;
		Transform val = null;
		Transform val2 = null;
		Vector3 val3 = default(Vector3);
		int num3 = 0;
		int num4 = default(int);
		if (!((Component)this).NetView().isMine)
		{
			return;
		}
		if ((Object)(object)matchcontroller == (Object)null)
		{
			matchcontroller = GameObject.Find("matchcontroller(Clone)");
		}
		if ((Object)(object)matchcontroller != (Object)null)
		{
			for (int i = 0; i < 7; i++)
			{
				val3 = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0f, 0.5f), Random.Range(-0.5f, 0.5f));
				num = Random.Range(0, myprobmax);
				if (num < myprobmin)
				{
					num2 = Random.Range(0, ((sc_gamecontrol)(object)((Component)((sc_matchcontroller)(object)matchcontroller.GetComponent(typeof(sc_matchcontroller))).gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemtosell.Length);
					val2 = ((sc_matchcontroller)(object)matchcontroller.GetComponent(typeof(sc_matchcontroller))).grounditens[((sc_gamecontrol)(object)((Component)((sc_matchcontroller)(object)matchcontroller.GetComponent(typeof(sc_matchcontroller))).gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemtosell[num2]];
					val = (Transform)Network.Instantiate((Object)(object)val2, ((Component)this).transform.position + val3, val2.rotation, 5);
					((Behaviour)(sc_updateposition)(object)((Component)val).GetComponent(typeof(sc_updateposition))).enabled = false;
					((Behaviour)(sc_sendpositionupdate)(object)((Component)val).GetComponent(typeof(sc_sendpositionupdate))).enabled = true;
					continue;
				}
				num3 = 0;
				for (num4 = 0; num4 < myitemsrand.Length; num4++)
				{
					num3 += myitensprob[num4];
				}
				num2 = Random.Range(0, num3);
				num3 = 0;
				for (num4 = 0; num4 < myitemsrand.Length; num4++)
				{
					if (num2 >= num3 && num2 < num3 + myitensprob[num4])
					{
						num2 = num4;
						break;
					}
					num3 += myitensprob[num4];
				}
				val3.y += 0.3f;
				val = (Transform)Network.Instantiate((Object)(object)myitemsrand[num2], ((Component)this).transform.position + val3, myitemsrand[num2].rotation, 5);
				((Behaviour)(sc_updateposition)(object)((Component)val).GetComponent(typeof(sc_updateposition))).enabled = false;
				((Behaviour)(sc_sendpositionupdate)(object)((Component)val).GetComponent(typeof(sc_sendpositionupdate))).enabled = true;
			}
		}
		Network.Destroy(((Component)this).NetView().viewID);
	}

	public void Start()
	{
	}

	public void Update()
	{
	}

	public void Main()
	{
	}
}
