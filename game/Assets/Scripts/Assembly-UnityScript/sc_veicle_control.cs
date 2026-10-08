using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_veicle_control : MonoBehaviour
{
	public float engine;

	public float vertical;

	public float horizontal;

	public float roll;

	public void Start()
	{
	}

	public void Update()
	{
		if (Input.GetKey((KeyCode)306))
		{
			engine = -1f;
		}
		if (Input.GetKey((KeyCode)32))
		{
			engine = 1f;
		}
		if (!Input.GetKey((KeyCode)306) && !Input.GetKey((KeyCode)32))
		{
			engine = 0f;
		}
		vertical = Input.GetAxis("Vertical");
		horizontal = Input.GetAxis("Horizontal");
		roll = Input.GetAxis("PlaneRot");
	}

	public void Main()
	{
	}
}
