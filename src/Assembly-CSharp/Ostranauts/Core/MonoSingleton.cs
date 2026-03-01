using System;
using UnityEngine;

namespace Ostranauts.Core
{
	public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
	{
		public static T Instance
		{
			get
			{
				if (MonoSingleton<T>._instance == null)
				{
					MonoSingleton<T>._instance = (T)((object)UnityEngine.Object.FindObjectOfType(typeof(T)));
					if (MonoSingleton<T>._instance == null)
					{
						string text = typeof(T).ToString();
						Debug.Log("No instance found, creating new gameobject for: " + text);
						GameObject gameObject = new GameObject
						{
							name = text
						};
						MonoSingleton<T>._instance = gameObject.AddComponent<T>();
					}
				}
				return MonoSingleton<T>._instance;
			}
		}

		protected void Awake()
		{
			if (MonoSingleton<T>._instance != null)
			{
				Debug.Log(typeof(T) + "- Prevented class from creating another Singleton instance, Gameobject was destroyed");
				UnityEngine.Object.Destroy(this);
			}
			if (MonoSingleton<T>._instance == null)
			{
				MonoSingleton<T>._instance = base.GetComponent<T>();
			}
		}

		private static T _instance;
	}
}
