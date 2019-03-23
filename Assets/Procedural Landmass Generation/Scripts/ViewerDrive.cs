using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewerDrive : MonoBehaviour
{
    public float rotationSpeed = 3;
    public float movementSpeed = 5;
    public float boostMultiplier = 3f;
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

   void FixedUpdate ()
    {
        float moveHorizontal = Input.GetAxis ("Mouse X");
        float moveVertical = Input.GetAxis ("Vertical");

        if (Input.GetKey(KeyCode.LeftShift))
            moveVertical *= boostMultiplier;

        Vector3 movement = transform.forward * moveVertical;
        Vector3 rotation = new Vector3(0.0f, moveHorizontal, 0.0f);

        rb.AddForce (movement * movementSpeed);
        rb.AddTorque(rotation * rotationSpeed);
    }
}
