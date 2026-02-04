
using UnityEngine;

/// <summary>
/// Interfaz para asignar a los objetos que se instrancen por pool la referencia al pool por la que fueron llamados para que
/// puedan regresar a este.
/// </summary>
public interface IPooled
{
	void OnObjectSpawned(GameobjectPool pool);
}

