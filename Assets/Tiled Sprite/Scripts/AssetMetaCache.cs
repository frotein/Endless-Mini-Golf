/**
 * @file
 * @brief Cache for fast access to important information about assets.
 * 
 * Currently asset timestamps.
 * 
 * @author Simon
 * @date September 2014
 * 
 */

#if UNITY_EDITOR

// .NET includes
using System;
using System.Collections.Generic;
using System.IO;

// Unity includes
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEditor;

// Custom includes

namespace AssetLib {

/**
 * @brief Stored meta data about an asset.
 */
public struct AssetMetaInfo
{
	public long timestamp;
	
	public AssetMetaInfo(long _timestamp)
	{
		timestamp = _timestamp;
	}
}

/**
 * @brief Cache management class.
 */
public static class AssetMetaCache
{
#region types

	static readonly string LOG_TAG = "[AssetLib.AssetMetaCache] ";
				
#endregion
		
#region methods

	/**
	 * @brief Return stored timestamp if exists, or retrieve it from hard-drive.
	 *
	 * @param _asset Asset in question.
	 * @return Universal time when the asset's original file was last written to.
	 *
	 */
	public static long GetTimestamp(Object _asset)
	{
		AssetMetaInfo info = new AssetMetaInfo();
		GetInfo(AssetDatabase.GetAssetPath(_asset), ref info);
		return info.timestamp;
	}
	
	/**
	 * @brief Update stored information if already present. This is called by an
	 *	appropriate AssetPostprocessor.
	 *
	 * @param _assetPath Asset to update.
	 *
	 */
	public static void UpdateMetaInfo(string _assetPath)
	{
		if (! initialised) {
			return;
		}
		
		string guid = AssetDatabase.AssetPathToGUID(_assetPath);
		if (infoTable.ContainsKey(guid)) {
			AssetMetaInfo info = new AssetMetaInfo();
			FetchData(_assetPath, ref info);
			infoTable[guid] = info;
		}
	}
	

	static void Init()
	{
		infoTable = new Dictionary<string, AssetMetaInfo>();
		initialised = true;
	}
	
	static void GetInfo(string _assetPath, ref AssetMetaInfo _info)
	{
		if (! initialised) {
			Init();
		}
		
		string guid = AssetDatabase.AssetPathToGUID(_assetPath);
		
		if (infoTable.ContainsKey(guid)) {
			// cache hit
			_info = infoTable[guid];
		}
		else {
			// cache miss
			FetchData(_assetPath, ref _info);
			infoTable[guid] = _info;
		}
	}
	
	static void FetchData(string _assetPath, ref AssetMetaInfo _info)
	{
		try {
			_info.timestamp = File.GetLastWriteTime(_assetPath).ToFileTimeUtc();
		}
		catch (Exception e) {
			_info.timestamp = 0;
			Debug.LogError(LOG_TAG + "Failed to retrieve meta data for '" + _assetPath + "'! Defaults returned. Reason: " + e.ToString());
		}
	}

#endregion
		
#region properties



#endregion
		
#region fields

	static bool initialised = false;
	static Dictionary<string, AssetMetaInfo> infoTable;

#endregion
}
	
}  /* namespace AssetLib */

#endif

/* EOF */
