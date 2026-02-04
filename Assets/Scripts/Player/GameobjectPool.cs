using UnityEngine;
using UnityEngine.Pool; //Importar la clase ObjectPool

public class GameobjectPool : MonoBehaviour
{
	[Tooltip("prefab base que se instanciará en el pool")]
	public GameObject prefab;

	[Tooltip("prefab base que se instanciará en el pool")]
	public int defaultSize = 25;

	[Tooltip("prefab base que se instanciará en el pool")]
	public int maxSize = 250;

	//Object Pool es similar a una lista pero no es visible en el inspector, almacena todos los clones del prefab
	private ObjectPool<GameObject> pool;

	//contandor que incrementa dependiendo de las copias del prefab, se usa para nombrar las copias: copia_0, copia_1, copia_2, etc.
	private int creationConter;

	protected virtual void Start()
	{
		/*
		Para crear un pool hay que especificar que tipo de clase va a almacenar (usualmente GameObject), también hay que crear las siguientes funciones:
		OnCreate: lógica que se ejecuta cunado se crea el pool, usualmente es para instanciar por primera vez las copias del prefab.
		OnTakeGameobjectFromPool: lógica que se ejecuta cuando se toma un objeto del pool, usualmente es para "prender" el objeto (por ejemplo llamar la función SetActive(true)).
		OnReturnGameobjectFromPool: lógica que se ejecuta cuando se regresea un objeto pool, usualmente es para "apagar" el objeto (por ejemplo llamar la función SetActive(true))
		OnDestroyGameObject: lógica que se ejecuta cuando se Destruye el objeto del pool.
		*/
		pool = new ObjectPool<GameObject>(OnCreate, OnTakeGameobjectFromPool, OnReturnGameobjectFromPool, OnDestroyGameObject, true, defaultSize, maxSize);
	}

	#region PoolActions

	/// <summary>
	/// Instancia el prefab para poder reutilizarse en el futuro
	/// </summary>
	protected virtual GameObject OnCreate()
	{
		GameObject gameObject = Instantiate(prefab, Vector3.zero, Quaternion.Euler(Vector3.zero));

		gameObject.SetActive(false);
		gameObject.name = prefab.name + "_" + creationConter;
		gameObject.transform.SetParent(null);
		creationConter++;
		return gameObject;
	}

	/// <summary>
	/// Cuando se toma un objeto del pool activamos el gameobject para que sea visible.
	/// </summary>
	protected virtual void OnTakeGameobjectFromPool(GameObject gameObject)
	{
		gameObject.SetActive(true);
	}

	/// <summary>
	/// Cuando se toma un objeto del pool apagamos el gameobject para que no sea visible.
	/// </summary>
	protected virtual void OnReturnGameobjectFromPool(GameObject gameObject)
	{
		gameObject.SetActive(false);
	}

	/// <summary>
	/// Si se llega a destruir un objeto del pool, removerlo por completo
	/// </summary>
	protected virtual void OnDestroyGameObject(GameObject gameObject)
	{
		Destroy(gameObject);
	}
	#endregion


	/// <summary>
	/// Función para tomar una copia del un objeto del pool
	/// </summary>
	/// <returns>una copia del objeto prefab ya instanciada</returns>
	public virtual GameObject GetObjectFromPool()
	{
		GameObject gameObject = pool.Get(); // la función Get es la que toma la copia del pool.

		IOnObjectSpawned(gameObject);
		return gameObject;
	}

	/// <summary>
	/// Función para regresar una copia activa del objeto al pool y apagarlo
	/// </summary>
	/// <param name="gameObject">el objeto que se regresará al pool</param>
	public virtual void ReturnObjectToPool(GameObject gameObject)
	{
		pool.Release(gameObject); // la función Release es la que regresa el objeto especificado al pool.
	}

	/// <summary>
	/// Función auxiliar usando la interface IPooled, básicamente indica a los objetos que se llamen por pool la referencia
	/// de este para poder volverlos a regresar.
	/// </summary>
	/// <param name="gameObject"></param>
	private void IOnObjectSpawned(GameObject gameObject)
	{
		IPooled iPooled = gameObject.GetComponent<IPooled>();
		if (iPooled != null)
		{
			iPooled.OnObjectSpawned(this);
		}
	}
}
