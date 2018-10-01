using System.Collections;
using UnityEngine;

public class MapGenerator : MonoBehaviour 
{
	public enum DrawMode { NoiseMap, ColorMap, Mesh };
	public DrawMode drawMode;

	//The max of vertex per Mesh that Unity allows is 255²
	//but 241, when in the formula is divisible by 1, 2, 4, 8, 10, 12
	//which gives us a good range for mesh simplification
	public const int mapChunkSize = 241;
	[Range(0, 6)]
	public int levelOfDetail;
	public float noiseScale;

	public int octaves;
	[Range(0,1)]
	public float persistance;
	public float lacunarity;
	public int seed;
	public Vector2 offset;
	public float meshHeightMultiplier;
	public AnimationCurve meshHeightCurve;

	public bool autoUpdate;

	public TerrainType[] regions;

	public void GenerateMap()
	{
		float [,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, offset);

		Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
		for(int y = 0; y < mapChunkSize; y++){
			for(int x = 0; x < mapChunkSize; x++){
				float currentHeight = noiseMap[x,y];
				
				for(int i = 0; i < regions.Length; i++){
					if(currentHeight <= regions[i].height){
						colorMap[y * mapChunkSize + x] = regions[i].color;
						break;
					}
				}
			}
		}

		MapDisplay display = FindObjectOfType<MapDisplay>();
		
		if (drawMode == DrawMode.NoiseMap)
			display.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
		else if (drawMode == DrawMode.ColorMap)
			display.DrawTexture(TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize));
		else if (drawMode == DrawMode.Mesh){
			MeshData meshData = MeshGenerator.GenerateTerrainMeshData(noiseMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail);
			Texture2D texture = TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize);
			display.DrawMesh(meshData, texture);	
		}
			
	}

	private void OnValidate() 
	{
		if(lacunarity < 1)
			lacunarity = 1;
		
		if(octaves < 0)
			octaves = 0;
	}
}

[System.Serializable]
public struct TerrainType
{
	public string name;
	public float height;
	public Color color;
}
