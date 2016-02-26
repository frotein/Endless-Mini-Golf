using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BallHitter : MonoBehaviour {
    
    public Transform ball; // The Ball Object
    public Transform powerLine; // the Object that has the visual effect for the hits power
    public Transform landSpot;
    public float maxPower, minPower;
    Vector2 powerAndDirection;
    bool aiming,hit;
    Rigidbody2D ballRB;
    Vector2 startPos, endPos;
    public List<Vector2> positions;    
    int waitFrames;
    // Use this for initialization
	void Start ()
    {
        aiming = false;
        hit = false;
        ballRB = ball.GetComponent<Rigidbody2D>();
        positions = new List<Vector2>();
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (ballRB.velocity.magnitude == 0) // if the ball is not moving...
        {
            if (Controls.Clicked()) // if you click on the screen ...
            {
                powerLine.gameObject.SetActive(true); // turn on the power line
                aiming = true; // say we are aiming
            }

            if (aiming) // if we are aiming
            {
               

                if (Controls.Released()) // if we release our click
                {
                    ShootBall(); // shoot the ball
                    aiming = false; // say we are no longer aiming
                    powerLine.gameObject.SetActive(false); // turn off power line
                }

                DrawPowerLine(); // draw the power line
            }

           

            if (waitFrames > 0)
                waitFrames--;
            else
            {
                // debug code for calculating trajectory
                landSpot.position = CalculateStopPosition().XYZ(landSpot.position.z);
                if (hit)
                {
                    positions.Add((Vector2)ball.position);
                    float totalDist = 0;
                    for (int i = 1; i < positions.Count; i++)
                    {
                        totalDist += Vector2.Distance(positions[i], positions[i - 1]);
                    }

                    
                }
                hit = false;        
            }
            // end of debug code
        }

        Controls.SetTouchCount(); // set touch count so mobile controls work
       
	}

    // Debug function, real one will be used in GenerateCourse
    public Vector2 CalculateStopPosition()
    {
        float magnitudeSquared = powerAndDirection.magnitude * powerAndDirection.magnitude;
        float drag = ballRB.drag;
        float distance = (magnitudeSquared * (2 / drag)) - (.3f / drag);
        Vector2 pos = (Vector2)ball.position;
        Vector2 dir = powerAndDirection.normalized;
        List<Vector2> hitPositions = new List<Vector2>();
        bool hitSomething = true;
        
        while(hitSomething)
        {
            hitSomething = false;
            RaycastHit2D[] rcHits = Physics2D.CircleCastAll(pos, ball.GetComponent<CircleCollider2D>().radius , dir, distance, 1 << 8);
            
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
                    i = rcHits.Length;
                }
            }              
        }
            
        
        return pos + dir * distance;
    }


    public void DrawPowerLine() // Draws the power line from the ball to the clicked positin with min and max lengths
    {
        Vector2 mousePos = Controls.ClickedPosition();
        Vector2 ballPos = (Vector2)(ball.transform.position);
        float dist = Vector2.Distance(mousePos, ballPos);
        Vector2 dir = (mousePos - ballPos).normalized;

        if (dist > maxPower)
        {
            mousePos = ballPos + dir * maxPower;
            dist = maxPower; 
        }
        else
        {
            if(dist < minPower)
            {
                mousePos = ballPos + dir * minPower;
                dist = minPower;
            }
        }

        Vector2 mid = (mousePos - ballPos) * .5f + ballPos;
        
        powerLine.position = mid.XYZ(powerLine.position.z);
        powerLine.up = dir;
        powerLine.localScale = new Vector3(powerLine.localScale.x, dist, powerLine.localScale.z);
        powerAndDirection = dir * dist;
    }

    public void ShootBall() // shoots the ball based on the power set in draw power line
    {
        positions.Clear();
        ballRB.AddForce(powerAndDirection * 100 * powerAndDirection.magnitude);
        positions.Add((Vector2)ball.position);
        hit = true;
        waitFrames = 3;
    }

   
}
