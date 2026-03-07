using System;
using System.Text;

namespace Ostranauts.Social.Models
{
	public class SocialStakes
	{
		protected SocialStakes()
		{
		}

		public SocialStakes(string context)
		{
			this.strContext = context;
			JsonContext context2 = DataHandler.GetContext(this.strContext);
			if (context2 != null)
			{
				this.defaultTitle = context2.strTitle;
				this.defaultBody = context2.strMainText;
			}
		}

		public virtual void UpdateUs(CondOwner coThem)
		{
			if (coThem == null || string.IsNullOrEmpty(this.defaultBody))
			{
				return;
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(this.defaultBody);
			this._strMTTInfo = stringBuilder.ToString();
		}

		public virtual void UpdateUs(Interaction ia)
		{
			if (ia == null)
			{
				return;
			}
			this.UpdateUs(ia.objThem);
		}

		public virtual void UpdateThem(Interaction ia)
		{
		}

		public bool Same(string strContext)
		{
			return this.strContext == strContext;
		}

		public string MTTInfo
		{
			get
			{
				return this._strMTTInfo;
			}
		}

		protected string defaultTitle = string.Empty;

		protected string defaultBody = string.Empty;

		protected string _strMTTInfo;

		protected string strContext = "Default";
	}
}
