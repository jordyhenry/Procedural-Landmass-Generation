using System.Collections;
using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour 
{
	public enum DrawMode { NoiseMap, Mesh, Falloff };
	public DrawMode drawMode;

    public TerrainData terrainData;
    public NoiseData noiseData;
    public TextureData textureData;

    public Material terrainMaterial;

    /*
    The max of vertex per Mesh that Unity allows is 255²
    but 241, when in the formula is divisible by 1, 2, 4, 8, 10, 12
    which gives us a good range for mesh simplification
    Compensate the 2 for the borderMesh calculation
    */
    public int mapChunkSize {
        get
        {
            if (terrainData.useFlatShading)
                return 95;
            else
                return 239;
        }
    }
    [Range(0, 6)]
	public int editorPreviewLevelOfDetail;
	public bool autoUpdate;

    float[,] falloffMap;

	private Queue<MapThreadInfo<MapData>> mapDataThreadInfoQeue = new Queue<MapThreadInfo<MapData>>();
	private Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQeue = new Queue<MapThreadInfo<MeshData>>();
    
    void OnValuesUpdated()
    {
        if(!Application.isPlaying)
        {
            DrawMapInEditor();
        }
    }

    void OnTextureValuesUpdated()
    {
        textureData.ApplyToMaterial(terrainMaterial);
    }

    public void DrawMapInEditor()
	{
		MapData mapData = GenerateMapData(Vector2.zero);
		MapDisplay display = FindObjectOfType<MapDisplay>();

        if (drawMode == DrawMode.NoiseMap)
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        else if (drawMode == DrawMode.Mesh) {
            MeshData meshData = MeshGenerator.GenerateTerrainMeshData(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, editorPreviewLevelOfDetail, terrainData.useFlatShading);
            display.DrawMesh(meshData);
        } else if (drawMode == DrawMode.Falloff) {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(falloffMap));
        }
	}

	private MapData GenerateMapData(Vector2 center)
	{
		float [,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, noiseData.seed, noiseData.noiseScale, noiseData.octaves, noiseData.persistance, noiseData.lacunarity, center + noiseData.offset, noiseData.normalizeMode);

        if (terrainData.useFalloff)
        {
            if(falloffMap == null)
                falloffMap = FalloffGenerator.GenerateFallofMap(mapChunkSize + 2);

            for (int y = 0; y < mapChunkSize + 2; y++)
            {
                for (int x = 0; x < mapChunkSize + 2; x++)
                {
                    if (terrainData.useFalloff)
                        noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                }
            }
        }

		return new MapData(noiseMap);
	}

	private void OnValidate() 
	{
        if (terrainData != null)
        {
            terrainData.OnValuesUpdated -= OnValuesUpdated;
            terrainData.OnValuesUpdated += OnValuesUpdated;
        }

        if (noiseData != null)
        {
            noiseData.OnValuesUpdated -= OnValuesUpdated;
            noiseData.OnValuesUpdated += OnValuesUpdated;
        }

        if(textureData != null)
        {
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }
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
		MeshData meshData = MeshGenerator.GenerateTerrainMeshData(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, lod, terrainData.useFlatShading);
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

public struct MapData
{
	public readonly float[,] heightMap;

	public MapData(float [,] _heightMap)
	{
		this.heightMap = _heightMap;
	}
}
