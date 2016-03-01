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
}
