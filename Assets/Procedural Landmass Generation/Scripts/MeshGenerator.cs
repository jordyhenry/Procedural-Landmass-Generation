using UnityEngine;

public static class MeshGenerator 
{
	public static MeshData GenerateTerrainMeshData(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurve, int levelOfDetail)
	{
		int width = heightMap.GetLength(0);
		int height = heightMap.GetLength(1);

		//Used to set the pivot of the mesh in the center
		//otherwise it will be at bottom left
		float topLeftX = (width - 1) / -2f;
		float topLeftZ = (height - 1) / 2f;

		int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
		int verticesPerLine = (width - 1) / meshSimplificationIncrement + 1;	

		MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);
		int vertexIndex = 0;
		
		for (int y = 0; y < height; y += meshSimplificationIncrement)
		{
			for (int x = 0; x < width; x += meshSimplificationIncrement)
			{
				float yHeight = heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier;
				meshData.vertices [vertexIndex] = new Vector3(topLeftX + x, yHeight, topLeftZ - y);
				meshData.uvs [vertexIndex] = new Vector2(x / (float)width, y / (float)height);

				//When generating triangles for the mesh, we dont need to use the right 
				//neither the bottom edges
				if(x < width - 1 && y < height - 1)
				{
					//The index of the current vertex
					int a = vertexIndex;
					//The right neighbour of the vertex 'a'
					int b = vertexIndex + 1;
					//The vertex below 'a', wee add 'width' in this case, cause we need to map 'heightMap'
					//into a 1D array
					int c = vertexIndex + verticesPerLine;
					//The right neighbour of the vertex 'c'
					int d = vertexIndex + verticesPerLine + 1;

					meshData.AddTriangle(a, d, c);
					meshData.AddTriangle(d, a, b);
				}

				vertexIndex ++;
			}
		}
		return meshData;
	}
}
