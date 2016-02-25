/**
 * @file
 * @brief This class is notified that a texture has been imported.
 * 
 * @author Simon
 * @date September 2014
 * 
 */

// .NET includes
using System;

// Unity includes
using UnityEngine;
using UnityEditor;

// Custom includes
using AssetLib;

/**
 * @brief This class is notified that a texture has been imported.
 */
public class TiledSpritePostprocessor : AssetPostprocessor
{
	static void OnPostprocessAllAssets(
		string[] _importedAssets, 
		string[] _deletedAssets, 
		string[] _movedToPaths, 
		string[] _movedFromPaths
	)
	{
		TiledSpriteRenderer.OnAssetsImported(_importedAssets, _deletedAssets, _movedToPaths, _movedFromPaths);
	}
	
	void OnPostprocessTexture(Texture2D _tex)
	{
		AssetMetaCache.UpdateMetaInfo(assetImporter.assetPath);
	}
}

/* EOF */
