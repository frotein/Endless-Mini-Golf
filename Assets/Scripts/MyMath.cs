using UnityEngine;
using System.Collections;

public static class MyMath
{

	public static Vector2 Reflect(Vector2 vector, Vector2 normal)
    {
        return vector - 2 * Vector2.Dot(vector, normal) * normal;
    }

    // returns the distance from a point to a line (desginnated by two points)
    // returns position or negative depending on what side the point is on
    public static bool LeftOfLine(Vector2 start, Vector2 end, Vector2 point)
    {
        if (start.x != end.x && start.y != end.y)
        {
            float tx = (point.x - start.x) / (end.x - start.x);
            float ty = (point.y - start.y) / (end.y - start.y);
            Vector2 dir = (end - start).normalized;
            return tx > ty;
            // find the t between tx and ty from the perp point; 
        }
        else
        {
            if (start.x == end.x)
                return point.x < start.x;
            else
                return point.y < start.y;
        }
    }

    public static Vector2 LineIntersectionPoint(Vector2 ps1, Vector2 pe1, Vector2 ps2,
   Vector2 pe2)
    {
        // Get A,B,C of first line - points : ps1 to pe1
        float A1 = pe1.y - ps1.y;
        float B1 = ps1.x - pe1.x;
        float C1 = A1 * ps1.x + B1 * ps1.y;

        // Get A,B,C of second line - points : ps2 to pe2
        float A2 = pe2.y - ps2.y;
        float B2 = ps2.x - pe2.x;
        float C2 = A2 * ps2.x + B2 * ps2.y;

        // Get delta and check if the lines are parallel
        float delta = A1 * B2 - A2 * B1;
        if (delta == 0)
            throw new System.Exception("Lines are parallel");

        // now return the Vector2 intersection point
        return new Vector2(
            (B2 * C1 - B1 * C2) / delta,
            (A1 * C2 - A2 * C1) / delta
        );
    }
}
