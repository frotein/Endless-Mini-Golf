using UnityEngine;
using System.Collections;

public class BallPhysics : MonoBehaviour {

    public BallHitter hitter;
    Rigidbody2D physicsBody;
    public float stopVelocity;
    public float fallInHoleMaximum; // the maximum movement speed to fall in the hole
    
    // Use this for initialization
	void Start ()
    {
        physicsBody = transform.GetComponent<Rigidbody2D>();
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (physicsBody.velocity.magnitude <= stopVelocity)
            physicsBody.velocity = Vector2.zero;

	}

    public void OnTriggerStay2D(Collider2D other)
    {
        if(other.sharedMaterial != null)
        {
            physicsBody.drag = other.sharedMaterial.friction;
        }

        if(other.transform.tag == "Hole")
        {
            if(physicsBody.velocity.magnitude < fallInHoleMaximum)
            {

            }
        }
    }

    public void OnCollisionEnter2D(Collision2D col)
    {
        hitter.positions.Add((Vector2)transform.position);
    }
}
