using System.Collections;
using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour 
{
	public enum DrawMode { NoiseMap, Mesh, Falloff };
	public DrawMode drawMode;

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureData textureData;

    public Material terrainMaterial;

    [Range(0, MeshSettings.numberOfSupportedLODs - 1)]
	public int editorPreviewLevelOfDetail;
	public bool autoUpdate;

    float[,] falloffMap;

	private Queue<MapThreadInfo<HeightMap>> heightMapThreadInfoQeue = new Queue<MapThreadInfo<HeightMap>>();
	private Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQeue = new Queue<MapThreadInfo<MeshData>>();

    private void Start()
    {
        textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
    }

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
        textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numberOfVerticesPerLine, meshSettings.numberOfVerticesPerLine, heightMapSettings, Vector2.zero);
		MapDisplay display = FindObjectOfType<MapDisplay>();

        if (drawMode == DrawMode.NoiseMap)
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap.values));
        else if (drawMode == DrawMode.Mesh) {
            MeshData meshData = MeshGenerator.GenerateTerrainMeshData(heightMap.values, meshSettings, editorPreviewLevelOfDetail);
            display.DrawMesh(meshData);
        } else if (drawMode == DrawMode.Falloff) {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(falloffMap));
        }
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

        if(textureData != null)
        {
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }
    }

	#region THREADING IMPLEMENTATION
	private void Update() 
	{
		if(heightMapThreadInfoQeue.Count > 0)
		{
			for(int i = 0; i < heightMapThreadInfoQeue.Count; i++)
			{
				MapThreadInfo<HeightMap> threadInfo = heightMapThreadInfoQeue.Dequeue();
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

	public void RequestHeightMapData(Vector2 center, Action<HeightMap> callback)
	{
        //textureData.UpdateMeshHeights(terrainMaterial, meshSettings.minHeight, meshSettings.maxHeight);
        ThreadStart threadStart = delegate {
			HeightMapThread(center, callback);
		};

		new Thread(threadStart).Start();
	}

	private void HeightMapThread(Vector2 center, Action<HeightMap> callback)
	{
		HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numberOfVerticesPerLine, meshSettings.numberOfVerticesPerLine, heightMapSettings, center);

        lock (heightMapThreadInfoQeue){
			heightMapThreadInfoQeue.Enqueue(new MapThreadInfo<HeightMap>(callback, heightMap));
		}
	}

	public void RequestMeshData(HeightMap heightMap, int lod, Action<MeshData> callback)
	{
		ThreadStart threadStart = delegate {
			MeshDataThread(heightMap, lod, callback);
		};

		new Thread(threadStart).Start();
	}

	private void MeshDataThread(HeightMap heightMap, int lod, Action<MeshData> callback)
	{
		MeshData meshData = MeshGenerator.GenerateTerrainMeshData(heightMap.values,meshSettings, lod);
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


