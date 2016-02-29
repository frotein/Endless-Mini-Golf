using UnityEngine;
using System.Collections;

public class RollingRock : MonoBehaviour
{
	public float rollTorque;

	void FixedUpdate()
	{
		GetComponent<Rigidbody>().AddTorque(0, 0, rollTorque, ForceMode.Acceleration);
	}
}
