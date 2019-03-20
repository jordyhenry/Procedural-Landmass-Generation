using UnityEngine;
using System.Collections.Generic;

public class EndlessTerrain : MonoBehaviour
{
    const float viewerMoveThresholdForChunkUpdate = 25f;
	const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;
    const float colliderGenerationDistanceThreshold = 5;

    public int colliderLODIndex;
	public LODInfo[] detailLevels;
	public static float maxViewDist;

	public Transform viewer;
	public Material mapMaterial;

	public static Vector2 viewerPosition;
	Vector2 lastViewerPosition;
	static MapGenerator mapGenerator;
	float meshWorldSize;
	int chunksVisibleInViewDist;

	Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	static List<TerrainChunk> visibleTerrainChuncks = new List<TerrainChunk>();

	private void Start() 
	{
		mapGenerator = FindObjectOfType<MapGenerator>();

		maxViewDist = detailLevels[detailLevels.Length - 1].visibleDistThreshold;
        meshWorldSize = mapGenerator.meshSettings.meshWorldSize;
		chunksVisibleInViewDist = Mathf.RoundToInt(maxViewDist / meshWorldSize);	

		UpdateVisibleChunks();
	}

	private void Update() 
	{
        //Dividing by the scale of the chunks, this way the movement of the player
        //will be relative to the scale of the map
		viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

        if(viewerPosition != lastViewerPosition)
        {
            foreach (TerrainChunk chunck in visibleTerrainChuncks)
            {
                chunck.UpdateCollisionMesh();
            }
        }

		if((lastViewerPosition - viewerPosition).SqrMagnitude() > sqrViewerMoveThresholdForChunkUpdate){
			lastViewerPosition = viewerPosition;
			UpdateVisibleChunks();
		}
	}

	void UpdateVisibleChunks()
	{
        HashSet<Vector2> alreadyUpdatedChunckCoords = new HashSet<Vector2>();
        for (int i = visibleTerrainChuncks.Count - 1; i >= 0; i--)
        {
            alreadyUpdatedChunckCoords.Add(visibleTerrainChuncks[i].coord);
            visibleTerrainChuncks[i].UpdateTerrainChunk();
        }


		int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / meshWorldSize);
		int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / meshWorldSize);

		for(int yOffset = -chunksVisibleInViewDist; yOffset <= chunksVisibleInViewDist; yOffset++){
			for(int xOffset = -chunksVisibleInViewDist; xOffset <= chunksVisibleInViewDist; xOffset++){
				Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (!alreadyUpdatedChunckCoords.Contains(viewedChunkCoord))
                {
                    if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                    {
                        terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                    }
                    else
                    {
                        terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, meshWorldSize, detailLevels, colliderLODIndex, transform, mapMaterial));
                    }
                }
			}
		}
	}

	public class TerrainChunk
	{

        public Vector2 coord;

		GameObject meshObject;
		Vector2 sampleCenter;
		Bounds bounds;

		MeshRenderer meshRenderer;
		MeshFilter meshFilter;
        MeshCollider meshCollider;

		LODInfo[] detailLevels;
		LODMesh[] lodMeshes;
        int colliderLODIndex;

		HeightMap mapData;
		bool mapDataReceived;
		int previousLODIndex = -1;
        bool hasSetCollider;

		public TerrainChunk(Vector2 coord, float meshWorldSize, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Material material)
		{
            this.coord = coord;
			this.detailLevels = detailLevels;
            this.colliderLODIndex = colliderLODIndex;

			sampleCenter = coord * meshWorldSize / mapGenerator.meshSettings.meshScale;
            Vector2 position = coord * meshWorldSize;
            bounds = new Bounds(position, Vector2.one * meshWorldSize);

			meshObject = new GameObject("Terrain Chunk");
			meshRenderer = meshObject.AddComponent<MeshRenderer>();
			meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshRenderer.material = material;

            meshObject.transform.position = new Vector3(position.x, 0, position.y);
			meshObject.transform.parent = parent;
			SetVisible(false);

			lodMeshes = new LODMesh[detailLevels.Length];
			for (int i = 0; i < detailLevels.Length; i++)
			{
				lodMeshes[i] = new LODMesh(detailLevels[i].lod);
                lodMeshes[i].updateCallback += UpdateTerrainChunk;
                if (i == colliderLODIndex)
                    lodMeshes[i].updateCallback += UpdateCollisionMesh;

			}

			mapGenerator.RequestHeightMapData(sampleCenter, OnMapDataReceived);
		}

		private void OnMapDataReceived(HeightMap mapData)
		{
			this.mapData = mapData;
			mapDataReceived = true;

			UpdateTerrainChunk();	
		}

		public void UpdateTerrainChunk()
		{
			if(!mapDataReceived) return;
			float viewerDistFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));

            bool wasVisible = IsVisible();
			bool visible = viewerDistFromNearestEdge <= maxViewDist;

			if(visible)
			{
				int lodIndex = 0;

				for (int i = 0; i < detailLevels.Length - 1; i++)
				{
					if(viewerDistFromNearestEdge > detailLevels[i].visibleDistThreshold)
						lodIndex = i + 1;
					else
						break;
				}

				if(lodIndex != previousLODIndex)
				{
					LODMesh lodMesh = lodMeshes[lodIndex];
					if(lodMesh.hasMesh){
						previousLODIndex = lodIndex;
						meshFilter.mesh = lodMesh.mesh;
					}else if(!lodMesh.hasRequestedMesh){
						lodMesh.RequestMesh(mapData);
					}
				}
			}

            if (wasVisible != visible)
            {
                if (visible)
                    visibleTerrainChuncks.Add(this);
                else
                    visibleTerrainChuncks.Remove(this);

                SetVisible(visible);
            }
		}

        public void UpdateCollisionMesh()
        {
            if (hasSetCollider) return;

            float sqrDstFromViewerToEdge = bounds.SqrDistance(viewerPosition);

            if (sqrDstFromViewerToEdge < detailLevels[colliderLODIndex].sqrVisibleDstThreshold)
                if (!lodMeshes[colliderLODIndex].hasRequestedMesh)
                    lodMeshes[colliderLODIndex].RequestMesh(mapData);

            if(sqrDstFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold)
            {
                if (lodMeshes[colliderLODIndex].hasMesh)
                {
                    meshCollider.sharedMesh = lodMeshes[colliderLODIndex].mesh;
                    hasSetCollider = true;
                }
            }
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

	public class LODMesh
	{
		public Mesh mesh;
		public bool hasRequestedMesh;
		public bool hasMesh;
		int lod;
		public event System.Action updateCallback;

		public LODMesh(int _lod)
		{
			this.lod = _lod;
		}

		private void OnMeshDataReceived(MeshData meshData)
		{
			mesh = meshData.CreateMesh();
			hasMesh = true;

			updateCallback();
		}

		public void RequestMesh(HeightMap mapData)
		{
			hasRequestedMesh = true;
			mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
		}
	}

	[System.Serializable]
	public struct LODInfo
	{
        [Range(0, MeshSettings.numberOfSupportedLODs - 1)]
		public int lod;
		public float visibleDistThreshold;

        public float sqrVisibleDstThreshold
        {
            get
            {
                return visibleDistThreshold * visibleDistThreshold;
            }
        }
	}
}
