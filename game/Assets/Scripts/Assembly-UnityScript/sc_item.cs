using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_item : MonoBehaviour
{
	public string nome;

	public Transform prefab;

	public int tipo;

	public Texture sprite;

	public int holdpos;

	public int itemnum;

	public int price;

	public int sellprice;

	public sc_item()
	{
		holdpos = -1;
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
