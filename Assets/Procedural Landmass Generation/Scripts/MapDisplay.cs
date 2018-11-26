﻿using System.Collections;
using UnityEngine;

// NOTE : The reason of using 'sharedMaterial' in this class, its because the property
// 'material', its only instantiated at runtime, and in this case we're able to create
// our assets in editor mode

public class MapDisplay : MonoBehaviour 
{
	public Renderer textureRenderer;
	public MeshFilter meshFilter;
	public MeshRenderer meshRenderer;

	public void DrawTexture(Texture2D texture)
	{
		textureRenderer.sharedMaterial.mainTexture = texture;
		textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
	}

	public void DrawMesh(MeshData meshData)
	{
		meshFilter.mesh = meshData.CreateMesh();

        meshFilter.transform.localScale = Vector3.one * FindObjectOfType<MapGenerator>().terrainData.uniformScale;
	}	
}
