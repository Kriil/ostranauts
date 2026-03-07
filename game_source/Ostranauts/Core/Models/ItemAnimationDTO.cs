using System;
using UnityEngine;

namespace Ostranauts.Core.Models
{
	public class ItemAnimationDTO
	{
		public ItemAnimationDTO(JsonItemAnimation jAnim)
		{
			this.FrameCount = jAnim.nFrameCount;
			string[] array = jAnim.strFrameRate.Split(new char[]
			{
				'-'
			});
			float fX = 0f;
			if (float.TryParse(array[0], out fX))
			{
				this.FrameRate = MathUtils.RoundToInt(fX);
			}
			if (array.Length > 1 && float.TryParse(array[1], out fX))
			{
				int num = MathUtils.RoundToInt(fX);
				this.FrameRate = UnityEngine.Random.Range(this.FrameRate, num + 1);
			}
			this.Columns = jAnim.nSheetColumns;
			this.Rows = jAnim.nSheetRows;
			this.Loop = jAnim.bLoop;
		}

		public int FrameCount = 16;

		public int FrameRate = 32;

		public int Columns = 1;

		public int Rows = 1;

		public bool Loop;
	}
}
