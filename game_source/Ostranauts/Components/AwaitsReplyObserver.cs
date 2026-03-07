using System;
using System.Collections.Generic;
using Ostranauts.Events;
using UnityEngine;
using UnityEngine.Events;

namespace Ostranauts.Components
{
	public class AwaitsReplyObserver : MonoBehaviour
	{
		private void Awake()
		{
			if (AwaitsReplyObserver._replayNotificationPrefab == null)
			{
				AwaitsReplyObserver._replayNotificationPrefab = (GameObject)Resources.Load("prefabAwaitsReply");
			}
			this._notificationGO = UnityEngine.Object.Instantiate<GameObject>(AwaitsReplyObserver._replayNotificationPrefab, base.transform).transform;
			this._notificationGO.name = "Notification AwaitsReply";
			this._notificationGO.gameObject.SetActive(false);
			this._co = base.GetComponent<CondOwner>();
		}

		private void OnEnable()
		{
			if (CondOwner.UpdateWaitingReplies == null)
			{
				CondOwner.UpdateWaitingReplies = new OnUpdateWaitingRepliesEvent();
			}
			else
			{
				CondOwner.UpdateWaitingReplies.RemoveListener(new UnityAction<List<ReplyThread>>(this.OnReplies));
			}
			CondOwner.UpdateWaitingReplies.AddListener(new UnityAction<List<ReplyThread>>(this.OnReplies));
		}

		private void OnDisable()
		{
			CondOwner.UpdateWaitingReplies.RemoveListener(new UnityAction<List<ReplyThread>>(this.OnReplies));
		}

		private void OnDestroy()
		{
			CondOwner.UpdateWaitingReplies.RemoveListener(new UnityAction<List<ReplyThread>>(this.OnReplies));
		}

		private void Update()
		{
			if (!this._active)
			{
				return;
			}
			this._notificationGO.position = new Vector3(base.transform.position.x + AwaitsReplyObserver._offset.x, base.transform.position.y + AwaitsReplyObserver._offset.y, AwaitsReplyObserver._offset.z);
			this._notificationGO.LookAt(this._notificationGO.position + CrewSim.objInstance.camMain.transform.rotation * Vector3.forward, CrewSim.objInstance.camMain.transform.rotation * Vector3.up);
		}

		private void OnReplies(List<ReplyThread> replies)
		{
			if (replies == null || replies.Count == 0)
			{
				if (this._active)
				{
					this.Deactivate();
				}
				return;
			}
			foreach (ReplyThread replyThread in replies)
			{
				if (replyThread != null && replyThread.strID == this._co.strID && !replyThread.bDone)
				{
					this.Activate();
					return;
				}
			}
			this.Deactivate();
		}

		private void Activate()
		{
			this._active = true;
			this._notificationGO.gameObject.SetActive(true);
		}

		private void Deactivate()
		{
			this._active = false;
			this._notificationGO.gameObject.SetActive(false);
		}

		private static GameObject _replayNotificationPrefab;

		private static readonly Vector3 _offset = new Vector3(0.75f, 0.75f, -5f);

		private Transform _notificationGO;

		private bool _active;

		private CondOwner _co;
	}
}
