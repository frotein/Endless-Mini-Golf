using UnityEngine;
using System.Collections;

// this class contains code relating to physics that should occur when the CENTER of the ball changes surfaces, 
// this is different from ball physics which deals with the actual ball movement physics, and uses the larger ball collider
public class BallPhysicsCenter : MonoBehaviour {


    public GenerateCourse courseGenerator;
    Rigidbody2D physicsBody;
    public float fallInHoleMaximum; // the maximum movement speed to fall in the hole
    // Use this for initialization
    void Start ()
    {
        physicsBody = transform.parent.GetComponent<Rigidbody2D>();
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void OnTriggerStay2D(Collider2D other)
    {

        if (other.sharedMaterial != null) // sets the balls drag based on the what surface the center it is over
        {
            physicsBody.drag = other.sharedMaterial.friction;
        }

        if (other.transform.tag == "Hole") // if the ball is over the hole...
        {
            if (physicsBody.velocity.magnitude < fallInHoleMaximum) // and slow enought ...
            {
                // set, got in hole
                courseGenerator.GenerateHole();
            }
        }
    }
}
