using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class GenerateCourse : MonoBehaviour {

    public Transform ball;
    public Transform hole;
    public Transform ballStart;
    public Transform ground;
    public Transform wallsPool;
    public LineRenderer movementLine;
    public float minPower;
    public float maxPower;
    public int bounces;

    public bool symetrical;
    PolyMesh groundMesh;
    PolygonCollider2D groundCollider;
    Rigidbody2D ballRB;
    List<Vector2> positions;
    Vector2 powerAndDirection;
    public Transform[] testPoints;
    public Transform[] back2Points;
    List<BouncePoint> bouncePoints;
    public float maxDirectionX;
    // Use this for initialization
	void Start ()
    {
        ballRB = ball.GetComponent<Rigidbody2D>();
        positions = new List<Vector2>();
        groundMesh = ground.GetComponent<PolyMesh>();
        groundCollider = ground.GetComponent<PolygonCollider2D>();
        groundMesh.keyPoints.Clear();
        
        Vector2[] pts = new Vector2[testPoints.Length];
        int i = 0;
        foreach (Transform point in testPoints)
        {
            groundMesh.keyPoints.Add(point.position);
            pts[i] = (Vector2)point.position;
            i++;
        }
        groundMesh.BuildMesh();
        groundCollider.points = pts;
     }

	// Update is called once per frame
	void Update ()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            GenerateHole();
            int i = 0;
            movementLine.SetVertexCount(positions.Count);
            foreach (Vector2 point in positions)
            {
                //Debug.Log(point);
                movementLine.SetPosition(i, point.XYZ(movementLine.transform.position.z));

                i++;
            }

        }

    }
    // all encompasing generate hole function
    public void GenerateHole()
    {
        PlaceBall();
        bouncePoints = new List<BouncePoint>();
        powerAndDirection = GenerateRandomDirectionVector() * Mathf.Lerp(minPower, maxPower, Random.value);
        TurnOffAllWalls();
        CalculateStopPosition();
        for(int i = 0; i < bounces;i++)
        {
            PlaceTemporaryWall(AddBouncePoint());
            CalculateStopPosition();
        }


        PlaceHole();
       

       // GenerateGroundAndWalls();
    }

    // place the ball at the beginning
    void PlaceBall()
    {
        ball.GetComponent<Rigidbody2D>().velocity = new Vector2(0, 0);
        ball.position = ballStart.position.XY().XYZ(ball.position.z);
    }

    // place the hole at the end
    void PlaceHole()
    {
        hole.position = positions[positions.Count - 1].XYZ(hole.position.z);
    }
    void TurnOffAllWalls()
    {
        for (int i = 0; i < wallsPool.childCount; i++)
        {
            wallsPool.GetChild(i).gameObject.SetActive(false);
        }
    }
    void MakeGround(List<Vector2> corners)
    {
        groundMesh.keyPoints.Clear();

        Vector2[] pts = new Vector2[corners.Count];
        int i = 0;
        foreach (Vector2 point in corners)
        {
            groundMesh.keyPoints.Add(point);
            pts[i] = point;
            i++;
        }
        groundMesh.BuildMesh();
        groundCollider.points = pts;
    }

    void SetOuterWalls(List<Vector2> points)
    {

        for (int i = 0; i < points.Count; i++)
        {
            Transform wall = wallsPool.GetChild(i);
            wall.gameObject.SetActive(true);
            Vector2 start = points[i];
            Vector2 end = points[(i + 1) % points.Count];
            Vector2 middle = (end - start) * .5f + start;
            Vector2 dir = (end - start).normalized;
            Vector2 perpDir = new Vector2(dir.y, -dir.x);
            float distance = Vector2.Distance(start, end);
            wall.localScale = new Vector3(wall.localScale.x, distance + .15f, wall.localScale.z);
            wall.position = middle.XYZ(wall.position.z);
            wall.right = perpDir.XYZ(wall.right.z);

        }
    }
    // minimum of 4 points
    void GenerateGroundAndWalls()
    {
        List<Vector2> rightEndPoints = new List<Vector2>();

        List<Vector2> leftEndPoints = new List<Vector2>();
        leftEndPoints.Add((Vector2)back2Points[0].position);
        rightEndPoints.Add((Vector2)back2Points[1].position);
        BothSidesEndPoints endP;
        for (int i = 0; i < positions.Count - 2; i++)
        {
            Vector2 start = positions[i];
            Vector2 end = positions[i + 1];

        }

        endP = SetBack(positions[positions.Count - 2], positions[positions.Count - 1]);
        leftEndPoints.Add(endP.leftEndPoint);
        rightEndPoints.Add(endP.rightEndPoint);


        List<Vector2> endPoints = new List<Vector2>();
        endPoints.AddRange(rightEndPoints);
        leftEndPoints.Reverse();
        endPoints.AddRange(leftEndPoints);
        MakeGround(endPoints);
        SetOuterWalls(endPoints);
    }

    
    BouncePoint AddBouncePoint() // adds bounce to the last line of the current course line
    {
        Vector2 minMaxBounceSpot = new Vector2(0.4f, 0.7f);
        float t = Random.Range(minMaxBounceSpot.x, minMaxBounceSpot.y);
        Vector2 start = positions[positions.Count - 2];
        Vector2 end = positions[positions.Count - 1];
        Vector2 position = (end - start) * t + start;
        BouncePoint bounceP;
        bounceP.position = position;
        bounceP.direction = GenerateRandomBounceVector();
        bouncePoints.Add(bounceP);
        return bounceP;
    }

    void PlaceTemporaryWall(BouncePoint bp)
    {
        Transform wall = wallsPool.GetChild(bouncePoints.Count - 1);
        wall.gameObject.SetActive(true);
       
        wall.position = bp.position.XYZ(wall.position.z);
        wall.up = bp.direction.XYZ(0);
        wall.position -= wall.right * (wall.localScale.x + ball.GetComponent<CircleCollider2D>().radius);
    }
    Vector2 GenerateRandomDirectionVector()
    {
        return new Vector2(Random.Range(-maxDirectionX,maxDirectionX), 1).normalized;
    }

    Vector2 GenerateRandomBounceVector()
    {
        float bounceRange = 8f;
        //Random.Range(bounceRange, -bounceRange)
        return new Vector2(2,1).normalized;
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

    // generates the two sides of a wall between the line, minimum used if it has multiple points
    BothSidesEndPoints GetSides(Vector2 start, Vector2 end, float min = 0.25f)
    {
        Vector2 RandBackRange = new Vector2(min, 1f);
        Vector2 RandSideRange = new Vector2(0.5f, 2.5f);
        float RandBack = Random.Range(RandBackRange.x, RandBackRange.y);
        bool evenBack = (symetrical) || Random.Range(0f, 1f) > 0.75;
        bool evenSides = (symetrical) || Random.Range(0f, 1f) > 0.65f;
        float distance = Vector2.Distance(start, end);
        Vector2 backCenter = (end - start) * (1 + (RandBack / distance)) + start;
        Vector2 dir = (end - start).normalized;


        Vector2 perpDir1 = new Vector2(dir.y, -dir.x);
        Vector2 perpDir2 = -perpDir1;
        float wallDist = Random.Range(RandSideRange.x, RandSideRange.y);
        Vector2 point1 = backCenter + perpDir1 * wallDist;

        if (!evenBack)
            RandBack = Random.Range(RandBackRange.x, RandBackRange.y);

        if (!evenSides)
            wallDist = Random.Range(RandSideRange.x, RandSideRange.y);

        backCenter = (end - start) * RandBack + start;
        Vector2 point2 = backCenter + perpDir2 * wallDist;
        BothSidesEndPoints endP;
        endP.leftEndPoint = point2;
        endP.rightEndPoint = point1;
        return endP;
    }
    BothSidesEndPoints SetBack(Vector2 start, Vector2 end)
    {
        Vector2 RandBackRange = new Vector2(0.5f, 2.0f);
        Vector2 RandSideRange = new Vector2(1f, 2.5f);
        float RandBack = Random.Range(RandBackRange.x, RandBackRange.y);
        bool evenBack = (symetrical) || Random.Range(0f, 1f) > 0.75;
        bool evenSides = (symetrical) || Random.Range(0f, 1f) > 0.65f;
        float distance = Vector2.Distance(start, end);
        Vector2 backCenter = (end - start) * (1 + (RandBack / distance)) + start;
        Vector2 dir = (end - start).normalized;


        Vector2 perpDir1 = new Vector2(dir.y, -dir.x);
        Vector2 perpDir2 = -perpDir1;
        float wallDist = Random.Range(RandSideRange.x, RandSideRange.y);
        Vector2 point1 = backCenter + perpDir1 * wallDist;

        if (!evenBack)
            RandBack = Random.Range(RandBackRange.x, RandBackRange.y);

        if (!evenSides)
            wallDist = Random.Range(RandSideRange.x, RandSideRange.y);

        backCenter = (end - start) * (1 + (RandBack / distance)) + start;
        Vector2 point2 = backCenter + perpDir2 * wallDist;
        BothSidesEndPoints endP;
        endP.leftEndPoint = point2;
        endP.rightEndPoint = point1;
        return endP;
    }
}


struct BouncePoint
{
    public Vector2 position;
    public Vector2 direction;
}

struct BothSidesEndPoints
{
    public Vector2 rightEndPoint;
    public Vector2 leftEndPoint;
}