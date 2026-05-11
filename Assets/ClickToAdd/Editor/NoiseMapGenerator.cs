using UnityEngine;
using System.Collections;
using System.Threading;

public static class NoiseMapGenerator{

	//Method gets individual point of noise
	public static float GetNoise(int octaves, int seed, float damping, float lacunarity, float x, float y, float xOffset = 0, float yOffset = 0)
	{
		float noise = 0f;

		for (int i = 0; i < octaves; i++) 
		{
			float freq = Mathf.Pow(lacunarity, i);
			noise += 1 / freq * Mathf.PerlinNoise(x * freq + xOffset, y * freq + yOffset);			
		}

		//keep noise in 
		//noise = Mathf.InverseLerp(1f, 0f, noise);
		noise = Mathf.Lerp(0f,0.8f, noise);
		noise = Mathf.Pow(noise, damping);

		return noise;
	}

	/// <summary>
	/// Draws noise map
	/// </summary>
	/// <param name="_texture">Texture.</param>
	public static void DrawMap(Texture2D texture, float[,] map)
	{
		Color[] tempArray = texture.GetPixels();

		for (int j = 0; j < texture.width; j++) 
		{
			
			for (int i = 0; i < texture.width; i++) 
			{
				float tempValue =  map[i,j];//GetNoise(octaves, seed, damping, j / (float)texture.width , i / (float)texture.width);
				tempArray[i + j * texture.width] = new Color(tempValue, tempValue, tempValue);
			}
		}

		texture.SetPixels(tempArray);
		texture.Apply();
	}

	/// <summary>
	/// Return 2D float array of noise
	/// </summary>
	/// <returns>The map.</returns>
	/// <param name="mapSize">Map size.</param>
	/// <param name="octaves">Octaves.</param>
	public static float[,] GetNoiseMap(int mapSize, int octaves, int seed, float damping, float lacunarity, float xOffset = 0f, float yOffset = 0f)
	{
		float[,] map = new float[mapSize,mapSize];

		for (int j = 0; j < mapSize; j++) 
		{
			for (int i = 0; i < mapSize; i++) 
			{
				map[i,j] = GetNoise(octaves, seed , damping, lacunarity, j / (float) mapSize, i / (float) mapSize, yOffset, yOffset);
			}
		}

		return map;
	}
}
