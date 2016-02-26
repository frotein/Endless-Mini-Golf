using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class GenerateCourse : MonoBehaviour {

    public Transform ball;
    public Transform wallPrefab;
    public float minPower;
    public int bounces;

    Rigidbody2D ballRB;
    List<Vector2> positions;
    Vector2 powerAndDirection;
    // Use this for initialization
	void Start ()
    {
        ballRB = ball.GetComponent<Rigidbody2D>();
        positions = new List<Vector2>();
    }
	
	// Update is called once per frame
	void Update () {
	
	}


    public void CalculateStopPosition() // returns the calculated positions in the positions List
    {
        positions.Clear();
        float magnitudeSquared = powerAndDirection.magnitude * powerAndDirection.magnitude;
        float drag = ballRB.drag;
        float distance = (magnitudeSquared * (2 / drag)) - (.3f / drag);
        Vector2 pos = (Vector2)ball.position;
        Vector2 dir = powerAndDirection.normalized;
        List<Vector2> hitPositions = new List<Vector2>();
        bool hitSomething = true;
        positions.Add(ball.position);
        while (hitSomething)
        {
            hitSomething = false;
            RaycastHit2D[] rcHits = Physics2D.CircleCastAll(pos, ball.GetComponent<CircleCollider2D>().radius, dir, distance, 1 << 8);

            for (int i = 0; i < rcHits.Length; i++)
            {
                RaycastHit2D rcHit = rcHits[i];
                if (!hitPositions.Contains(rcHit.centroid))
                {
                    hitSomething = true;
                    dir = MyMath.Reflect(dir, rcHit.normal).normalized;
                    pos = rcHit.centroid;
                    distance -= rcHit.distance;
                    hitPositions.Add(rcHit.centroid);
                    positions.Add(rcHit.centroid);
                    i = rcHits.Length;
                }
            }
        }


        positions.Add(pos + dir * distance);
    }

    
}
