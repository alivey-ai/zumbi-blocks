using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_meelegun : MonoBehaviour
{
	public int dmg;

	public int dmgrnd;

	public string holdanim;

	public string[] atkanim;

	public float[] atkspeed;

	public int[] endtime;

	public float[] hittime;

	public float range;

	public bool blunt;

	public sc_meelegun()
	{
		dmgrnd = 10;
		range = 2f;
	}

	public void Main()
	{
	}
}
