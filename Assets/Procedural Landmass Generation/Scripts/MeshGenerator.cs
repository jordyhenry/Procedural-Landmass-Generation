﻿using UnityEngine;

public static class MeshGenerator 
{
    

    public static MeshData GenerateTerrainMeshData(float[,] heightMap, MeshSettings meshSettings, int levelOfDetail)
	{
		int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;

        int borderedSize = heightMap.GetLength(0);
        int meshSize = borderedSize - 2 * meshSimplificationIncrement;
        int meshSizeUnsimplified = borderedSize - 2;

		//Used to set the pivot of the mesh in the center
		//otherwise it will be at bottom left
		float topLeftX = (meshSizeUnsimplified - 1) / -2f;
		float topLeftZ = (meshSizeUnsimplified - 1) / 2f;

		
		int verticesPerLine = (meshSize - 1) / meshSimplificationIncrement + 1;	

		MeshData meshData = new MeshData(verticesPerLine, meshSettings.useFlatShading);

        int[,] vertexIndicesMap = new int[borderedSize, borderedSize];
        int meshVertexIndex = 0;
        int borderVertexIndex = -1;



        for (int y = 0; y < borderedSize; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x += meshSimplificationIncrement)
            {
                bool isBorderVertex = (y == 0) || (y == borderedSize - 1) || (x == 0) || (x == borderedSize - 1);

                if (isBorderVertex)
                {
                    vertexIndicesMap[x, y] = borderVertexIndex;
                    borderVertexIndex--;
                }
                else
                {
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++; 
                }
            }
        }

		for (int y = 0; y < borderedSize; y += meshSimplificationIncrement)
		{
			for (int x = 0; x < borderedSize; x += meshSimplificationIncrement)
			{
                int vertexIndex = vertexIndicesMap[x, y];
				Vector2 percent = new Vector2((x - meshSimplificationIncrement) / (float)meshSize, (y - meshSimplificationIncrement) / (float)meshSize);

                float yHeight = heightMap[x, y];
                Vector3 vertexPosition = new Vector3((topLeftX + percent.x * meshSizeUnsimplified) * meshSettings.meshScale, yHeight, (topLeftZ - percent.y * meshSizeUnsimplified) * meshSettings.meshScale);


                meshData.AddVertex(vertexPosition, percent, vertexIndex);

				//When generating triangles for the mesh, we dont need to use the right 
				//neither the bottom edges
				if(x < borderedSize - 1 && y < borderedSize - 1)
				{
					int a = vertexIndicesMap[x, y];
					int b = vertexIndicesMap[x + meshSimplificationIncrement, y];
                    int c = vertexIndicesMap[x, y + meshSimplificationIncrement];
                    int d = vertexIndicesMap[x + meshSimplificationIncrement, y + meshSimplificationIncrement];

                    meshData.AddTriangle(a, d, c);
					meshData.AddTriangle(d, a, b);
				}

				vertexIndex ++;
			}
		}

        meshData.Finalize();

		return meshData;
	}
}
