using System.Collections;
using UnityEngine;

public static class Noise  
{
	public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset)
	{
		float[,] noiseMap = new float[mapWidth, mapHeight];

		//PseudoRandomNumberGeneration
		System.Random prng = new System.Random(seed);
		
		//This are used to implement our seed to the map, and to add our custom offset, this way we can scroll the noise
		Vector2[] octavesOffsets = new Vector2[octaves];
		for(int i = 0; i < octaves; i++){
			float offsetX = prng.Next(-100000, 100000) + offset.x;
			float offsetY = prng.Next(-100000, 100000) + offset.y;
			
			octavesOffsets[i] = new Vector2(offsetX, offsetY);
		}

		if(scale <= 0) 
			scale = .0001f;

		float maxNoiseHeight = float.MinValue;
		float minNoiseHeight = float.MaxValue;
		
		//This variables are used to scale the noise from the center of the map instead of the corner, like it does without this
		float halfWidth = mapWidth / 2f;
		float halfHeight = mapHeight / 2f;

		for(int y = 0; y < mapHeight; y++){
			for(int x = 0; x < mapWidth; x++){

				float amplitude = 1;
				float frequency = 1;
				float noiseHeight = 0;

				for(int i = 0; i < octaves; i++){
					float sampleX = (x - halfWidth) / scale * frequency + octavesOffsets[i].x;
					float sampleY = (y - halfHeight) / scale * frequency + octavesOffsets[i].y;

					float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
					//Changing the range of 'perlinValue' from 0 to 1 to -1 to 1
					perlinValue = perlinValue * 2 - 1;
					noiseHeight += perlinValue * amplitude;

					amplitude *= persistance;
					frequency *= lacunarity;
				}

				if(noiseHeight > maxNoiseHeight){
					maxNoiseHeight = noiseHeight;
				}else if(noiseHeight < minNoiseHeight){
					minNoiseHeight = noiseHeight;
				}

				noiseMap[x, y] = noiseHeight;
			}
		}

		//So, when we change the range of the perlin value, to -1 to 1, we keep track of the min and max value produced by it.
		//This way we can normalize(change the range to 0 to 1) the noiseMap
		for(int y = 0; y < mapHeight; y++){
			for(int x = 0; x < mapWidth; x++){
				//Mathf.InverseLerp return a correspondent value mapped from 0 to 1, from a range we set in the first two parameters
				noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
			}
		}

		return noiseMap;
	}
}
