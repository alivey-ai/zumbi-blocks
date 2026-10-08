using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[AddComponentMenu("Camera-Control/Mouse Look")]
public class MouseLook : MonoBehaviour
{
	public enum RotationAxes
	{
		MouseXAndY = 0,
		MouseX = 1,
		MouseY = 2
	}

	public RotationAxes axes;

	public float sensitivityX = 15f;

	public float sensitivityY = 15f;

	public float minimumX = -360f;

	public float maximumX = 360f;

	public float minimumY = -60f;

	public float maximumY = 60f;

	private float rotationY;

	private void Update()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		if (axes == RotationAxes.MouseXAndY)
		{
			float num = ((Component)this).transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivityX;
			rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
			rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);
			((Component)this).transform.localEulerAngles = new Vector3(0f - rotationY, num, 0f);
		}
		else if (axes == RotationAxes.MouseX)
		{
			((Component)this).transform.Rotate(0f, Input.GetAxis("Mouse X") * sensitivityX, 0f);
		}
		else
		{
			rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
			rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);
			((Component)this).transform.localEulerAngles = new Vector3(0f - rotationY, ((Component)this).transform.localEulerAngles.y, 0f);
		}
	}

	private void Start()
	{
		if (((Object)(object)((Component)this).GetComponent<Rigidbody>() != null))
		{
			((Component)this).GetComponent<Rigidbody>().freezeRotation = true;
		}
	}
}
