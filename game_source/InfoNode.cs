using System;
using System.Collections.Generic;

public class InfoNode
{
	public int depth;

	public InfoNode parent;

	public List<InfoNode> children = new List<InfoNode>();

	public string label;

	public MainWindowData mainWindowData;

	public List<InfoNode> siblings;

	public int lookupIndex;

	public string strLookup;

	public string escapeChar;

	public InfoPanel InfoPanel;

	public bool expanded;
}
