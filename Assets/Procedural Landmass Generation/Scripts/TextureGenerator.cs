using UnityEngine;

public static class TextureGenerator
{
	public static Texture2D TextureFromColorMap(Color[] colorMap, int width, int height)
	{
		Texture2D texture = new Texture2D(width, height);
		//Fix the blurriness in the texture
		texture.filterMode = FilterMode.Point;
		//Fix the repeating colors from texture borders
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.SetPixels(colorMap);
		texture.Apply();

		return texture;
	}

	public static Texture2D TextureFromHeightMap(float[,] heightMap)
	{
		int width = heightMap.GetLength(0);
		int height = heightMap.GetLength(1);

		Color[] colorMap = new Color[width * height];

		for(int y = 0; y < height; y++){
			for(int x = 0; x < width; x++){
				//Finding the correpondent position of a 2d array value on a 1d array
				int index = y * width + x;
				colorMap[index] = Color.Lerp(Color.black, Color.white, heightMap[x, y]);
			}
		}
		
		return TextureFromColorMap(colorMap, width, height);
	}
}
