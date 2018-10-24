using System.Collections;
using UnityEngine;

public static class Noise  
{
    public enum NormalizeMode
    {
        LOCAL,
        GLOBAL
    }

	public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset, NormalizeMode normalizeMode)
	{
		float[,] noiseMap = new float[mapWidth, mapHeight];

		//PseudoRandomNumberGeneration
		System.Random prng = new System.Random(seed);

        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        //This are used to implement our seed on the map, and to add our custom offset, this way we can scroll the noise
        Vector2[] octavesOffsets = new Vector2[octaves];
		for(int i = 0; i < octaves; i++){
			float offsetX = prng.Next(-100000, 100000) + offset.x;
			float offsetY = prng.Next(-100000, 100000) - offset.y;
			
			octavesOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= persistance;
		}

		if(scale <= 0) 
			scale = .0001f;

		float maxNoiseHeight = float.MinValue;
		float minNoiseHeight = float.MaxValue;
		
		//This variables are used to scale the noise from the center of the map instead of the corner, like it does without this
		float halfWidth = mapWidth / 2f;
		float halfHeight = mapHeight / 2f;

        float minLocalNoiseHeight = minNoiseHeight;
        float maxLocalNoiseHeight = maxNoiseHeight;
        for (int y = 0; y < mapHeight; y++){
			for(int x = 0; x < mapWidth; x++){

				amplitude = 1;
				frequency = 1;
				float noiseHeight = 0;

				for(int i = 0; i < octaves; i++){
					float sampleX = (x - halfWidth + octavesOffsets[i].x) / scale * frequency;
					float sampleY = (y - halfHeight + octavesOffsets[i].y) / scale * frequency;

					float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
					//Changing the range of 'perlinValue' from 0 to 1 to -1 to 1
					perlinValue = perlinValue * 2 - 1;
					noiseHeight += perlinValue * amplitude;

					amplitude *= persistance;
					frequency *= lacunarity;
				}

				if(noiseHeight > maxLocalNoiseHeight)
                {
                    maxLocalNoiseHeight = noiseHeight;
				}else if(noiseHeight < minLocalNoiseHeight)
                {
                    minLocalNoiseHeight = noiseHeight;
				}

				noiseMap[x, y] = noiseHeight;
			}
		}

		//So, when we change the range of the perlin value, to -1 to 1, we keep track of the min and max value produced by it.
		//This way we can normalize(change the range to 0 to 1) the noiseMap
		for(int y = 0; y < mapHeight; y++){
			for(int x = 0; x < mapWidth; x++){
                if (normalizeMode == NormalizeMode.LOCAL)
                {
                    //Mathf.InverseLerp return a correspondent value mapped from 0 to 1, from a range we set in the first two parameters
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
                else
                {
                    //
                    float normalizedHeight = noiseMap[x, y] + 1f / (2f * maxPossibleHeight / 1.5f);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }
			}
		}

		return noiseMap;
	}
}
