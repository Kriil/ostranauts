using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.Objectives
{
	public class ObjectivesAppPageSelector : MonoBehaviour
	{
		private void Awake()
		{
		}

		private void Start()
		{
			this._button.onClick.AddListener(new UnityAction(this.SetPage));
		}

		private void SetPage()
		{
			this._app.SetPage(this._appPage);
		}

		[SerializeField]
		private ObjectivesApp _app;

		[SerializeField]
		private ObjectivesAppPage _appPage;

		[SerializeField]
		private Button _button;
	}
}
