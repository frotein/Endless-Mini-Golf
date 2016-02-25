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
    bool showedDist;
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
        if (ballRB.velocity.magnitude == 0)
        {
            if (Controls.Clicked())
            {
                powerLine.gameObject.SetActive(true);
                aiming = true;
            }
            if (aiming)
            {
                DrawPowerLine();

                if (Controls.Released())
                {
                    ShootBall();
                    aiming = false;
                    powerLine.gameObject.SetActive(false);
                }
            }
            endPos = (Vector2)ball.position;

            // Debug.Log(Vector2.Distance(startPos, endPos) + " : " + powerAndDirection.magnitude * powerAndDirection.magnitude);
            landSpot.position = CalculateStopPosition().XYZ(landSpot.position.z);

            if (waitFrames > 0)
                waitFrames--;
            else
            {
                if(hit)
                {
                    positions.Add((Vector2)ball.position);
                    float totalDist = 0;
                    for (int i = 1; i < positions.Count; i++)
                    {
                        totalDist += Vector2.Distance(positions[i], positions[i - 1]);
                    }

                    //Debug.Log(powerAndDirection.magnitude  + " : " + totalDist);
                }
                hit = false;        
            }
        }
       
	}
    public Vector2 CalculateStopPosition()
    {
        float magnitudeSquared = powerAndDirection.magnitude * powerAndDirection.magnitude;
        float distance = (magnitudeSquared + (magnitudeSquared - 1) + .7f); ;
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
    public void DrawPowerLine()
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

    public void ShootBall()
    {
        positions.Clear();
        ballRB.AddForce(powerAndDirection * 100 * powerAndDirection.magnitude);
        positions.Add((Vector2)ball.position);
        showedDist = false;
        hit = true;
        waitFrames = 3;
    }

   
}
