using System;
using System.Collections.Generic;
using UnityEngine;

public class ParanormalSpawner
{
	public static void Init(float dist = 5f, int attempts = 5, int layers = 10, float layerWidth = 1f, string name = "SuperNatural/Mariner")
	{
		ParanormalSpawner.m_distance = dist;
		ParanormalSpawner.m_attemptsPerLayer = attempts;
		ParanormalSpawner.m_layers = layers;
		ParanormalSpawner.m_layerWidth = layerWidth;
		ParanormalSpawner.m_marinerName = name;
		ParanormalSpawner.m_marinerPrefab = (Resources.Load(ParanormalSpawner.m_marinerName) as GameObject);
	}

	public static void SpawnMarinerNearestDark(string name)
	{
		ParanormalSpawner.m_marinerName = name;
		ParanormalSpawner.m_marinerPrefab = (Resources.Load(ParanormalSpawner.m_marinerName) as GameObject);
		ParanormalSpawner.SpawnMarinerNearestDark();
	}

	public static void SpawnMarinerNearestDark(Mariner.State state)
	{
		GameObject gameObject = ParanormalSpawner.SpawnMarinerNearestDark();
		if (gameObject != null)
		{
			Mariner component = gameObject.GetComponent<Mariner>();
			component.m_state = state;
		}
	}

	public static GameObject SpawnMarinerNearestDark(float dist, int attempts, int layers, float layerWidth, string name)
	{
		ParanormalSpawner.m_distance = dist;
		ParanormalSpawner.m_attemptsPerLayer = attempts;
		ParanormalSpawner.m_layers = layers;
		ParanormalSpawner.m_layerWidth = layerWidth;
		ParanormalSpawner.m_marinerName = name;
		return ParanormalSpawner.SpawnMarinerNearestDark();
	}

	public static GameObject SpawnMarinerNearestDark()
	{
		Vector3 vector = default(Vector3);
		if (CrewSim.GetSelectedCrew() != null)
		{
			vector = CrewSim.GetSelectedCrew().tf.position;
		}
		Ship shipCurrentLoaded = CrewSim.shipCurrentLoaded;
		CondTrigger ct = new CondTrigger();
		for (int i = 0; i < ParanormalSpawner.m_layers; i++)
		{
			for (int j = 0; j < ParanormalSpawner.m_attemptsPerLayer; j++)
			{
				Vector2 vector2 = UnityEngine.Random.insideUnitCircle.normalized * (ParanormalSpawner.m_distance + (float)i * ParanormalSpawner.m_layerWidth);
				vector2 += new Vector2(vector.x, vector.y);
				List<CondOwner> list = new List<CondOwner>();
				shipCurrentLoaded.GetCOsAtWorldCoords1(vector2, ct, true, true, list);
				if (list.Count <= 0)
				{
					GameObject result = ParanormalSpawner.SpawnMariner(new Vector3(vector2.x, vector2.y, -2f));
					Debug.Log("Spawned mariner on " + (i * ParanormalSpawner.m_attemptsPerLayer + j) + "th attempt!");
					return result;
				}
			}
		}
		Debug.Log("Went through all possible cycles of spawning mariner and could not find appropriate place to spawn her!");
		return null;
	}

	public static void SpawnMarinerRandom(float dist)
	{
		Vector2 vector = UnityEngine.Random.insideUnitCircle.normalized * dist;
		GameObject gameObject = ParanormalSpawner.SpawnMariner(CrewSim.coPlayer.tf.position + new Vector3(vector.x, vector.y, -2f));
	}

	public static GameObject SpawnMariner(Vector3 position)
	{
		if (ParanormalSpawner.m_marinerPrefab == null)
		{
			ParanormalSpawner.m_marinerPrefab = (Resources.Load(ParanormalSpawner.m_marinerName) as GameObject);
		}
		return UnityEngine.Object.Instantiate<GameObject>(ParanormalSpawner.m_marinerPrefab, position, Quaternion.Euler(0f, 180f, 0f), CrewSim.coPlayer.tf.parent);
	}

	public static float m_distance = 5f;

	public static int m_attemptsPerLayer = 5;

	public static int m_layers = 10;

	public static float m_layerWidth = 1f;

	public static string m_marinerName = "Supernatural/Mariner";

	public static GameObject m_marinerPrefab;
}
