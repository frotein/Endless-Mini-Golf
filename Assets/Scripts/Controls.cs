using UnityEngine;
using System.Collections;


// a static class that unifies controls for both PC and mobile
public static class Controls
{
    static int touchCount;
    static Vector2 worldPosition;
    public static bool Clicked()
    {

        // if its playihng on a mobile device, use touchEvents, otherwise use mouse
        if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            return (touchCount == 0 && Input.touchCount == 1);
		else
            return Input.GetMouseButtonDown(0);


        
	}

	public static Vector2 ClickedPosition() // must make sure clicked is true if on mobile
	{
		Vector2 screenPosition = Vector2.zero;

        if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
        {
            if (Input.touchCount > 0)
            {
                screenPosition = Input.touches[0].position;
            }
        }
        else
            screenPosition = Input.mousePosition;

        worldPosition = (Vector2)Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, Camera.main.nearClipPlane));
        return worldPosition;
	}

	public static bool Released()
	{
        if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            return (Input.touchCount == 0) && touchCount == 1; 
        else
            return Input.GetMouseButtonUp(0);

        
	}

	public static void SetTouchCount() // must be called at the end of the frame
	{
		touchCount = Input.touchCount;
	}
	
}
