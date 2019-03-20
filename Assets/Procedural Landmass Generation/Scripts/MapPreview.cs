using System.Collections;
using UnityEngine;

// NOTE : The reason of using 'sharedMaterial' in this class, its because the property
// 'material', its only instantiated at runtime, and in this case we're able to create
// our assets in editor mode

public class MapPreview : MonoBehaviour 
{
	public Renderer textureRenderer;
	public MeshFilter meshFilter;
	public MeshRenderer meshRenderer;

    public enum DrawMode { NoiseMap, Mesh, Falloff };
    public DrawMode drawMode;

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureData textureData;

    public Material terrainMaterial;

    [Range(0, MeshSettings.numberOfSupportedLODs - 1)]
    public int editorPreviewLevelOfDetail;
    public bool autoUpdate;
    
    public void DrawMapInEditor()
    {
        textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numberOfVerticesPerLine, meshSettings.numberOfVerticesPerLine, heightMapSettings, Vector2.zero);

        if (drawMode == DrawMode.NoiseMap)
            DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
        else if (drawMode == DrawMode.Mesh)
        {
            MeshData meshData = MeshGenerator.GenerateTerrainMeshData(heightMap.values, meshSettings, editorPreviewLevelOfDetail);
            DrawMesh(meshData);
        }
        else if (drawMode == DrawMode.Falloff)
        {
            HeightMap falloffMap = new HeightMap(FalloffGenerator.GenerateFallofMap(meshSettings.numberOfVerticesPerLine), 0, 1);
            DrawTexture(TextureGenerator.TextureFromHeightMap(falloffMap));
        }
    }

    public void DrawTexture(Texture2D texture)
	{
		textureRenderer.sharedMaterial.mainTexture = texture;
		textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10f;

        textureRenderer.gameObject.SetActive(true);
        meshFilter.gameObject.SetActive(false);
	}

	public void DrawMesh(MeshData meshData)
	{
		meshFilter.mesh = meshData.CreateMesh();

        textureRenderer.gameObject.SetActive(false);
        meshFilter.gameObject.SetActive(true);
    }

    void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            DrawMapInEditor();
        }
    }

    void OnTextureValuesUpdated()
    {
        textureData.ApplyToMaterial(terrainMaterial);
    }

    private void OnValidate()
    {
        if (meshSettings != null)
        {
            meshSettings.OnValuesUpdated -= OnValuesUpdated;
            meshSettings.OnValuesUpdated += OnValuesUpdated;
        }

        if (heightMapSettings != null)
        {
            heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
            heightMapSettings.OnValuesUpdated += OnValuesUpdated;
        }

        if (textureData != null)
        {
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }
    }
}
