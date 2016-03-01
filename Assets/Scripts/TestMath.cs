using UnityEngine;
using System.Collections;

public class TestMath : MonoBehaviour {

    public Transform start;
    public Transform end;
    public Transform marker;
    public Transform mouseMarker;
    // Use this for initialization
	void Start ()
    {
	
	}
	
	// Update is called once per frame
	void Update ()
    {
        Debug.Log(MyMath.LeftOfLine(start.position.XY(), end.position.XY(), mouseMarker.position.XY()));
        mouseMarker.position = Controls.ClickedPosition().XYZ(mouseMarker.position.z);
    }
}
