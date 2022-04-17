using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
	// Start is called before the first frame update
	void Start()
	{

		

	}

	// Update is called once per frame
	void Update()
	{
		Rigidbody rb = GetComponent<Rigidbody>();

		Vector3 move = Vector3.zero;

		if (Input.GetKey(KeyCode.W))
			move = transform.forward;
		if (Input.GetKey(KeyCode.A))
			move = -transform.right;
		if (Input.GetKey(KeyCode.S))
			move = -transform.forward;
		if (Input.GetKey(KeyCode.D))
			move = transform.right;

		//rb.AddForce(move);
		transform.position += move * Time.deltaTime * 1000;
	}
}
