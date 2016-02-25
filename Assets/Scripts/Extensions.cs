using UnityEngine;
using System.Collections;

public static class ExtensionMethods
{
    public static Vector2 XZ(this Vector3 vec)
    {
        return new Vector2(vec.x, vec.z);
    }

    public static Vector2 XY(this Vector3 vec)
    {
    	return new Vector2(vec.x, vec.y);
    }

    public static Vector3 XYZ(this Vector2 vec, float z)
    {
    	return new Vector3(vec.x, vec.y, z);
    } 
	
    public static Vector3 XSetYZ(this Vector2 vec, float y)
    {
        return new Vector3(vec.x, y, vec.y);  
    }
	public static float DistanceSqr(this Vector2 p1, Vector2 p2)
	{
		float distX = p1.x - p2.x;
		float distY = p1.y - p2.y;

		return (distX * distX) + (distY * distY);
	}
}