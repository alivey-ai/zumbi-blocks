using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_life : MonoBehaviour
{
	public float quantia;

	public void Start()
	{
	}

	[RPC]
	public void hurtme(float dmg)
	{
		if ((Object)(object)(sc_zumbi)(object)((Component)this).GetComponent(typeof(sc_zumbi)) != (Object)null)
		{
			if (((sc_zumbi)(object)((Component)this).GetComponent(typeof(sc_zumbi))).state != -1)
			{
				quantia -= dmg;
			}
			if ((Object)(object)((sc_zumbi)(object)((Component)this).GetComponent(typeof(sc_zumbi))).alvo == (Object)null)
			{
				quantia -= dmg * 2f;
			}
		}
		else
		{
			quantia -= dmg;
		}
	}

	public void Main()
	{
	}
}
