using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletScript : MonoBehaviour
{
    private bool _Launched = false;
    private Vector3 _Origin = Vector3.zero;

    [SerializeField] Rigidbody _Rigidbody;
    [SerializeField] float _MinSpeed;
    [SerializeField] float _MaxDistance;

	private void Update()
	{
		if (_Launched)
			if (_Rigidbody.velocity.magnitude < _MinSpeed || Vector3.Distance(transform.position, _Origin) > _MaxDistance)
				Destroy(gameObject);
	}

	public void Launch(Vector3 target, float velocity, float mass)
	{
		_Origin = transform.position;
		_Rigidbody.mass = mass;
		_Rigidbody.velocity = (target - transform.position).normalized * velocity;
		_Launched = true;
	}
}
