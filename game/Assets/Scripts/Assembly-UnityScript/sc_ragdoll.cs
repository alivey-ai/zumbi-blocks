using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_ragdoll : MonoBehaviour
{
	public float conter;

	public int lifetime;

	public Transform[] parts;

	public int bodycount;

	public GameObject[] bodes;

	public Transform[] peles;

	public Material[] skincolor;

	public int skin;

	public Transform[] camisas;

	public Material[] camisacolor;

	public int camisanum;

	public Transform[] headmesh;

	public Transform headpos;

	public ParticleSystem bloodfont;

	public sc_ragdoll()
	{
		lifetime = 1000;
		bodycount = 10;
	}

	public void Start()
	{
		conter = 0f;
		bodes = GameObject.FindGameObjectsWithTag("bode");
		if (bodes.Length > bodycount)
		{
			int num = Random.Range(0, bodes.Length);
			if ((Object)(object)bodes[num] == (Object)(object)((Component)this).gameObject)
			{
				num++;
				if (num > bodes.Length - 1)
				{
					num = 0;
				}
			}
			Object.Destroy((Object)(object)bodes[num]);
		}
		for (int i = 0; i < peles.Length; i++)
		{
			((Component)peles[i]).GetComponent<Renderer>().material = skincolor[skin];
		}
		for (int i = 0; i < camisas.Length; i++)
		{
			((Component)camisas[i]).GetComponent<Renderer>().material = camisacolor[camisanum];
		}
	}

	public void Update()
	{
		conter += 100f * Time.deltaTime;
		if (!(conter <= 500f))
		{
			{ var __em = bloodfont.emission; __em.rateOverTime = 0f; }
		}
		if (!(conter <= (float)lifetime))
		{
			Object.Destroy((Object)(object)((Component)this).gameObject);
		}
	}

	public void Main()
	{
	}
}
