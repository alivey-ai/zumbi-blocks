using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_plane : MonoBehaviour
{
	public float antigrav;

	public float needforspeed;

	public Vector3 startimpulse;

	public Vector3 startimpulseglobal;

	public float jetstr;

	public float lift;

	public float leme;

	public float mintolift;

	public float mintoleme;

	public Transform wingpos;

	public Transform lemepos;

	public Transform[] flap;

	public float[] flapstr;

	public string[] flapcontrol;

	public bool separador___________________;

	public float needspeedcoef;

	public float antigravcoef;

	public float speedcoef;

	public float liftspeed;

	public float lemespeed;

	public float collisionforce;

	public sc_plane()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		antigrav = 1f;
		needforspeed = 50f;
		startimpulse = Vector3.zero;
		startimpulseglobal = Vector3.zero;
		jetstr = 1f;
		lift = 1f;
		leme = 1f;
		mintolift = 0.1f;
		mintoleme = 0.1f;
		separador___________________ = true;
		speedcoef = 1f;
		liftspeed = 1f;
		lemespeed = 1f;
	}

	public void OnCollisionEnter(Collision collision)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		Vector3 relativeVelocity = collision.relativeVelocity;
		if (!(relativeVelocity.magnitude <= 1f))
		{
			float num = collisionforce;
			Vector3 relativeVelocity2 = collision.relativeVelocity;
			collisionforce = num + relativeVelocity2.magnitude;
		}
	}

	public void Start()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		((Component)this).GetComponent<Rigidbody>().AddForce(((Component)this).transform.TransformDirection(startimpulse), (ForceMode)1);
		((Component)this).GetComponent<Rigidbody>().AddForce(startimpulseglobal, (ForceMode)1);
	}

	public void Update()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0248: Unknown result type (might be due to invalid IL or missing references)
		//IL_0252: Unknown result type (might be due to invalid IL or missing references)
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_028e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0299: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = default(Vector3);
		float num = 0f;
		((Component)this).GetComponent<Rigidbody>().AddForce(((Component)this).transform.forward * jetstr * Input.GetAxis("Jump") * Time.deltaTime, (ForceMode)1);
		antigravcoef = ((Component)this).transform.up.y;
		antigravcoef = Mathf.Abs(antigravcoef);
		val = ((Component)this).transform.InverseTransformPoint(((Component)this).transform.position + ((Component)this).GetComponent<Rigidbody>().velocity);
		liftspeed = (0f - val.y) * lift;
		lemespeed = (0f - val.x) * leme;
		val.Normalize();
		speedcoef = Mathf.Abs(val.z);
		Vector3 velocity = ((Component)this).GetComponent<Rigidbody>().velocity;
		needspeedcoef = velocity.magnitude / needforspeed;
		if (!(needspeedcoef >= 0f))
		{
			needspeedcoef = 0f;
		}
		if (!(needspeedcoef <= 1f))
		{
			needspeedcoef = 1f;
		}
		num = speedcoef * needspeedcoef;
		for (int i = 0; i < flap.Length; i++)
		{
			((Component)this).GetComponent<Rigidbody>().AddForceAtPosition(flap[i].up * speedcoef * needspeedcoef * flapstr[i] * Time.deltaTime * Input.GetAxis(flapcontrol[i]), flap[i].position, (ForceMode)1);
		}
		if (liftspeed > mintolift || !(liftspeed >= 0f - mintolift))
		{
			((Component)this).GetComponent<Rigidbody>().AddForceAtPosition(wingpos.up * liftspeed * Time.deltaTime, wingpos.position, (ForceMode)1);
		}
		if (lemespeed > mintoleme || !(lemespeed >= 0f - mintoleme))
		{
			((Component)this).GetComponent<Rigidbody>().AddForceAtPosition(lemepos.right * lemespeed * Time.deltaTime, lemepos.position, (ForceMode)1);
		}
		((Component)this).GetComponent<Rigidbody>().AddForce(wingpos.TransformDirection(-Physics.gravity) * ((Component)this).GetComponent<Rigidbody>().mass * antigrav * Time.deltaTime * num, (ForceMode)1);
	}

	public void OnGUI()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		GUI.color = Color.red;
		Rect val = new Rect(10f, (float)(Screen.height - 60), 500f, 20f);
		Vector3 velocity = ((Component)this).GetComponent<Rigidbody>().velocity;
		GUI.Label(val, " speed = " + velocity.magnitude);
		Rect val2 = new Rect(10f, (float)(Screen.height - 40), 500f, 20f);
		Vector3 velocity2 = ((Component)this).GetComponent<Rigidbody>().velocity;
		GUI.Label(val2, " Km/h = " + velocity2.magnitude * 0.75f * 3.6f);
		GUI.Label(new Rect(10f, (float)(Screen.height - 20), 200f, 20f), "speed de pouso= " + ((Component)this).GetComponent<Rigidbody>().velocity.y);
		GUI.Label(new Rect(110f, 130f, 40f, 40f), "0");
		GUI.color = Color.red;
		GUI.Label(new Rect(10f, 10f, 40f, 40f), "dir");
		GUI.Label(new Rect(30f, 10f, 40f, 40f), "vel");
		GUI.Label(new Rect(50f, 10f, 40f, 40f), "ali");
		GUI.Label(new Rect(65f, 10f, 40f, 40f), "sus");
		GUI.Label(new Rect(90f, 10f, 40f, 40f), "atr");
		GUI.VerticalSlider(new Rect(10f, 40f, 20f, 100f), speedcoef, 1f, 0f);
		GUI.VerticalSlider(new Rect(30f, 40f, 20f, 100f), needspeedcoef, 1f, 0f);
		GUI.VerticalSlider(new Rect(50f, 40f, 20f, 100f), antigravcoef, 1f, 0f);
		GUI.VerticalSlider(new Rect(70f, 40f, 20f, 100f), speedcoef * needspeedcoef, 1f, 0f);
		GUI.VerticalSlider(new Rect(90f, 40f, 20f, 200f), liftspeed, 3f, -3f);
	}

	public void Main()
	{
	}
}
