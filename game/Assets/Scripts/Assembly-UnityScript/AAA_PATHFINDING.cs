using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class AAA_PATHFINDING : MonoBehaviour
{
	public GameObject destino;

	public GameObject atual;

	public Transform[] menorcaminho;

	public float menordist;

	public float menordoornum;

	public Transform[] caminhoatual;

	public bool achou;

	public AAA_PATHFINDING()
	{
		menordist = -1f;
		menordoornum = -1f;
	}

	public void search(GameObject area_atual, int cont, int N)
	{
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
		//IL_026d: Unknown result type (might be due to invalid IL or missing references)
		int num = default(int);
		float num2 = default(float);
		((AI_area)(object)area_atual.GetComponent(typeof(AI_area))).jahpassei = true;
		if ((Object)(object)area_atual != (Object)(object)destino)
		{
			if (cont < N)
			{
				for (num = 0; num < ((AI_area)(object)area_atual.GetComponent(typeof(AI_area))).porta.Length; num++)
				{
					if ((Object)(object)((AI_area)(object)area_atual.GetComponent(typeof(AI_area))).porta[num] != (Object)null && (Object)(object)((AI_porta)(object)((Component)((AI_area)(object)area_atual.GetComponent(typeof(AI_area))).porta[num]).GetComponent(typeof(AI_porta))).destino != (Object)null && (Object)(object)(AI_area)(object)((AI_porta)(object)((Component)((AI_area)(object)area_atual.GetComponent(typeof(AI_area))).porta[num]).GetComponent(typeof(AI_porta))).destino.GetComponent(typeof(AI_area)) != (Object)null && !((AI_area)(object)((AI_porta)(object)((Component)((AI_area)(object)area_atual.GetComponent(typeof(AI_area))).porta[num]).GetComponent(typeof(AI_porta))).destino.GetComponent(typeof(AI_area))).jahpassei)
					{
						caminhoatual[cont] = ((AI_area)(object)area_atual.GetComponent(typeof(AI_area))).porta[num];
						search(((AI_porta)(object)((Component)((AI_area)(object)area_atual.GetComponent(typeof(AI_area))).porta[num]).GetComponent(typeof(AI_porta))).destino, cont + 1, N);
					}
				}
				((AI_area)(object)area_atual.GetComponent(typeof(AI_area))).jahpassei = false;
			}
			else
			{
				((AI_area)(object)area_atual.GetComponent(typeof(AI_area))).jahpassei = false;
			}
			return;
		}
		achou = true;
		num2 = Vector3.Distance(((Component)this).transform.position, caminhoatual[0].position);
		for (num = 0; num < cont - 1; num++)
		{
			num2 += Vector3.Distance(caminhoatual[num].position, caminhoatual[num + 1].position);
		}
		if (!(num2 >= menordist))
		{
			menordist = num2;
			menordoornum = cont;
			for (num = 0; num < menorcaminho.Length; num++)
			{
				menorcaminho[num] = caminhoatual[num];
			}
		}
		((AI_area)(object)area_atual.GetComponent(typeof(AI_area))).jahpassei = false;
	}

	public void Update()
	{
	}

	public void Main()
	{
	}
}
