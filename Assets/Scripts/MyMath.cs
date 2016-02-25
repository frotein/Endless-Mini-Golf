using UnityEngine;
using System.Collections;

public static class MyMath
{

	public static Vector2 Reflect(Vector2 vector, Vector2 normal)
    {
        return vector - 2 * Vector2.Dot(vector, normal) * normal;
    }
}
