/**
 * @file
 * @brief TiledSpriteRenderer allows to manage a tiled area as a single object.
 * 
 * TiledSpriteRenderer keeps its size and fills its space with a Sprite tile instead
 * of resizing to match the Sprite's dimensions. To achieve this, it tesselates the 
 * underlying mesh, so there is a size limit involved. This depends on the size of
 * the tile and a few other parameters. However, it will not matter for the most
 * of real world applications.
 *
 * A Tiled Sprite can be adjusted dynamically. Just don't do it very often, because it
 * usually has to regenerate the mesh and that may cause considerable slow downs. If you
 * don't adjust it at run time, it will keep its settings from the editor and will not
 * incur any mesh generation overhead when running the game. 
 *
 * Provided several Tiled Sprites use the same material and texture (atlas), they should 
 * result in one draw call, regardless of other TiledSpriteRenderer settings (except maybe 
 * sorting layer).
 *
 * You can choose from which position the tile is repeated and even flip it horizontally
 * or vertically.
 * 
 * @author Simon
 * @date July 2014
 *
 *
 * UPDATE September 2014 (version 1.1.1):
 *
 * This code has become more robust since the initial release, yet every little thing in here
 * deals with specific issues in Unity 4.5.2. I'll attempt to summarize.
 *
 * A lot of paranoia that can be seen in form of "if (initialised)" and "regenerateMesh = true"
 * is caused by the fact that Unity doesn't call Awake() (or Destroy() for that matter) on
 * inactive objects. Hence there isn't a proper constructor to rely on. Public API calls can 
 * arrive at any time without the underlying mesh data being actually populated yet. And it all
 * becomes a little more messy.
 *
 * Another Unity thing is shallow duplicates. When an object is duplicated (in editor or at run time), 
 * it still references the original mesh data. Sadly, there's no way to know that. Unity doesn't notify 
 * about cloning. All we get to know is that an object was created; origins not disclosed. To deal with 
 * it, a table of active mesh references is kept internally and checked upon every time a new tiled
 * sprite is created. So duplicates are handled properly.
 *
 * However, extra measures don't end here. Texture re-imports provide for a sensitive topic. When the 
 * underlying tile texture changes, we need to at least update UVs (but in reality regenerate the
 * whole mesh because of a possible tile size change). It's tricky to know when that happens. Unity
 * quietly fixes up linked texture and sprite and pretends nothing happened. There are two ways to
 * squeeze the information: 1) Update() is called on any change in the scene in editor, but you 
 * don't get to know what's changed. 2) A custom AssetPostprocessor. The AssetPostprocessor is the 
 * answer. But it reports all changes. We have to check against all Tiled Sprites in the scene to see
 * if they're affected. For these purposes a texture lookup table is kept internally.
 *
 * Still it's not as easy as to compare an old texture reference with a new one. When
 * an AssetPostprocessor imports a texture, it doesn't provide a reference to the texture it replaces.
 * So we know a new texture has been imported, but is it one of those we use? For that we need to check
 * its path... and track renames, moves, and deletions, since paths may change. Luckily, we need to 
 * worry about it only in editor.
 *
 * Yet it's still not enough. What if a scene that contains an object that uses currently imported 
 * texture is not loaded? What then? The object does need to be updated. Well, either we regenerate
 * the mesh every time it loads (expensive) or check the timestamp. Comparing timestamps sounds intriguing, 
 * but accessing hard drive for say 1000 objects could be expensive. FYI, on my old SATA II hard disc
 * with 7200 rpm, one check took between 0.05 ms and 0.1 ms. It's not much, but it adds up. Fortunately,
 * we can bring that down if texture atlases are used by caching the results. And we do. If let's say
 * all objects in the scene use one atlas, it comes down to 1 disc read for all checks. It's bound
 * by number of textures used, which tends to be smaller than number of objects that use them. So 
 * this is taken care of efficiently.
 *
 * Last but not least is the issue of recording prefab instance modifications. Say you have a MonoBehaviour
 * with some fields you want to serialize. You attach it to a GameObject and all is working fine until
 * you make a prefab out of it. You change a serialized field in Start() during the edit phase, for example, 
 * expecting it to stay that way forever, but after hitting play or reloading the scene (even after you saved
 * it), it is reset to an older value. The reason is: it reverts to the last recorded modification from the
 * original prefab, but these are recorded only by the object's Inspector and by Awake() when the object
 * is created for the first time, as far as I know. Changes to serialized fields outside Editor.OnInspectorGUI()
 * or the first Awake() will not propagate automatically! That's where #RecordPrefabDifferences() comes in
 * and takes care of all state that needs to persist between scene loads. Take a look at it if you need 
 * to manipulate Tiled Sprites by a script in editor.
 *
 */

// .NET includes
using System;
using System.Collections.Generic;
using ConditionalAttribute = System.Diagnostics.ConditionalAttribute;

// Unity includes
using UnityEngine;
using UnityEngine.Rendering;

// Custom includes
using CoreLib;
#if UNITY_EDITOR
using AssetLib;
#endif

/**
 * @brief TiledSpriteRenderer is the component that renders a Tiled Sprite. It uses the MeshRenderer
 *	underneath.
 */
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[ExecuteInEditMode]
public class TiledSpriteRenderer : MonoBehaviour
{
#region types

	static public readonly string LOG_TAG = "[TiledSpriteRenderer] ";
	
	
	/**
	 * @brief Maximum number of tile edges allowed in one dimension; equal to number of tiles + 1.
	 *
	 * One Tiled Sprite can generate up to 254*254 (64 516) vertices. That allows for 127
	 * tiles horizontally and 127 tiles vertically.
	 */
	static readonly int EDGE_COUNT_LIMIT_1D = 128;
	
#endregion

#region methods	
	
	// Public interface.
				
	/**
	 * @brief Set all parameters that effect mesh changes at once. Shortcut to the full fledged
	 *	#SetTiling(), without the tile offset.
	 *
	 * @param _size Size of the tiled area.
	 * @param _sprite Tile Sprite.
	 * @param _anchor Position of the "first" tile respective to the tiled area. Custom results
	 *	in Bottom Left (which is the default).
	 *
	 */
	public void SetTiling(Vector2 _size, Sprite _sprite, SpriteAlignment _anchor)
	{
		SetTiling(_size, _sprite, _anchor, new Vector2(0, 0));
	}
	
	/**
	 * @brief Set all parameters that effect mesh changes at once. For other settings, use 
	 *	respective properties.
	 *
	 * @param _size Size of the tiled area.
	 * @param _sprite Tile Sprite.
	 * @param _anchor Position of the "first" tile respective to the tiled area. Custom results
	 *	in Bottom Left (which is the default).
	 * @param _offset Offset of the "first" tile from the _anchor, in -1..1 tile range.
	 *
	 */
	public void SetTiling(Vector2 _size, Sprite _sprite, SpriteAlignment _anchor, Vector2 _offset)
	{
		m_size = _size;
		m_sprite = _sprite;
		m_tileAnchor = _anchor;
		m_tileOffset = _offset;
		
		UpdateTexRef();
		
		ValidateInput();
		UpdateMesh(false);
		UpdateMaterial();
	}
	
	/**
	 * @brief This is called by an associated AssetPostprocessor when an asset is imported.
	 *
	 * If a Tiled Sprite uses a texture found in those assets, it needs to update its UV.
	 *
	 * @param _tex Imported texture.
	 *
	 */
	[Conditional("UNITY_EDITOR")]
	public static void OnAssetsImported(
		string[] _importedAssets, 
		string[] _deletedAssets, 
		string[] _movedToPaths, 
		string[] _movedFromPaths
	)
	{
		var index = GetTexIndex();
		
		// remove deleted references
		
		foreach (string path in _deletedAssets) {
			index.Remove(path);
		}
		
		// update renamed
		
		for (int i = 0; i < _movedFromPaths.Length; ++i) {
			if (index.ContainsKey(_movedFromPaths[i])) {
				string fromPath = _movedFromPaths[i],
					toPath = _movedToPaths[i];
				List<TiledSpriteRenderer> sprites = index[fromPath];		
				index.Remove(fromPath);
				if (index.ContainsKey(toPath)) {
					// This shouldn't really happen, but to be safe...
					index[toPath].AddRange(sprites);
				}
				else {
					index.Add(toPath, sprites);
				}
			}
		}
		
		// update imported
		
		foreach (string path in _importedAssets) {
			if (index.ContainsKey(path)) {
				foreach (TiledSpriteRenderer tiledSprite in index[path]) {
					tiledSprite.UpdateMesh(false);
					tiledSprite.UpdateTexTimestamp();
					tiledSprite.RecordPrefabDifferences();
				}
			}
		}
	}
		
	/**
	 * @brief This is called when a variable changes in the editor. An editor platform
	 *	should have enough processing power to run a full update every time.
	 * 
	 */
	[Conditional("UNITY_EDITOR")]
	public void OnInspectorChange()
	{
		UpdateTexRef();
		ValidateInput();
		if (initialised) {
			OnChange();
			UpdateUV(flippedHorizontally != m_flipHorizontal, flippedVertically != m_flipVertical);
			flippedHorizontally = m_flipHorizontal;
			flippedVertically = m_flipVertical;
		}
		else {
			regenerateMesh = true;
		}
		
		// Note: Prefab changes are recorded via Inspector.
	}
	
	/**
	 * @brief If this is a prefab instance, collects modifications against the prefab and stores 
	 *	them to be saved with the scene. As it would be done automatically on changes in the Inspector.
	 *
	 * Most of the API (#SetTiling(), public properties) is meant to be used at run time
	 * and there you don't have to care about this method at all. However, if you ever need to change
	 * the parameters directly from an editor script, and this is a prefab instance, you need
	 * to call this.
	 *
	 * For example, you have a custom EditorWindow that flips all tiles when a button is pressed:
	 *
	 * TiledSpriteRenderer[] tiledSprites;
	 * foreach (TiledSpriteRenderer tiledSprite in tiledSprites) {
	 *   tiledSprite.flipHorizontal = ! tiledSprite.flipHorizontal;
	 *   tiledSprite.flipVertical = ! tiledSprite.flipVertical;
	 *   tiledSprite.RecordPrefabDifferences();
	 * }
	 *
	 * When the scene is saved and loaded later, all changes are then properly restored.
	 *
	 * It is safe to call this on TiledSpriteRenderers that aren't prefab instances.
	 * 
	 */
	[Conditional("UNITY_EDITOR")]
	public void RecordPrefabDifferences()
	{
	#if UNITY_EDITOR
		UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
	#endif
	}
	
	
	// MonoBehaviour callbacks.
	
	/**
	 * @brief Constructor.
	 * 
	 */
	protected virtual void Awake()
	{
		renderer = (MeshRenderer) base.GetComponent<Renderer>();
		renderer.hideFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
		renderer.shadowCastingMode = ShadowCastingMode.Off;
		renderer.receiveShadows = false;
		renderer.useLightProbes = false;
		renderer.enabled = false;
		
		meshFilter = GetComponent<MeshFilter>();
		meshFilter.hideFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
		
		vertices = new Vector3[] {};
		uv = new Vector2[] {};
		uv2 = new Vector2[] {};
		colors = new Color32[] {};
		
		if ((meshFilter.sharedMesh == null) || ! RegisterMesh(meshFilter.sharedMesh)) {	
			mesh = new Mesh();
			mesh.name = "Tile Grid";
			meshFilter.mesh = mesh;
			regenerateMesh = true;
			RegisterMesh(mesh);
		}
		else {
			mesh = meshFilter.sharedMesh;			
		}
		
		flippedHorizontally = m_flipHorizontal;
		flippedVertically = m_flipVertical;
		
		AddTexRef(m_sprite, this);
		
	#if UNITY_EDITOR
		if (! regenerateMesh && ! Application.isPlaying) {
			// The tile texture may have been changed while this object has been inactive or even
			// not loaded at all. So we need to check the timestamp and update UV if needed.
			long timestamp = 0;
			int currentTsHigh = 0,
				currentTsLow = 0;
			if ((m_sprite != null) && (m_sprite.texture != null)) {
				timestamp = AssetMetaCache.GetTimestamp(m_sprite.texture);
				currentTsHigh = (int) (timestamp >> 32);
				currentTsLow = (int) (timestamp & 0xFFFFFFFF);
			}
			if ((currentTsLow != timestampLow) || (currentTsHigh != timestampHigh)) {
				regenerateMesh = true;
				timestampLow = currentTsLow;
				timestampHigh = currentTsHigh;
			}
		}
	#endif
		
		initialised = true;
	}
	
	protected virtual void OnEnable()
	{
		renderer.enabled = true;
		UpdateSorting();
	}
		
	protected virtual void OnDisable()
	{
		renderer.enabled = false;
	}
	
	/**
	 * @brief Initialisation. 
	 *
	 * Note that mesh is not updated unless its parameters change. It's been generated during 
	 *	the design phase already.
	 * 
	 */
	protected virtual void Start()
	{
		if (regenerateMesh) {
			OnChange();
			regenerateMesh = false;
			if (! Application.isPlaying) {
				RecordPrefabDifferences();
			}
		}
		else if (m_material != null) {
			// The default material assigned to the script is not yet available in Awake() or OnEnable() (in editor),
			// so this is the best place to update it for the first time.
			UpdateMaterial();
		}
	}
	
	/**
	 * @brief Clean-up.
	 * 
	 */
	protected virtual void OnDestroy()
	{
		meshFilter.mesh = null;
		UnregisterMesh(mesh);
		RemoveTexRef(this);
		GameObjectUtils.DestroyObject(mesh);
	}
	
#if UNITY_EDITOR
	protected virtual void Update()
	{
		if (! Application.isPlaying) {
			// When the texture is re-imported, material needs to be updated.
			if (m_material != null) {
				UpdateMaterial();
			}
		}
	}
#endif
	
	// Updating routines.
	
	/**
	 * @brief Full update.
	 * 
	 */	
	void OnChange() 
	{
		UpdateMesh(true);
		UpdateMaterial();
		UpdateSorting();
	}
	
	void UpdateMesh(bool _forceColorUpdate)
	{
		if (! initialised) {
			// Parameters changed before Awake() has been run, do a full update when appropriate.
			// (Can't rely on Awake() when the GameObject is disabled. It does a lousy job as a constructor.)
			regenerateMesh = true;
			return;
		}
				
		// Calculate number of vertices needed.
		
		float tileFillX = m_size.x,
			tileFillY = m_size.y;
		int tileEdgeCountX = 2,
			tileEdgeCountY = 2;	
		Vector2 tileSize = m_size;
		float halfPixelSize = 0;
		
		if (m_sprite != null) {
			Vector2 anchorCoords = ToCoords(m_tileAnchor);
			
			tileSize = m_sprite.bounds.size;
			halfPixelSize = tileSize.x / m_sprite.rect.width * 0.5f;
			
			tileEdgeCountX = CalculateEdges(out tileFillX, m_size.x, tileSize.x, anchorCoords.x, m_tileOffset.x, halfPixelSize);
			tileEdgeCountY = CalculateEdges(out tileFillY, m_size.y, tileSize.y, anchorCoords.y, m_tileOffset.y, halfPixelSize);
			
			if ((tileEdgeCountX > EDGE_COUNT_LIMIT_1D) || (tileEdgeCountY > EDGE_COUNT_LIMIT_1D)) {
				tileEdgeCountX = Mathf.Min(tileEdgeCountX, EDGE_COUNT_LIMIT_1D);
				tileEdgeCountY = Mathf.Min(tileEdgeCountY, EDGE_COUNT_LIMIT_1D);
				Debug.LogError(LOG_TAG + "Vertex limit exceeded! Number of tiles clamped. "
					+ "Try making your TiledSprite smaller or using bigger tiles.", gameObject);
			}
		}
		
		// double inner vertices, so different UVs per tile can be used
		int vertexCount = ((tileEdgeCountX - 2) * 2 + 2) * ((tileEdgeCountY - 2) * 2 + 2);
		
		// Re-allocate vertex buffers.
		
		bool bufferSizeChanged = false;
		int[] tris = null;
		
		if (vertices.Length != vertexCount) {
			if ((vertices.Length == 0) && (mesh.vertexCount == vertexCount)) {
				ReloadVertexData();
			}
			else {
				bufferSizeChanged = true;
				
				vertices = new Vector3[vertexCount];
				uv = new Vector2[vertexCount];
				colors = new Color32[vertexCount];
				
				int tileCount = (tileEdgeCountX - 1) * (tileEdgeCountY - 1);
				tris = new int[tileCount * 6];
				
				// update triangles
				int vertexIdx = 0,
					trisIdx = 0;
				for (int i = 0; i < tileCount; ++i) {
					tris[trisIdx] = vertexIdx;
					tris[trisIdx + 1] = vertexIdx + 1;
					tris[trisIdx + 2] = vertexIdx + 2;
					tris[trisIdx + 3] = vertexIdx;
					tris[trisIdx + 4] = vertexIdx + 2;
					tris[trisIdx + 5] = vertexIdx + 3;
					trisIdx += 6;
					vertexIdx += 4;
				}
				
				mesh.Clear();
			}
		}
		
		// Update vertex values.
		
		Vector2 pivotRel = ToCoords(m_pivot);
		Vector2 pivotAbs = new Vector2(m_size.x * pivotRel.x, m_size.y * pivotRel.y) - m_pivotOffset;
		Vector2 areaBounds = m_size - pivotAbs;
		float tileFillXClamped = tileFillX,
			tileFillYClamped = tileFillY;
		if (tileFillXClamped >= m_size.x - halfPixelSize) {
			tileFillXClamped = m_size.x;
		}
		if (tileFillYClamped >= m_size.y - halfPixelSize) {
			tileFillYClamped = m_size.y;
		}
		tileFillXClamped -= pivotAbs.x;
		tileFillYClamped -= pivotAbs.y;
		
		Vector4 uv01,
			uvMargins;
		if (m_sprite != null) {
			Rect texRect = m_sprite.textureRect;
			Texture tex = m_sprite.texture;
			texRect.xMin = texRect.xMin / tex.width;
			texRect.xMax = texRect.xMax / tex.width;
			texRect.yMin = texRect.yMin / tex.height;
			texRect.yMax = texRect.yMax / tex.height;			
			
			Vector2 u = GetTexCoords1D(tileSize.x, m_size.x, tileFillX, tileEdgeCountX - 1, new Vector2(texRect.xMin, texRect.xMax));
			Vector2 v = GetTexCoords1D(tileSize.y, m_size.y, tileFillY, tileEdgeCountY - 1, new Vector2(texRect.yMin, texRect.yMax));
			uvMargins = new Vector4(u.x, u.y, v.x, v.y);
			
			uv01 = new Vector4(texRect.xMin, texRect.xMax, texRect.yMin, texRect.yMax);
			if (tileEdgeCountX == 2) {
				uv01[1] = uvMargins[1];
			}
			if (tileEdgeCountY == 2) {
				uv01[3] = uvMargins[3];
			}
		}
		else {
			uv01 = new Vector4(0, 1, 0, 1);
			uvMargins = uv01;
		}		
		
		Vector2 tilePos = -pivotAbs;
		int vertexIndex = 0;
		
		// first row
		
		// ... first horizontal tile
		vertices[vertexIndex] = new Vector3(tilePos.x, tilePos.y, 0);
		vertices[vertexIndex + 1] = new Vector3(tilePos.x, tileFillYClamped, 0);
		vertices[vertexIndex + 2] = new Vector3(tileFillXClamped, tileFillYClamped, 0);
		vertices[vertexIndex + 3] = new Vector3(tileFillXClamped, tilePos.y, 0);
		uv[vertexIndex] = new Vector2(uvMargins[0], uvMargins[2]);
		uv[vertexIndex + 1] = new Vector2(uvMargins[0], uv01[3]);
		uv[vertexIndex + 2] = new Vector2(uv01[1], uv01[3]);
		uv[vertexIndex + 3] = new Vector2(uv01[1], uvMargins[2]);
		vertexIndex += 4;
		tilePos.x = tileFillXClamped;
		
		// ... middle tiles
		for (int j = tileEdgeCountX - 3; j > 0; --j) {
			vertices[vertexIndex] = new Vector3(tilePos.x, tilePos.y, 0);
			vertices[vertexIndex + 1] = new Vector3(tilePos.x, tileFillYClamped, 0);
			vertices[vertexIndex + 2] = new Vector3(tilePos.x + tileSize.x, tileFillYClamped, 0);
			vertices[vertexIndex + 3] = new Vector3(tilePos.x + tileSize.x, tilePos.y, 0);
			uv[vertexIndex] = new Vector2(uv01[0], uvMargins[2]);
			uv[vertexIndex + 1] = new Vector2(uv01[0], uv01[3]);
			uv[vertexIndex + 2] = new Vector2(uv01[1], uv01[3]);
			uv[vertexIndex + 3] = new Vector2(uv01[1], uvMargins[2]);
			vertexIndex += 4;
			tilePos.x += tileSize.x;
		}
		
		// ... last horizontal tile
		if (tileEdgeCountX > 2) {
			vertices[vertexIndex] = new Vector3(tilePos.x, tilePos.y, 0);
			vertices[vertexIndex + 1] = new Vector3(tilePos.x, tileFillYClamped, 0);
			vertices[vertexIndex + 2] = new Vector3(areaBounds.x, tileFillYClamped, 0);
			vertices[vertexIndex + 3] = new Vector3(areaBounds.x, tilePos.y, 0);
			uv[vertexIndex] = new Vector2(uv01[0], uvMargins[2]);
			uv[vertexIndex + 1] = new Vector2(uv01[0], uv01[3]);
			uv[vertexIndex + 2] = new Vector2(uvMargins[1], uv01[3]);
			uv[vertexIndex + 3] = new Vector2(uvMargins[1], uvMargins[2]);
			vertexIndex += 4;
		}
		
		tilePos.y = tileFillYClamped;

		// middle
		for (int i = tileEdgeCountY - 3; i > 0; --i) {
			tilePos.x = -pivotAbs.x;
			
			// ... first horizontal tile
			vertices[vertexIndex] = new Vector3(tilePos.x, tilePos.y, 0);
			vertices[vertexIndex + 1] = new Vector3(tilePos.x, tilePos.y + tileSize.y, 0);
			vertices[vertexIndex + 2] = new Vector3(tileFillXClamped, tilePos.y + tileSize.y, 0);
			vertices[vertexIndex + 3] = new Vector3(tileFillXClamped, tilePos.y, 0);
			uv[vertexIndex] = new Vector2(uvMargins[0], uv01[2]);
			uv[vertexIndex + 1] = new Vector2(uvMargins[0], uv01[3]);
			uv[vertexIndex + 2] = new Vector2(uv01[1], uv01[3]);
			uv[vertexIndex + 3] = new Vector2(uv01[1], uv01[2]);
			vertexIndex += 4;
			tilePos.x = tileFillXClamped;
			
			// ... middle tiles
			for (int j = tileEdgeCountX - 3; j > 0; --j) {
				vertices[vertexIndex] = new Vector3(tilePos.x, tilePos.y, 0);
				vertices[vertexIndex + 1] = new Vector3(tilePos.x, tilePos.y + tileSize.y, 0);
				vertices[vertexIndex + 2] = new Vector3(tilePos.x + tileSize.x, tilePos.y + tileSize.y, 0);
				vertices[vertexIndex + 3] = new Vector3(tilePos.x + tileSize.x, tilePos.y, 0);
				uv[vertexIndex] = new Vector2(uv01[0], uv01[2]);
				uv[vertexIndex + 1] = new Vector2(uv01[0], uv01[3]);
				uv[vertexIndex + 2] = new Vector2(uv01[1], uv01[3]);
				uv[vertexIndex + 3] = new Vector2(uv01[1], uv01[2]);
				vertexIndex += 4;
				tilePos.x += tileSize.x;
			}
			
			// ... last horizontal tile
			if (tileEdgeCountX > 2) {
				vertices[vertexIndex] = new Vector3(tilePos.x, tilePos.y, 0);
				vertices[vertexIndex + 1] = new Vector3(tilePos.x, tilePos.y + tileSize.y, 0);
				vertices[vertexIndex + 2] = new Vector3(areaBounds.x, tilePos.y + tileSize.y, 0);
				vertices[vertexIndex + 3] = new Vector3(areaBounds.x, tilePos.y, 0);
				uv[vertexIndex] = new Vector2(uv01[0], uv01[2]);
				uv[vertexIndex + 1] = new Vector2(uv01[0], uv01[3]);
				uv[vertexIndex + 2] = new Vector2(uvMargins[1], uv01[3]);
				uv[vertexIndex + 3] = new Vector2(uvMargins[1], uv01[2]);
				vertexIndex += 4;
			}
			
			tilePos.y += tileSize.y;
		}
		
		// last row
		if (tileEdgeCountY > 2) {
			tilePos.x = -pivotAbs.x;
			
			// ... first horizontal tile
			vertices[vertexIndex] = new Vector3(tilePos.x, tilePos.y, 0);
			vertices[vertexIndex + 1] = new Vector3(tilePos.x, areaBounds.y, 0);
			vertices[vertexIndex + 2] = new Vector3(tileFillXClamped, areaBounds.y, 0);
			vertices[vertexIndex + 3] = new Vector3(tileFillXClamped, tilePos.y, 0);
			uv[vertexIndex] = new Vector2(uvMargins[0], uv01[2]);
			uv[vertexIndex + 1] = new Vector2(uvMargins[0], uvMargins[3]);
			uv[vertexIndex + 2] = new Vector2(uv01[1], uvMargins[3]);
			uv[vertexIndex + 3] = new Vector2(uv01[1], uv01[2]);
			vertexIndex += 4;
			tilePos.x = tileFillXClamped;
			
			// ... middle tiles
			for (int j = tileEdgeCountX - 3; j > 0; --j) {
				vertices[vertexIndex] = new Vector3(tilePos.x, tilePos.y, 0);
				vertices[vertexIndex + 1] = new Vector3(tilePos.x, areaBounds.y, 0);
				vertices[vertexIndex + 2] = new Vector3(tilePos.x + tileSize.x, areaBounds.y, 0);
				vertices[vertexIndex + 3] = new Vector3(tilePos.x + tileSize.x, tilePos.y, 0);
				uv[vertexIndex] = new Vector2(uv01[0], uv01[2]);
				uv[vertexIndex + 1] = new Vector2(uv01[0], uvMargins[3]);
				uv[vertexIndex + 2] = new Vector2(uv01[1], uvMargins[3]);
				uv[vertexIndex + 3] = new Vector2(uv01[1], uv01[2]);
				vertexIndex += 4;
				tilePos.x += tileSize.x;
			}
			
			// ... last horizontal tile
			if (tileEdgeCountX > 2) {
				vertices[vertexIndex] = new Vector3(tilePos.x, tilePos.y, 0);
				vertices[vertexIndex + 1] = new Vector3(tilePos.x, areaBounds.y, 0);
				vertices[vertexIndex + 2] = new Vector3(areaBounds.x, areaBounds.y, 0);
				vertices[vertexIndex + 3] = new Vector3(areaBounds.x, tilePos.y, 0);
				uv[vertexIndex] = new Vector2(uv01[0], uv01[2]);
				uv[vertexIndex + 1] = new Vector2(uv01[0], uvMargins[3]);
				uv[vertexIndex + 2] = new Vector2(uvMargins[1], uvMargins[3]);
				uv[vertexIndex + 3] = new Vector2(uvMargins[1], uv01[2]);
				vertexIndex += 4;
			}
		}
		
		// Apply buffers to mesh.
		
		mesh.vertices = vertices;
		
		if (! UpdateUV(flippedHorizontally, flippedVertically)) {
			mesh.uv = uv;
		}
		
		if (m_enableClippingUV) {
			UpdateClippingUV();
			mesh.uv2 = uv2;
		}
		else {
			uv2 = new Vector2[] {};
			mesh.uv2 = null;
		}
		
		if (bufferSizeChanged || _forceColorUpdate) {
			UpdateColor();
		}
			
		if (bufferSizeChanged) {
			mesh.triangles = tris;
		}
		
		mesh.RecalculateBounds();
		if (bufferSizeChanged) {
			mesh.RecalculateNormals();
		}
	}
	
	bool UpdateUV(bool _flipHorizontal, bool _flipVertical)
	{
		if (! _flipHorizontal && ! _flipVertical) {
			return false;
		}

		if (! initialised) {
			// Parameters changed before Awake() has been run, do a full update when appropriate.
			// (Can't rely on Awake() when the GameObject is disabled. It does a lousy job as a constructor.)
			regenerateMesh = true;
			return true;
		}
		
		if (vertices.Length == 0) {
			ReloadVertexData();
		}
		
		Rect texRect;
		if ((m_sprite != null) && (m_sprite.texture != null)) {
			Texture tex = m_sprite.texture;
			texRect = m_sprite.rect;
			texRect.xMin /= tex.width;
			texRect.xMax /= tex.width;
			texRect.yMin /= tex.height;
			texRect.yMax /= tex.height;
		}
		else {
			texRect = Rect.MinMaxRect(0, 0, 1, 1);
		}
				
		for (int i = 0; i < uv.Length; i += 4) {
			if (_flipHorizontal) {
				uv[i].x = texRect.xMin + texRect.xMax - uv[i].x;
				uv[i + 1].x = texRect.xMin + texRect.xMax - uv[i + 1].x;
				uv[i + 2].x = texRect.xMin + texRect.xMax - uv[i + 2].x;
				uv[i + 3].x = texRect.xMin + texRect.xMax - uv[i + 3].x;
			}
			if (_flipVertical) {
				uv[i].y = texRect.yMin + texRect.yMax - uv[i].y;
				uv[i + 1].y = texRect.yMin + texRect.yMax - uv[i + 1].y;
				uv[i + 2].y = texRect.yMin + texRect.yMax - uv[i + 2].y;
				uv[i + 3].y = texRect.yMin + texRect.yMax - uv[i + 3].y;
			}
		}
		
		mesh.uv = uv;
		
		return true;
	}
	
	void UpdateClippingUV()
	{
		if ((uv2.Length == 0) || (uv2.Length != vertices.Length)) {
			uv2 = new Vector2[vertices.Length];
		}
		
		Vector2 minPos = vertices[0],
			maxPos = vertices[vertices.Length - 1],
			size = maxPos - minPos;
		if (Mathf.Abs(size.x) < Mathf.Epsilon) {
			size.x = 1;
		}
		if (Mathf.Abs(size.y) < Mathf.Epsilon) {
			size.y = 1;
		}
		for (int i = 0; i < vertices.Length; ++i) {
			Vector2 dist = ((Vector2) vertices[i] - minPos);
			dist.x = Mathf.Clamp01(dist.x / size.x);
			dist.y = Mathf.Clamp01(dist.y / size.y);
			uv2[i] = dist;
		}
	}
	
	void UpdateColor()
	{
		if (! initialised) {
			// Parameters changed before Awake() has been run, do a full update when appropriate.
			// (Can't rely on Awake() when the GameObject is disabled. It does a lousy job as a constructor.)
			regenerateMesh = true;
			return;
		}
		
		if (vertices.Length == 0) {
			ReloadVertexData();
		}
		
		Color32 vertexColor = m_color;
		for (int i = 0; i < colors.Length; ++i) {
			colors[i] = vertexColor;
		}
		mesh.colors32 = colors;
	}
	
	void UpdateMaterial() 
	{
		if (! initialised) {
			// Parameters changed before Awake() has been run, do a full update when appropriate.
			// (Can't rely on Awake() when the GameObject is disabled. It does a lousy job as a constructor.)
			regenerateMesh = true;
			return;
		}
		
		if ((renderer.sharedMaterials.Length != 1) || (renderer.sharedMaterials[0] != m_material)) {
			// Update only when necessary to prevent false scene changes.
			renderer.sharedMaterials = new Material[] {m_material};
		}
		
		MaterialPropertyBlock materialProperties = new MaterialPropertyBlock();
		renderer.GetPropertyBlock(materialProperties);
		
		if ((m_sprite != null) && (m_sprite.texture != null)) {
			materialProperties.SetTexture("_MainTex", m_sprite.texture);
		}
		else if (m_material != null) {
			if (materialProperties.GetTexture("_MainTex") != null) {
				materialProperties.Clear();
			}
			m_material.SetTexture("_MainTex", null);
		}
		
		renderer.SetPropertyBlock(materialProperties);
	}
	
	void UpdateSorting()
	{
		if (! initialised) {
			// Parameters changed before Awake() has been run, do a full update when appropriate.
			// (Can't rely on Awake() when the GameObject is disabled. It does a lousy job as a constructor.)
			regenerateMesh = true;
			return;
		}
		
		renderer.sortingLayerName = m_sortingLayer;
		renderer.sortingOrder = m_orderInLayer;
	}
	
	void ValidateInput()
	{
		// size
		if (Single.IsNaN(m_size.x) || Single.IsInfinity(m_size.x) || (m_size.x < 1)) {
			m_size.x = 1;
		}
		if (Single.IsNaN(m_size.y) || Single.IsInfinity(m_size.y) || (m_size.y < 1)) {
			m_size.y = 1;
		}
		
		// tile anchor
		if (m_tileAnchor == SpriteAlignment.Custom) {
			m_tileAnchor = SpriteAlignment.BottomLeft;
		}
		
		// tile offset
		if (Single.IsNaN(m_tileOffset.x)) {
			m_tileOffset.x = 0;
		}
		if (Single.IsNaN(m_tileOffset.y)) {
			m_tileOffset.y = 0;
		}
		m_tileOffset = new Vector2(Mathf.Clamp(m_tileOffset.x, -1, 1), Mathf.Clamp(m_tileOffset.y, -1, 1));
		
		// pivot
		if (m_pivot == SpriteAlignment.Custom) {
			m_pivot = SpriteAlignment.Center;
		}
	}	
	
	// Mesh generation helpers.
	
	void ReloadVertexData()
	{
		vertices = mesh.vertices;
		uv = mesh.uv;
		if (m_enableClippingUV) {
			Vector2[] queryUV2 = mesh.uv2;
			if (queryUV2 != null) {
				uv2 = queryUV2;
			}
		}
		colors = mesh.colors32;
	}
	
	static Vector2 ToCoords(SpriteAlignment _anchor)
	{
		switch (_anchor) {
			
		case SpriteAlignment.BottomLeft:
			return new Vector2(0, 0);
			
		case SpriteAlignment.BottomCenter:
			return new Vector2(0.5f, 0);
			
		case SpriteAlignment.BottomRight:
			return new Vector2(1, 0);
			
		case SpriteAlignment.LeftCenter:
			return new Vector2(0, 0.5f);
			
		case SpriteAlignment.Center:
			return new Vector2(0.5f, 0.5f);
			
		case SpriteAlignment.RightCenter:
			return new Vector2(1, 0.5f);
			
		case SpriteAlignment.TopLeft:
			return new Vector2(0, 1);
			
		case SpriteAlignment.TopCenter:
			return new Vector2(0.5f, 1);
			
		case SpriteAlignment.TopRight:
			return new Vector2(1, 1);
			
		default:
			goto case SpriteAlignment.BottomLeft;
		}
	}
	
	static int CalculateEdges(
		out float _tileStart, 
		float _areaSize, 
		float _tileSize, 
		float _anchorPos, 
		float _offset, 
		float _halfPixelSize
	)
	{
		int edgeCount = 2;
		float tileStart;
		
		tileStart = _areaSize * _anchorPos + _tileSize * (-_anchorPos + _offset);
		
		if (tileStart < 0) {
			do {
				tileStart += _tileSize;
			} while (tileStart < 0);
		}
		else {
			tileStart = tileStart % _tileSize;
		}
		
		if (tileStart < _areaSize - _halfPixelSize) {
			float areaWithoutLeftMargin = _areaSize - tileStart;
			// number of whole tiles
			edgeCount += Mathf.FloorToInt(areaWithoutLeftMargin / _tileSize) - 1;
			// far margin
			if (areaWithoutLeftMargin % _tileSize >= _halfPixelSize) {
				edgeCount += 1;
			}
			// near margin
			if (tileStart >= _halfPixelSize) {
				edgeCount += 1;
			}
			else {
				// tileStart should always point to the beginning of the second tile fragment
				tileStart = _tileSize;
			}
		}
		
		_tileStart = tileStart;
		return edgeCount;
	}
	
	static Vector2 GetTexCoords1D(float _tileSize, float _areaSize, float _secondTilePos, int _tileCount, Vector2 _texRange)
	{
		Vector2 texCoords;
		
		texCoords.x = Mathf.Clamp01((_tileSize - _secondTilePos) / _tileSize);
		if (_tileCount == 1) {
			texCoords.y = Mathf.Clamp01((_tileSize - (_secondTilePos - _areaSize)) / _tileSize);
		}
		else {
			texCoords.y = Mathf.Clamp01((_areaSize - _tileSize * (_tileCount - 2) - _secondTilePos) / _tileSize);
		}
		texCoords.x = Mathf.Lerp(_texRange.x, _texRange.y, texCoords.x);
		texCoords.y = Mathf.Lerp(_texRange.x, _texRange.y, texCoords.y);
		
		return texCoords;
	}
	
	// Mesh book-keeping to properly handle duplicates.
	// Using a Set would be enough, but Dictionary doesn't add another DLL dependency.

	static bool RegisterMesh(Mesh _mesh)
	{
		if (meshRefs == null) {
			meshRefs = new Dictionary<Mesh, int>();
		}
		
		if (meshRefs.ContainsKey(_mesh)) {
			return false;
		}
		else {
			meshRefs.Add(_mesh, 0);
			return true;
		}
	}
	
	static void UnregisterMesh(Mesh _mesh)
	{
		if (meshRefs == null) {
			return;
		}
		
		meshRefs.Remove(_mesh);
	}
	
	// Texture book-keeping to effectively react to texture changes. Inner layer.
	
	[Conditional("UNITY_EDITOR")]
	void UpdateTexRef()
	{
		if (initialised) {
			RemoveTexRef(this);
			AddTexRef(m_sprite, this);
		}

		UpdateTexTimestamp();		
	}
	
	[Conditional("UNITY_EDITOR")]
	void UpdateTexTimestamp()
	{
	#if UNITY_EDITOR
		if ((m_sprite != null) && (m_sprite.texture != null)) {
			long texTimestamp = AssetMetaCache.GetTimestamp(m_sprite.texture);
			timestampHigh = (int) (texTimestamp >> 32);
			timestampLow = (int) (texTimestamp & 0xFFFFFFFF);
		}
		else {
			timestampHigh = 0;
			timestampLow = 0;
		}
	#endif
	}
	
	[Conditional("UNITY_EDITOR")]
	static void AddTexRef(Sprite _sprite, TiledSpriteRenderer _tiledSprite)
	{
	#if UNITY_EDITOR
		var index = GetTexIndex();
		string texPath = null;
		
		if ((_sprite != null) && (_sprite.texture != null)) {
			texPath = UnityEditor.AssetDatabase.GetAssetPath(_sprite.texture);
		}
		if (string.IsNullOrEmpty(texPath)) {
			return;
		}
		
		if (! index.ContainsKey(texPath)) {
			index.Add(texPath, new List<TiledSpriteRenderer>());
		}
		if (! index[texPath].Contains(_tiledSprite)) {
			index[texPath].Add(_tiledSprite);
		}
	#endif
	}
	
	[Conditional("UNITY_EDITOR")]
	static void RemoveTexRef(TiledSpriteRenderer _tiledSprite)
	{
		var index = GetTexIndex();
		foreach (List<TiledSpriteRenderer> sprites in index.Values) {
			sprites.Remove(_tiledSprite);
		}
	}
	
	static Dictionary<string, List<TiledSpriteRenderer>> GetTexIndex()
	{
	#if UNITY_EDITOR
		if (textureRefs == null) {
			textureRefs = new Dictionary<string, List<TiledSpriteRenderer>>();
		}
		
		return textureRefs;
	#else
		return null;
	#endif
	}
		
#endregion

#region properties

	/**
	 * @brief Size of the tiled area.
	 */
	public Vector2 size
	{
		get {
			return m_size;
		}
		set {
			m_size = value;
			ValidateInput();
			UpdateMesh(false);
		}
	}
	
	/**
	 * @brief Tile Sprite -- it can be packed in an atlas. Can be changed through #SetTiling().
	 */
	public Sprite sprite
	{
		get {
			return m_sprite;
		}		
	}
	
	/**
	 * @brief Starting position of the tile -- can be anything but Custom (in which case
	 *	it will be set to Bottom Left). Can be changed through #SetTiling().
	 */
	public SpriteAlignment tileAnchor
	{
		get {
			return m_tileAnchor;
		}
	}
	
	/**
	 * @brief A fine position offset from the #tileAnchor; in range -1 .. 1 tile.
	 *	Can be changed through #SetTiling().
	 */
	public Vector2 tileOffset
	{
		get {
			return m_tileOffset;
		}
	}
	
	/**
	 * @brief Pivot point that the mesh is relative to.
	 *
	 * Setting this at run time will not affect anything until the mesh is re-generated.
	 * But it is useful when creating tiled sprites from a script. This can be set first
	 * and then #SetTiling() called with appropriate parameters.
	 * Otherwise, changing the pivot point dynamically at run time is generally not a good
	 * idea.
	 *	
	 */
	public SpriteAlignment pivot
	{
		get {
			return m_pivot;
		}
		set {
			m_pivot = value;
			ValidateInput();
		}
	}
	
	/**
	 * @brief Pivot point offset relative to the pivot point, in world units.
	 *
	 * Setting this at run time will not affect anything until the mesh is re-generated.
	 * But it is useful when creating tiled sprites from a script. This can be set first
	 * and then #SetTiling() called with appropriate parameters.
	 * Otherwise, changing the pivot point dynamically at run time is generally not a good
	 * idea.
	 *	
	 */
	public Vector2 pivotOffset
	{
		get {
			return m_pivotOffset;
		}
		set {
			m_pivotOffset = value;
			ValidateInput();
		}
	}
	
	/**
	 * @brief Material to use.
	 */
	public Material sharedMaterial
	{
		get {
			return m_material;
		}		
		set {
			m_material = value;
			UpdateMaterial();
		}
	}
	
	/**
	 * @brief Optional tint (colour is multiplied).
	 */
	public Color color
	{
		get {
			return m_color;
		}		
		set {
			m_color = value;
			UpdateColor();
		}
	}
	
	/**
	 * @brief Whether to flip tile content horizontally.
	 */
	public bool flipHorizontal
	{
		get {
			return flippedHorizontally;
		}
		set {
			UpdateUV(flippedHorizontally != value, false);
			flippedHorizontally = value;
			m_flipHorizontal = flippedHorizontally;
		}
	}
	
	/**
	 * @brief Whether to flip tile content vertically.
	 */
	public bool flipVertical
	{
		get {
			return flippedVertically;
		}
		set {
			UpdateUV(false, flippedVertically != value);
			flippedVertically = value;
			m_flipVertical = flippedVertically;
		}
	}
	
	/**
	 * @brief Sorting layer.
	 */
	public string sortingLayer
	{
		get {
			return m_sortingLayer;
		}
		set {
			m_sortingLayer = value;
			UpdateSorting();
		}
	}
	
	/**
	 * @brief Sorting order in layer.
	 */
	public int sortingOrder
	{
		get {
			return m_orderInLayer;
		}
		set {
			m_orderInLayer = value;
			UpdateSorting();
		}
	}
		
#endregion
	
#region fields
	
	// References of generated meshes to handle duplicates correctly.
	static Dictionary<Mesh, int> meshRefs;
	
#if UNITY_EDITOR
	// Referenced textures that should be updated on texture re-import.
	static Dictionary<string, List<TiledSpriteRenderer>> textureRefs;
#endif
	
	// editor variables

	[SerializeField]
	Vector2 m_size = new Vector2(32, 32);
	
	[SerializeField]
	Sprite m_sprite;
	
	[SerializeField]
	SpriteAlignment m_tileAnchor = SpriteAlignment.BottomLeft;
	
	[SerializeField]
	[Tooltip("In tiles, range (-1..1)")]
	Vector2 m_tileOffset;
	
	[SerializeField]
	SpriteAlignment m_pivot = SpriteAlignment.Center;
	
	[SerializeField]
	[Tooltip("In world units")]
	Vector2 m_pivotOffset;
	
	[SerializeField]
	Material m_material;
	
	[SerializeField]
	Color m_color = Color.white;
	
	[SerializeField]
	bool m_flipHorizontal = false;
	
	[SerializeField]
	bool m_flipVertical = false;
	
	[SerializeField]
	[Tooltip("Generates second UV that can be used to set up clipping via material")]
	bool m_enableClippingUV = false;
	
	[SerializeField]
	string m_sortingLayer = "Default";
	
	[SerializeField]
	int m_orderInLayer = 0;
	
	// mesh related data
	
	new MeshRenderer renderer;
	MeshFilter meshFilter;
	Mesh mesh;
	Vector3[] vertices;
	Vector2[] uv;
	Vector2[] uv2;
	Color32[] colors;
	
	// internal state
	
	bool initialised = false;
	bool flippedHorizontally = false;
	bool flippedVertically = false;
	
	[SerializeField]
	[HideInInspector]
	bool regenerateMesh = false;

#if UNITY_EDITOR
	
	[SerializeField]
	[HideInInspector]
	int timestampHigh;
	
	[SerializeField]
	[HideInInspector]
	int timestampLow;
	
#endif
	
#endregion
}

/* EOF */
