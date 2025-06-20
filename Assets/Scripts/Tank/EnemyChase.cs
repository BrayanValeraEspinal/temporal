using UnityEngine;

public class EnemyChase : MonoBehaviour
{
	public float moveSpeed = 4f;
	public float chaseRange = 20f;
	public float attackRange = 2f; // distancia para atacar
	public int attackDamage = 10;
	public float attackCooldown = 1.5f; // segundos entre ataques

	private Rigidbody rb;
	private Transform targetTank;
	private float lastAttackTime;

	void Start()
	{
		rb = GetComponent<Rigidbody>();
		InvokeRepeating("UpdateTarget", 0f, 0.5f);
		lastAttackTime = -attackCooldown; // para poder atacar al inicio
	}

	void UpdateTarget()
	{
		GameObject[] tanks = GameObject.FindGameObjectsWithTag("Tank");
		float shortestDistance = Mathf.Infinity;
		Transform nearestTank = null;

		foreach (GameObject tank in tanks)
		{
			float distance = Vector3.Distance(transform.position, tank.transform.position);
			if (distance < shortestDistance && distance <= chaseRange)
			{
				shortestDistance = distance;
				nearestTank = tank.transform;
			}
		}

		targetTank = nearestTank;
	}

	void FixedUpdate()
	{
		if (targetTank != null)
		{
			float distanceToTank = Vector3.Distance(transform.position, targetTank.position);

			if (distanceToTank > attackRange)
			{
				// Moverse hacia el tanque
				Vector3 direction = (targetTank.position - transform.position).normalized;
				Vector3 movement = direction * moveSpeed * Time.fixedDeltaTime;
				rb.MovePosition(rb.position + movement);

				// Mirar al tanque
				Vector3 lookDirection = targetTank.position - transform.position;
				lookDirection.y = 0;
				if (lookDirection != Vector3.zero)
				{
					Quaternion rot = Quaternion.LookRotation(lookDirection);
					rb.MoveRotation(rot);
				}
			}
			else
			{
				// Atacar si pasó el cooldown
				if (Time.time >= lastAttackTime + attackCooldown)
				{
					Attack();
					lastAttackTime = Time.time;
				}
			}
		}
	}

	void Attack()
	{
		// Intentar hacer daño al tanque
		TankHealth tankHealth = targetTank.GetComponent<TankHealth>();
		if (tankHealth != null)
		{
			tankHealth.TakeDamage(attackDamage);
			// Aquí podrías agregar efectos de ataque, sonido, animación, etc.
		}
	}
}
