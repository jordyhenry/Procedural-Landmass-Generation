using System.Collections;
using UnityEngine;

public static class Noise  
{
    public enum NormalizeMode
    {
        LOCAL,
        GLOBAL
    }

	public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseSettings settings, Vector2 sampleCenter)
	{
		float[,] noiseMap = new float[mapWidth, mapHeight];

		//PseudoRandomNumberGeneration
		System.Random prng = new System.Random(settings.seed);

        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        //This are used to implement our seed on the map, and to add our custom offset, this way we can scroll the noise
        Vector2[] octavesOffsets = new Vector2[settings.octaves];
		for(int i = 0; i < settings.octaves; i++){
			float offsetX = prng.Next(-100000, 100000) + settings.offset.x + sampleCenter.x;
			float offsetY = prng.Next(-100000, 100000) - settings.offset.y - sampleCenter.y;
			
			octavesOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= settings.persistance;
		}

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

				for(int i = 0; i < settings.octaves; i++){
					float sampleX = (x - halfWidth + octavesOffsets[i].x) / settings.scale * frequency;
					float sampleY = (y - halfHeight + octavesOffsets[i].y) / settings.scale * frequency;

					float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
					//Changing the range of 'perlinValue' from 0 to 1 to -1 to 1
					perlinValue = perlinValue * 2 - 1;
					noiseHeight += perlinValue * amplitude;

					amplitude *= settings.persistance;
					frequency *= settings.lacunarity;
				}

				if(noiseHeight > maxLocalNoiseHeight)
                {
                    maxLocalNoiseHeight = noiseHeight;
				}

                if (noiseHeight < minLocalNoiseHeight)
                {
                    minLocalNoiseHeight = noiseHeight;
				}

				noiseMap[x, y] = noiseHeight;

                if (settings.normalizeMode == NormalizeMode.GLOBAL)
                {
                    float normalizedHeight = (noiseMap[x, y] + 1f) / (maxPossibleHeight / .9f);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }
            }
		}

        //So, when we change the range of the perlin value, to -1 to 1, we keep track of the min and max value produced by it.
        //This way we can normalize(change the range to 0 to 1) the noiseMap
        if (settings.normalizeMode == NormalizeMode.LOCAL)
        {
            for (int y = 0; y < mapHeight; y++) {
                for (int x = 0; x < mapWidth; x++) {
                    //Mathf.InverseLerp return a correspondent value mapped from 0 to 1, from a range we set in the first two parameters
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
            }
        }

		return noiseMap;
	}
}

[System.Serializable]
public class NoiseSettings
{
    public Noise.NormalizeMode normalizeMode;

    public float scale = 50;
    public int octaves = 6;

    [Range(0, 1)]
    public float persistance = .6f;
    public float lacunarity = 2;
    public int seed;
    public Vector2 offset;

    public void ValidateValues()
    {
        scale = Mathf.Max(scale, .01f);
        octaves = Mathf.Max(octaves, 1);
        lacunarity = Mathf.Max(lacunarity, 1);
        persistance = Mathf.Clamp01(persistance);
    }
}
