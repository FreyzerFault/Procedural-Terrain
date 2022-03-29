using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
	public Transform Player;
	public GameObject TerrainPrefab;

	public Vector2 chunkSize = new Vector2(241, 241);

	// Distancia desde el chunk central al borde
	public int renderDistance = 6;

	public int seed = DateTime.Now.Millisecond;

	private Vector2[,] chunks;

	private Vector2 playerChunk = new Vector2(0,0);

	private Dictionary<Vector2, NoiseMeshDisplay> terrains = new Dictionary<Vector2, NoiseMeshDisplay>();

	// Start is called before the first frame update
	void Start()
	{
		Clear();
		LoadChunks();
	}

	// Update is called once per frame
	void Update()
	{
		// Cuando cambie de chunk recalculamos to
		if (playerChunk != getChunk(Player.position))
		{
			playerChunk = getChunk(Player.position);

			LoadChunks();
		}

		Vector2 playerPosition = getLocalPos(Player.position);
	}

	// Load ALL chunks (Update if no need to Create)
	public void LoadChunks()
	{
		if (Player == null)
			Player = GameObject.FindGameObjectWithTag("Player").transform;

		playerChunk = getChunk(Player.position);

		int borderLength = GetBorderLength();

		chunks = new Vector2[borderLength,borderLength];

		// Volvemos a generar los chunks con distinto LOD segun su distancia
		for (int x = 0; x < borderLength; x++)
		for (int y = 0; y < borderLength; y++)
		{
			Vector2 chunk = CreateChunk(x,y);

			// Si ya esta cargado actualizamos su LOD y su malla solamente
			if (terrains.ContainsKey(chunk))
			{
				UpdateChunk(chunk);
			}
			else
			{
				CreateTerrain(chunk);
			}
		}
	}

	// Update ALL chunks
	public void ReloadChunks()
	{
		int borderLength = GetBorderLength();
		for (int x = 0; x < borderLength; x++)
		for (int y = 0; y < borderLength; y++)
		{
			UpdateChunk(CreateChunk(x,y));
		}
	}

	private void UpdateChunk(Vector2 chunk)
	{
		NoiseMeshDisplay terrain = terrains[chunk];
		terrain.UpdateLOD(Player.position);
		terrain.CreateMesh();
	}

	private Vector2 CreateChunk(int x, int y)
	{
		Vector2 chunk = new Vector2(
			x - renderDistance + playerChunk.x,
			y - renderDistance + playerChunk.y
		);
		return chunks[x, y] = chunk;
	}
	
	private void CreateTerrain(Vector2 chunk)
	{
		OffsetTerrain(chunk);
		UpdateLOD(chunk);

		Vector2 globalPos = getGlobalPos(chunk);
		terrains.Add(chunk,
			Instantiate(
				TerrainPrefab,
				new Vector3(globalPos.x, 0, globalPos.y),
				Quaternion.identity,
				transform
			).GetComponent<NoiseMeshDisplay>()
		);

		// Si estamos en el Editor no se inicializara si no lo forzamos
		if (Application.isEditor)
			terrains[chunk].CreateTerrain();
	}

	private void OffsetTerrain(Vector2 chunk)
	{
		TerrainPrefab.GetComponent<NoiseMeshDisplay>().offset = chunk * 5.9f;
	}
	private void UpdateLOD(Vector2 chunk)
	{
		int LOD = Mathf.FloorToInt((chunk - playerChunk).magnitude);
		TerrainPrefab.GetComponent<NoiseMeshDisplay>().LOD = LOD;
	}


	private int GetBorderLength()
	{
		return renderDistance * 2 + 1;
	}


	Vector2 getChunk(Vector2 pos)
	{
		return new Vector2(
			Mathf.Round(pos.x / chunkSize.x),
			Mathf.Round(pos.y / chunkSize.y)
		);
	}
	Vector2 getChunk(Vector3 pos)
	{
		return new Vector2(
			Mathf.Round(pos.x / chunkSize.x),
			Mathf.Round(pos.z / chunkSize.y)
		);
	}

	Vector2 getLocalPos(Vector2 pos)
	{
		return pos - getGlobalPos(getChunk(pos));
	}
	Vector2 getLocalPos(Vector3 pos)
	{
		return new Vector2(pos.x, pos.z) - getGlobalPos(getChunk(pos));
	}

	Vector2 getGlobalPos(Vector2 chunkPos)
	{
		return chunkPos * (new Vector2(chunkSize.x - 1, chunkSize.y - 1));
	}

	public void ResetRandomSeed()
	{
		seed = DateTime.Now.Millisecond;
	}

	public void Clear()
	{
		NoiseMeshDisplay[] children = GetComponentsInChildren<NoiseMeshDisplay>();
		foreach (NoiseMeshDisplay child in children)
		{
			Destroy(child.gameObject);
		}
		terrains.Clear();
	}

	public void ClearImmediate()
	{
		NoiseMeshDisplay[] children = GetComponentsInChildren<NoiseMeshDisplay>();
		foreach (NoiseMeshDisplay child in children)
		{
			DestroyImmediate(child.gameObject);
		}
		terrains.Clear();
	}
}
