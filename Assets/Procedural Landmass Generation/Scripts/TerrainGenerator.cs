using UnityEngine;
using System.Collections.Generic;

public class TerrainGenerator : MonoBehaviour
{
    const float viewerMoveThresholdForChunkUpdate = 25f;
	const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;
    
    public int colliderLODIndex;
	public LODInfo[] detailLevels;

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureData textureSettings;

	public Transform viewer;
	public Material mapMaterial;

	public Vector2 viewerPosition;
	Vector2 lastViewerPosition;
	float meshWorldSize;
	int chunksVisibleInViewDist;

	Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	List<TerrainChunk> visibleTerrainChuncks = new List<TerrainChunk>();

	private void Start() 
	{
        textureSettings.ApplyToMaterial(mapMaterial);
        textureSettings.UpdateMeshHeights(mapMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

        float maxViewDist = detailLevels[detailLevels.Length - 1].visibleDistThreshold;
        meshWorldSize = meshSettings.meshWorldSize;
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
                        TerrainChunk newChunck = new TerrainChunk(viewedChunkCoord, heightMapSettings, meshSettings, detailLevels, colliderLODIndex, transform, viewer, mapMaterial);
                        terrainChunkDictionary.Add(viewedChunkCoord, newChunck);
                        newChunck.OnVisibilityChanged += OnTerrainChunckVisibilityChanged;
                        newChunck.Load();
                    }
                }
			}
		}
	}

    void OnTerrainChunckVisibilityChanged(TerrainChunk chunck, bool isVisible)
    {
        if (isVisible)
            visibleTerrainChuncks.Add(chunck);
        else
            visibleTerrainChuncks.Remove(chunck);
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
