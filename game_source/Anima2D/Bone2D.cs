using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Anima2D
{
	public class Bone2D : MonoBehaviour
	{
		public Ik2D attachedIK
		{
			get
			{
				return this.m_AttachedIK;
			}
			set
			{
				this.m_AttachedIK = value;
			}
		}

		public Color color
		{
			get
			{
				return this.m_Color;
			}
			set
			{
				this.m_Color = value;
			}
		}

		public Bone2D child
		{
			get
			{
				if (this.m_Child)
				{
					this.child = this.m_Child;
				}
				if (this.m_CachedChild && this.m_ChildTransform != this.m_CachedChild.transform)
				{
					this.m_CachedChild = null;
				}
				if (this.m_ChildTransform && this.m_ChildTransform.parent != base.transform)
				{
					this.m_CachedChild = null;
				}
				if (!this.m_CachedChild && this.m_ChildTransform && this.m_ChildTransform.parent == base.transform)
				{
					this.m_CachedChild = this.m_ChildTransform.GetComponent<Bone2D>();
				}
				return this.m_CachedChild;
			}
			set
			{
				this.m_Child = null;
				this.m_CachedChild = value;
				this.m_ChildTransform = this.m_CachedChild.transform;
			}
		}

		public Vector3 localEndPosition
		{
			get
			{
				return Vector3.right * this.localLength;
			}
		}

		public Vector3 endPosition
		{
			get
			{
				return base.transform.TransformPoint(this.localEndPosition);
			}
		}

		public float localLength
		{
			get
			{
				if (this.child)
				{
					Vector3 vector = base.transform.InverseTransformPoint(this.child.transform.position);
					this.m_Length = Mathf.Clamp(vector.x, 0f, vector.x);
				}
				return this.m_Length;
			}
			set
			{
				if (!this.child)
				{
					this.m_Length = value;
				}
			}
		}

		public float length
		{
			get
			{
				return base.transform.TransformVector(this.localEndPosition).magnitude;
			}
		}

		public Bone2D parentBone
		{
			get
			{
				Transform parent = base.transform.parent;
				if (!this.mParentBone)
				{
					if (parent)
					{
						this.mParentBone = parent.GetComponent<Bone2D>();
					}
				}
				else if (parent != this.mParentBone.transform)
				{
					if (parent)
					{
						this.mParentBone = parent.GetComponent<Bone2D>();
					}
					else
					{
						this.mParentBone = null;
					}
				}
				return this.mParentBone;
			}
		}

		public Bone2D linkedParentBone
		{
			get
			{
				if (this.parentBone && this.parentBone.child == this)
				{
					return this.parentBone;
				}
				return null;
			}
		}

		public Bone2D root
		{
			get
			{
				Bone2D bone2D = this;
				while (bone2D.parentBone)
				{
					bone2D = bone2D.parentBone;
				}
				return bone2D;
			}
		}

		public Bone2D chainRoot
		{
			get
			{
				Bone2D bone2D = this;
				while (bone2D.parentBone && bone2D.parentBone.child == bone2D)
				{
					bone2D = bone2D.parentBone;
				}
				return bone2D;
			}
		}

		public int chainLength
		{
			get
			{
				Bone2D bone2D = this;
				int num = 1;
				while (bone2D.parentBone && bone2D.parentBone.child == bone2D)
				{
					num++;
					bone2D = bone2D.parentBone;
				}
				return num;
			}
		}

		public static Bone2D GetChainBoneByIndex(Bone2D chainTip, int index)
		{
			if (!chainTip)
			{
				return null;
			}
			Bone2D bone2D = chainTip;
			int chainLength = bone2D.chainLength;
			int num = 0;
			while (num < chainLength && bone2D)
			{
				if (num == index)
				{
					return bone2D;
				}
				if (!bone2D.linkedParentBone)
				{
					return null;
				}
				bone2D = bone2D.parentBone;
				num++;
			}
			return null;
		}

		[SerializeField]
		[FormerlySerializedAs("color")]
		private Color m_Color = Color.white;

		[SerializeField]
		[FormerlySerializedAs("mLength")]
		private float m_Length = 1f;

		[SerializeField]
		[HideInInspector]
		[FormerlySerializedAs("mChild")]
		private Bone2D m_Child;

		[SerializeField]
		[HideInInspector]
		private Transform m_ChildTransform;

		[SerializeField]
		private Ik2D m_AttachedIK;

		private Bone2D m_CachedChild;

		private Bone2D mParentBone;
	}
}
