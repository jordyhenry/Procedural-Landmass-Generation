using UnityEngine;

public class TerrainChunk
{
    const float colliderGenerationDistanceThreshold = 5;
    public event System.Action<TerrainChunk, bool> OnVisibilityChanged;
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

    HeightMap heightMap;
    bool heightMapReceived;
    int previousLODIndex = -1;
    bool hasSetCollider;
    float maxViewerDst;

    HeightMapSettings heightMapSettings;
    MeshSettings meshSettings;

    Transform viewer;

    public TerrainChunk(Vector2 coord, HeightMapSettings heightMapSettings, MeshSettings meshSettings, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Transform viewer, Material material)
    {
        this.coord = coord;
        this.detailLevels = detailLevels;
        this.colliderLODIndex = colliderLODIndex;
        this.heightMapSettings = heightMapSettings;
        this.meshSettings = meshSettings;
        this.viewer = viewer;

        sampleCenter = coord * meshSettings.meshWorldSize / meshSettings.meshScale;
        Vector2 position = coord * meshSettings.meshWorldSize;
        bounds = new Bounds(position, Vector2.one * meshSettings.meshWorldSize);

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
        maxViewerDst = detailLevels[detailLevels.Length - 1].visibleDistThreshold;

    }

    public void Load()
    {
        ThreadedDataRequester.RequestData(
            () => HeightMapGenerator.GenerateHeightMap(meshSettings.numberOfVerticesPerLine, meshSettings.numberOfVerticesPerLine, heightMapSettings, sampleCenter),
            OnHeightMapReceived
        );
    }

    private void OnHeightMapReceived(object heightMapObject)
    {
        this.heightMap = (HeightMap)heightMapObject;
        heightMapReceived = true;

        UpdateTerrainChunk();
    }

    Vector2 viewerPosition
    {
        get
        {
            return new Vector2(viewer.position.x, viewer.position.z);
        }
    }

    public void UpdateTerrainChunk()
    {
        if (!heightMapReceived) return;
        float viewerDistFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));

        bool wasVisible = IsVisible();
        bool visible = viewerDistFromNearestEdge <= maxViewerDst;

        if (visible)
        {
            int lodIndex = 0;

            for (int i = 0; i < detailLevels.Length - 1; i++)
            {
                if (viewerDistFromNearestEdge > detailLevels[i].visibleDistThreshold)
                    lodIndex = i + 1;
                else
                    break;
            }

            if (lodIndex != previousLODIndex)
            {
                LODMesh lodMesh = lodMeshes[lodIndex];
                if (lodMesh.hasMesh)
                {
                    previousLODIndex = lodIndex;
                    meshFilter.mesh = lodMesh.mesh;
                }
                else if (!lodMesh.hasRequestedMesh)
                {
                    lodMesh.RequestMesh(heightMap, meshSettings);
                }
            }
        }

        if (wasVisible != visible)
        {
            SetVisible(visible);

            if (OnVisibilityChanged != null)
                OnVisibilityChanged(this, visible); 
        }
    }

    public void UpdateCollisionMesh()
    {
        if (hasSetCollider) return;

        float sqrDstFromViewerToEdge = bounds.SqrDistance(viewerPosition);

        if (sqrDstFromViewerToEdge < detailLevels[colliderLODIndex].sqrVisibleDstThreshold)
            if (!lodMeshes[colliderLODIndex].hasRequestedMesh)
                lodMeshes[colliderLODIndex].RequestMesh(heightMap, meshSettings);

        if (sqrDstFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold)
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

    private void OnMeshDataReceived(object meshDataObject)
    {
        mesh = ((MeshData)meshDataObject).CreateMesh();
        hasMesh = true;

        updateCallback();
    }

    public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings)
    {
        hasRequestedMesh = true;
        ThreadedDataRequester.RequestData(
            () => MeshGenerator.GenerateTerrainMeshData(heightMap.values, meshSettings, lod),
            OnMeshDataReceived
        );
    }
}