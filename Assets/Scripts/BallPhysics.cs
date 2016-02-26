using UnityEngine;
using System.Collections;

public class BallPhysics : MonoBehaviour {

    public BallHitter hitter;
    Rigidbody2D physicsBody;
    public float stopVelocity;
   
    
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

    public void OnCollisionEnter2D(Collision2D col)
    {
        hitter.positions.Add((Vector2)transform.position);
    }
}
