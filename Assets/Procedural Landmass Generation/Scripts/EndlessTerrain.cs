using UnityEngine;
using System.Collections.Generic;

public class EndlessTerrain : MonoBehaviour
{
	public const float maxViewDist = 300;
	public Transform viewer;
	public Material mapMaterial;

	public static Vector2 viewerPosition;
	static MapGenerator mapGenerator;
	int chunkSize;
	int chunksVisibleInViewDist;

	Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

	private void Start() 
	{
		mapGenerator = FindObjectOfType<MapGenerator>();
		chunkSize = MapGenerator.mapChunkSize - 1;
		chunksVisibleInViewDist = Mathf.RoundToInt(maxViewDist / chunkSize);	
	}

	private void Update() 
	{
		viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
		UpdateVisibleChunks();
	}

	void UpdateVisibleChunks()
	{
		for(int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
			terrainChunksVisibleLastUpdate[i].SetVisible(false);

		terrainChunksVisibleLastUpdate.Clear();

		int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
		int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

		for(int yOffset = -chunksVisibleInViewDist; yOffset <= chunksVisibleInViewDist; yOffset++){
			for(int xOffset = -chunksVisibleInViewDist; xOffset <= chunksVisibleInViewDist; xOffset++){
				Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

				if(terrainChunkDictionary.ContainsKey(viewedChunkCoord)){
					terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
					
					if(terrainChunkDictionary[viewedChunkCoord].IsVisible())
						terrainChunksVisibleLastUpdate.Add(terrainChunkDictionary[viewedChunkCoord]);
				}else{
					terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, transform, mapMaterial));
				}
			}
		}
	}

	public class TerrainChunk
	{
		GameObject meshObject;
		Vector2 position;
		Bounds bounds;

		MeshRenderer meshRenderer;
		MeshFilter meshFilter;

		public TerrainChunk(Vector2 coord, int size, Transform parent, Material material)
		{
			position = coord * size;
			bounds = new Bounds(position, Vector2.one * size);
			Vector3 positionV3 = new Vector3(position.x, 0, position.y);

			meshObject = new GameObject("Terrain Chunk");
			meshRenderer = meshObject.AddComponent<MeshRenderer>();
			meshRenderer.material = material;
			meshFilter = meshObject.AddComponent<MeshFilter>();

			meshObject.transform.position = positionV3;
			meshObject.transform.parent = parent;
			SetVisible(false);

			mapGenerator.RequestMapData(OnMapDataReceived);
		}

		private void OnMapDataReceived(MapData mapData)
		{
			mapGenerator.RequestMeshData(mapData, OnMeshDataReceived);
		}

		private void OnMeshDataReceived(MeshData meshData)
		{
			meshFilter.mesh = meshData.CreateMesh();
		}


		public void UpdateTerrainChunk()
		{
			float viewerDistFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
			bool visible = viewerDistFromNearestEdge <= maxViewDist;
			SetVisible(visible);
		}

		public void SetVisible(bool _visible)
		{
			meshObject.SetActive(_visible);
		}

		public bool IsVisible()
		{
			return meshObject.activeSelf;
		}
	}
}
