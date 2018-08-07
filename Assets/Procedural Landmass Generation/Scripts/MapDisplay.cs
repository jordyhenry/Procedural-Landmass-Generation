using System.Collections;
using UnityEngine;

public class MapDisplay : MonoBehaviour 
{
	public Renderer textureRenderer;

	public void DrawNoiseMap(float[,] noiseMap)
	{
		int width = noiseMap.GetLength(0);
		int height = noiseMap.GetLength(1);

		Texture2D texture = new Texture2D(width, height);

		Color[] colorMap = new Color[width * height];

		for(int y = 0; y < height; y++){
			for(int x = 0; x < width; x++){
				//Finding the correpondent position of a 2d array value on a 1d array
				int index = y * width + x;
				colorMap[index] = Color.Lerp(Color.black, Color.white, noiseMap[x, y]);
			}
		}
		texture.SetPixels(colorMap);
		texture.Apply();

		//textureRenderer.material cannot be used in this case, because its only instantiated at runtime
		textureRenderer.sharedMaterial.mainTexture = texture;
		textureRenderer.transform.localScale = new Vector3(width, 1, height);
	}
	
}
