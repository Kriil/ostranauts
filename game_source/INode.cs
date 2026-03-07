using System;
using UnityEngine;

public interface INode
{
	int nLayoutColumn { get; set; }

	Transform transform { get; }

	void Redraw();

	void DeleteNode();

	void SaveData();
}
