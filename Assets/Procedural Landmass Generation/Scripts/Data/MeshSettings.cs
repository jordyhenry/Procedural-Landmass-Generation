using System.Collections;
using UnityEngine;

[CreateAssetMenu]
public class MeshSettings : UpdatableData
{
    public const int numberOfSupportedLODs = 5;
    public const int numberOfSupportedChunckSizes = 9;
    public const int numberOfSupportedFlatShadedChunckSizes = 3;
    public static readonly int[] supportedChunckSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };

    public float meshScale = 2f;
    public bool useFlatShading;

    [Range(0, numberOfSupportedChunckSizes - 1)]
    public int chunckSizeIndex;
    [Range(0, numberOfSupportedFlatShadedChunckSizes - 1)]
    public int flatShadedChunckSizeIndex;

    /*
        number of vertices  per line of mesh rendered at LOD 0.
        Includes the two extra vertices that are exlcuded from final mesh, but used for calculating normals
    */
    public int numberOfVerticesPerLine
    {
        get
        {
            return supportedChunckSizes[(useFlatShading) ? flatShadedChunckSizeIndex : chunckSizeIndex] + 1;
        }
    }

    public float meshWorldSize
    {
        get
        {
            return (numberOfVerticesPerLine - 3) * meshScale;
        }
    }
}
