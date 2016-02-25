/**
 * @file
 * @brief Custom Inspector view to track parameter changes of #TiledSpriteRenderer
 * 	better. It's more accurate than OnValidate() message on MonoBehaviours.
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

/**
 * @brief Custom Inspector view for #TiledSpriteRenderer.
 */
[CustomEditor(typeof(TiledSpriteRenderer))]
public class TiledSpriteInspector : Editor
{
	/**
	 * @brief Handler for Inspector view.
	 */
	public override void OnInspectorGUI()
	{
		GUI.changed = false;
		
		DrawDefaultInspector();

		if (GUI.changed) {
			var target = (TiledSpriteRenderer) this.target;
			target.OnInspectorChange();
		}
	}
}

/* EOF */
