using UnityEngine;

public class TiledSpriteMain : MonoBehaviour 
{
	public TiledSpriteRenderer waterSpriteRenderer;
	public Sprite waterSprite;	
	public TiledSpriteRenderer progressBar;
	
	static readonly float PROGRESS_TIME = 3.0f;
	float progress;
	
	// Use this for initialization
	void Start () 
	{
		Vector2 size = new Vector2(Camera.main.pixelWidth, Camera.main.pixelHeight);
		
		waterSpriteRenderer.SetTiling(size, waterSprite, SpriteAlignment.Center);
		waterSpriteRenderer.flipHorizontal = true;
		
		progress = 0;
	}
	
	// Per-frame update
	void Update()
	{
		progressBar.sharedMaterial.SetFloat("_RightClip", progress);
		progress += Time.deltaTime / PROGRESS_TIME;
		if (progress >= 1) {
			progress = 0;
		}
	}
}
