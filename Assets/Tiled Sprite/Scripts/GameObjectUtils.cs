/**
 * @file
 * @brief Helpful GameObject and Transform extensions.
 * 
 * @author Simon
 * @date August 2013
 * 
 */

// .NET includes
using System;
using System.Collections.Generic;

// Unity includes
using UnityEngine;
using Object = UnityEngine.Object;

// Custom includes

namespace CoreLib {
	
/**
 * @brief GameObject and Transform extensions.
 */
public static class GameObjectUtils
{
#region nested types

			
		
#endregion
		
#region methods
		
	/**
	 * @brief Retreives a GameObject's child specified by _name in a path-like manner (e.g. "Monster/Arm/Hand")
	 * 		relative to _root object.
	 *
	 * This is an extension method of GameObject.
	 *
	 * @param _root Parent GameObject, for which to look up the child.
	 * @param _name Path to child relative to parent.
	 * @param _enableWildcards If set to true, all name tokens with a trailing '*'
	 *		will be compared as prefixes (e.g. "Name*" will match "Name", "Nameless", or "Name 123"). 
	 *		Default is false.
	 *
	 * @return Child GameObject found exactly on the specified path, or null.
	 *
	 */
	static public GameObject FindChild(this GameObject _root, string _name, bool _enableWildcards = false)
	{
		var tokens = _name.Split('/');
		
		Transform examinedNode = _root.transform;
		foreach (var childName in tokens) {
			string childNamePrefix = null;
			if (_enableWildcards && (childName.Length > 0) && (childName[childName.Length - 1] == '*')) {
				childNamePrefix = childName.Substring(0, childName.Length - 1);
			}
			
			bool matchFound = false;
			for (int i = examinedNode.childCount; i > 0; --i) {
				if (childNamePrefix != null) {
					matchFound = examinedNode.GetChild(i - 1).name.StartsWith(childNamePrefix);
				}
				else {
					matchFound = (examinedNode.GetChild(i - 1).name == childName);
				}
				
				if (matchFound) {
					examinedNode = examinedNode.GetChild(i - 1);
					break;
				}
			}
			
			if (! matchFound) {
				return null;
			}
		}
		
		return examinedNode.gameObject;
	}
	
	/**
	 * @brief Retreives all GameObject's children with the tag _tag.
	 *
	 * This is an extension method of GameObject.
	 *
	 * @param _root Parent GameObject, for which to do the look up.
	 * @param _tag The tag to be searched for.
	 *
	 * @return An array of GameObject's children with the given tag, or an empty array.
	 *
	 * @exception System.ArgumentNullException If _tag is null or empty, or _root is null.
	 *
	 */
	static public GameObject[] FindChildObjectsWithTag(this GameObject _root, string _tag)
	{
		if ((_root == null) || string.IsNullOrEmpty(_tag)) {
			throw new ArgumentException("Illegal arguments passed to FindGameObjectsWithTag()!");
		}
		
		List<GameObject> matches = new List<GameObject>();
		List<Transform> nodesToExamine = new List<Transform>();
		
		nodesToExamine.Add(_root.transform);
		while (nodesToExamine.Count > 0) {
			Transform processedNode = nodesToExamine[nodesToExamine.Count - 1];
			nodesToExamine.RemoveAt(nodesToExamine.Count - 1);
			
			// prepare children
			if (processedNode.childCount > 0) {
				for (int i = 0; i < processedNode.childCount; ++i) {
					nodesToExamine.Add(processedNode.GetChild(i));
				}
			}
			
			// tag and bag the current node
			if (processedNode.CompareTag(_tag)) {
				matches.Add(processedNode.gameObject);
			}
		}
		
		return matches.ToArray();
	}
	
	/**
	 * @brief Set a Transform from another Transform.
	 *
	 * @param _ltrans Transform to modify.
	 * @param _rtrans Transform values to copy over to _ltrans.
	 * @param _reparent Whether to also copy parent. False by default.
	 * 
	 */
	static public void Set(this Transform _ltrans, Transform _rtrans, bool _reparent = false)
	{
		_ltrans.localPosition = _rtrans.localPosition;
		_ltrans.localRotation = _rtrans.localRotation;
		_ltrans.localScale = _rtrans.localScale;
		
		if (_reparent) {
			_ltrans.parent = _rtrans.parent;
		}
	}
		
	/**
	 * @brief Properly destroy an object whether it's running in a player or the editor.
	 * 
	 */
	static public void DestroyObject(UnityEngine.Object _object)
	{
#if UNITY_EDITOR
		if (Application.isPlaying) {
			Object.Destroy(_object);
		}
		else {
			Object.DestroyImmediate(_object, false);
		}
#else
		Object.Destroy(_object);
#endif 	
	}
		
#endregion
		
#region properties



#endregion
		
#region fields



#endregion
}
	
}  /* namespace CoreLib */

/* EOF */