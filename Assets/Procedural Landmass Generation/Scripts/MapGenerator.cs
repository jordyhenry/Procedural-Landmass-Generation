using System.Collections;
using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour 
{
	public enum DrawMode { NoiseMap, ColorMap, Mesh };
	public DrawMode drawMode;

    public Noise.NormalizeMode normalizeMode;

	//The max of vertex per Mesh that Unity allows is 255²
	//but 241, when in the formula is divisible by 1, 2, 4, 8, 10, 12
	//which gives us a good range for mesh simplification
	public const int mapChunkSize = 241;
	[Range(0, 6)]
	public int editorPreviewLevelOfDetail;
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

	private Queue<MapThreadInfo<MapData>> mapDataThreadInfoQeue = new Queue<MapThreadInfo<MapData>>();
	private Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQeue = new Queue<MapThreadInfo<MeshData>>();

	public void DrawMapInEditor()
	{
		MapData mapData = GenerateMapData(Vector2.zero);
		MapDisplay display = FindObjectOfType<MapDisplay>();
		
		if (drawMode == DrawMode.NoiseMap)
			display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
		else if (drawMode == DrawMode.ColorMap)
			display.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
		else if (drawMode == DrawMode.Mesh){
			MeshData meshData = MeshGenerator.GenerateTerrainMeshData(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLevelOfDetail);
			Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize);
			display.DrawMesh(meshData, texture);	
		}
	}

	private MapData GenerateMapData(Vector2 center)
	{
		float [,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, center + offset, normalizeMode);

		Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
		for(int y = 0; y < mapChunkSize; y++){
			for(int x = 0; x < mapChunkSize; x++){
				float currentHeight = noiseMap[x,y];
				
				for(int i = 0; i < regions.Length; i++){
					if(currentHeight >= regions[i].height){
						colorMap[y * mapChunkSize + x] = regions[i].color;
                    }
                    else
                    {
                        break;
                    }
				}
			}
		}

		return new MapData(noiseMap, colorMap);
	}

	private void OnValidate() 
	{
		if(lacunarity < 1)
			lacunarity = 1;
		
		if(octaves < 0)
			octaves = 0;
	}

	#region THREADING IMPLEMENTATION
	private void Update() 
	{
		if(mapDataThreadInfoQeue.Count > 0)
		{
			for(int i = 0; i < mapDataThreadInfoQeue.Count; i++)
			{
				MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQeue.Dequeue();
				threadInfo.callback(threadInfo.parameter);
			}
		}

		if(meshDataThreadInfoQeue.Count > 0)
		{
			for (int i = 0; i < meshDataThreadInfoQeue.Count; i++)
			{
				MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQeue.Dequeue();
				threadInfo.callback(threadInfo.parameter);
			}
		}
	}

	public void RequestMapData(Vector2 center, Action<MapData> callback)
	{
		ThreadStart threadStart = delegate {
			MapDataThread(center, callback);
		};

		new Thread(threadStart).Start();
	}

	private void MapDataThread(Vector2 center, Action<MapData> callback)
	{
		MapData mapData = GenerateMapData(center);
		
		lock(mapDataThreadInfoQeue){
			mapDataThreadInfoQeue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
		}
	}

	public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
	{
		ThreadStart threadStart = delegate {
			MeshDataThread(mapData, lod, callback);
		};

		new Thread(threadStart).Start();
	}

	private void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
	{
		MeshData meshData = MeshGenerator.GenerateTerrainMeshData(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod);
		lock(meshDataThreadInfoQeue) {
			meshDataThreadInfoQeue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
		}
	}

	struct MapThreadInfo<T>
	{
		public readonly Action<T> callback;
		public readonly T parameter;

		public MapThreadInfo(Action <T> _callback, T _parameter)
		{
			this.callback = _callback;
			this.parameter = _parameter;
		}
	}
	#endregion 
}

[System.Serializable]
public struct TerrainType
{
	public string name;
	public float height;
	public Color color;
}

public struct MapData
{
	public readonly float[,] heightMap;
	public readonly Color[] colorMap;

	public MapData(float [,] _heightMap, Color[] _colorMap)
	{
		this.heightMap = _heightMap;
		this.colorMap = _colorMap;
	}
}
