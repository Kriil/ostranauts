using System;
using System.Collections.Generic;
using Vectrosity;

namespace Ostranauts.ShipGUIs.Utilities
{
	public class BODraw
	{
		public BODraw(BodyOrbit bo)
		{
			this.bo = bo;
			this.aProjs = new List<VectorLine>();
		}

		public void Destroy()
		{
			this.bo = null;
			VectorLine.Destroy(ref this.lineTrackFull);
			VectorLine.Destroy(ref this.lineTrackPartial);
			VectorLine.Destroy(ref this.lineBody);
			VectorLine.Destroy(ref this.lineGrav);
			VectorLine.Destroy(ref this.lineGravInner);
			VectorLine.Destroy(this.aProjs);
		}

		public void SetState(bool active, bool showProjections)
		{
			if (this.bDrawTrack == active)
			{
				return;
			}
			this.bDrawTrack = active;
			this.lineBody.active = active;
			this.lineGrav.active = active;
			if (this.lineGravInner != null)
			{
				this.lineGravInner.active = active;
			}
			if (this.lineTrackFull != null)
			{
				this.lineTrackFull.active = active;
			}
			foreach (VectorLine vectorLine in this.aProjs)
			{
				vectorLine.active = showProjections;
			}
		}

		public bool Active
		{
			get
			{
				return this.bDrawTrack;
			}
		}

		public BodyOrbit bo;

		public VectorLine lineTrackFull;

		public VectorLine lineTrackPartial;

		public VectorLine lineBody;

		public VectorLine lineGrav;

		public VectorLine lineGravInner;

		public List<VectorLine> aProjs;

		private bool bDrawTrack = true;

		public double dTrackEpoch;
	}
}
