using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_consumable : MonoBehaviour
{
	public float life;

	public bool callsuply;

	public bool stopbleeding;

	public string holdanim;

	public string consumeanim;

	public int consumetime;

	public bool rotarm;

	public sc_consumable()
	{
		life = 5f;
		consumetime = 150;
		rotarm = true;
	}

	public void Main()
	{
	}
}
