using UnityEngine;
using System.Collections;


// a static class that unifies controls for both PC and mobile
public static class Controls
{
    static int touchCount;
    public static bool Clicked()
    {

        touchCount = Input.touchCount;
        // if its playihng on a mobile device, use touchEvents, otherwise use mouse
        if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            return (touchCount == 0 && Input.touchCount == 1);
		else
            return Input.GetMouseButtonDown(0);


        
	}

	public static Vector2 ClickedPosition() // must make sure clicked is true
	{
		Vector2 screenPosition;

        if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            screenPosition = Input.touches[0].position;
		else
            screenPosition = Input.mousePosition;

        return (Vector2)Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x,screenPosition.y,Camera.main.nearClipPlane)); 
	}

	public static bool Released()
	{
        touchCount = Input.touchCount;

        if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            return (Input.touchCount == 0); 
        else
            return Input.GetMouseButtonUp(0);

        
	}

	public static void SetTouchCount() // must be called at the end of the frame
	{
		touchCount = Input.touchCount;
	}
	
}
