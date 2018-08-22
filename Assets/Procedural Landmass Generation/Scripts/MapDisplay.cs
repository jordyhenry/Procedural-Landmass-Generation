using System.Collections;
using UnityEngine;

public class MapDisplay : MonoBehaviour 
{
	public Renderer textureRenderer;

	public void DrawTexture(Texture2D texture)
	{
		//textureRenderer.material cannot be used in this case, because its only instantiated at runtime
		textureRenderer.sharedMaterial.mainTexture = texture;
		textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
	}
	
}
