using System.Collections;
using UnityEngine;

public class Projectile : MonoBehaviour, IPooled //implementar interfaz
{
	[SerializeField] private float speed = 15f;
	[SerializeField] private float damage = 10f;
	[SerializeField] private float lifetime = 5f;

	private Vector3 direction;
	private Rigidbody rb;

	private GameobjectPool projectilePool;
	private Coroutine lifeTimeRoutine;

	private void Awake()
	{
		rb = GetComponent<Rigidbody>();
		//Destroy(gameObject, lifetime);
		lifeTimeRoutine = StartCoroutine(LifetimeRoutine());
	}

	public void SetDirection(Vector3 dir)
	{
		direction = dir.normalized;
		rb.linearVelocity = direction * speed;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Enemy"))
		{
			EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
			if (enemyHealth != null)
			{
				enemyHealth.TakeDamage(damage);
			}
			RemoveProjectile();
		}
	}

	/// <summary>
	/// Función para remover el proyectil, si fue obtenido de un pool lo regresa a este, de lo contrario
	/// se destruye.
	/// </summary>
	private void RemoveProjectile()
	{
		if (projectilePool != null)
		{
			projectilePool.ReturnObjectToPool(gameObject);
		}
		else
		{
			Destroy(gameObject);
		}

		StopCoroutine(lifeTimeRoutine);
	}

	private IEnumerator LifetimeRoutine()
	{
		yield return new WaitForSeconds(lifetime);
		RemoveProjectile();
	}

	/// <summary>
	/// Si el objeto fue obtendio de un pool, guardar en una variable el pool para poder regresarlo
	/// </summary>
	/// <param name="pool">el pool del que el objeto fue obtenido</param>
	void IPooled.OnObjectSpawned(GameobjectPool pool)
	{
		projectilePool = pool;
		lifeTimeRoutine = StartCoroutine(LifetimeRoutine());
	}
}
