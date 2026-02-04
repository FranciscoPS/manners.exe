using UnityEngine;

public class AutoAttackSystem : MonoBehaviour
{
	[SerializeField] private GameObject projectilePrefab;
	[SerializeField] private Transform firePoint;
	[SerializeField] private float attackRange = 10f;
	[SerializeField] private float attackCooldown = 0.5f;
	[SerializeField] private LayerMask enemyLayer;

	private float cooldownTimer = 0f;
	private Transform currentTarget;
	public GameobjectPool projectilePool;

	private void Update()
	{
		cooldownTimer -= Time.deltaTime;

		FindClosestEnemy();

		if (currentTarget != null && cooldownTimer <= 0f)
		{
			Shoot();
			cooldownTimer = attackCooldown;
		}
	}

	private void FindClosestEnemy()
	{
		Collider[] enemies = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);

		if (enemies.Length == 0)
		{
			currentTarget = null;
			return;
		}

		float closestDistance = Mathf.Infinity;
		Transform closestEnemy = null;

		foreach (Collider enemy in enemies)
		{
			float distance = Vector3.Distance(transform.position, enemy.transform.position);
			if (distance < closestDistance)
			{
				closestDistance = distance;
				closestEnemy = enemy.transform;
			}
		}

		currentTarget = closestEnemy;
	}

	private void Shoot()
	{
		GameObject projectile = null;

		//Si hay un pool asignado obtener el objeto de ahí, de lo contrario instancearlo.
		if (projectilePool != null)
		{
			projectile = projectilePool.GetObjectFromPool();
			projectile.transform.position = firePoint.position;
		}
		else
		{
			projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
		}

		Projectile projScript = projectile.GetComponent<Projectile>();

		if (projScript != null)
		{
			Vector3 direction = (currentTarget.position - firePoint.position).normalized;
			projScript.SetDirection(direction);
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, attackRange);
	}
}
