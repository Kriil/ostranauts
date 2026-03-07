using System;
using UnityEngine;

public class Mariner : MonoBehaviour
{
	private void Start()
	{
		this.m_camera = GameObject.Find("Main Camera").GetComponent<Camera>();
		this.m_animator = base.GetComponent<Animator>();
	}

	private void Update()
	{
		if (!this.m_animator.GetCurrentAnimatorStateInfo(0).IsName("Spaced1"))
		{
			this.m_animator.Play("Spaced1");
		}
		Vector2 screenPos = this.GetScreenPos();
		this.m_lifeTimer -= Time.deltaTime;
		if (!this.m_destroying && this.m_lifeTimer <= 0f)
		{
			this.m_destroying = true;
			UnityEngine.Object.Destroy(base.gameObject, 1f);
			this.m_bodyFade.Play("FadeOut");
		}
		switch (this.m_state)
		{
		case Mariner.State.following:
			this.GoTowards(CrewSim.coPlayer.tf.position);
			break;
		case Mariner.State.leaving:
			this.GoAway(CrewSim.coPlayer.tf.position);
			break;
		}
		this.FacePlayer();
	}

	public void StartShadowing()
	{
		if (this.m_state != Mariner.State.shadowing)
		{
			this.m_state = Mariner.State.shadowing;
		}
		else
		{
			Debug.Log("Told Mariner to shadow, but it it already shadowing!");
		}
	}

	public void StartFollowing()
	{
		if (this.m_state != Mariner.State.following)
		{
			this.m_state = Mariner.State.following;
		}
		else
		{
			Debug.Log("Told Mariner to follow, but it it already following!");
		}
	}

	public void StartLeaving()
	{
		if (this.m_state != Mariner.State.leaving)
		{
			this.m_state = Mariner.State.leaving;
		}
		else
		{
			Debug.Log("Told Mariner to leave, but it it already leaving!");
		}
	}

	private Vector2 GetScreenPos()
	{
		Vector3 vector = this.m_camera.WorldToScreenPoint(base.transform.position);
		Vector3 vector2 = new Vector2((float)Screen.width / 2f, (float)Screen.height / 2f);
		Vector2 result = new Vector2((vector.x - vector2.x) / vector2.x, (vector.y - vector2.y) / vector2.y);
		return result;
	}

	private void GoTowards(Vector3 pos)
	{
		Vector2 a = new Vector2(pos.x - base.transform.position.x, pos.y - base.transform.position.y);
		float num = this.m_moveSpeed * Time.deltaTime;
		if (a.magnitude <= num)
		{
			base.transform.position = new Vector3(pos.x, pos.y, this.m_floatHeight);
			return;
		}
		a.Normalize();
		a *= num;
		base.transform.position += new Vector3(a.x, a.y, 0f);
	}

	private void GoAway(Vector3 pos)
	{
		Vector2 vector = new Vector2(pos.x - base.transform.position.x, pos.y - base.transform.position.y);
		if (vector == Vector2.zero)
		{
			vector = new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f));
		}
		vector = -vector;
		vector.Normalize();
		float d = this.m_moveSpeed * Time.deltaTime;
		vector *= d;
		base.transform.position += new Vector3(vector.x, vector.y, 0f);
	}

	private void FacePlayer()
	{
		Vector2 to = new Vector2(base.transform.position.x - CrewSim.coPlayer.tf.position.x, base.transform.position.y - CrewSim.coPlayer.tf.position.y);
		to.Normalize();
		float num = Vector2.Angle(Vector2.up, to);
		if (to.x < 0f)
		{
			base.transform.rotation = Quaternion.Euler(base.transform.rotation.x, base.transform.rotation.y, num);
		}
		else
		{
			base.transform.rotation = Quaternion.Euler(base.transform.rotation.x, base.transform.rotation.y, -num);
		}
	}

	public Mariner.State m_state;

	public bool m_destroying;

	public float m_lifeTimer = 5f;

	public float m_moveSpeed = 2f;

	public float m_floatHeight = -2f;

	public Animator m_bodyFade;

	private Camera m_camera;

	private Animator m_animator;

	public enum State
	{
		watching,
		shadowing,
		following,
		leaving
	}
}
