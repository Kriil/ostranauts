using System;
[Serializable]
// DTO for a single FFU_BR `aMathOps` entry inside an extended condtrigger.
// Each record describes which condition to compare, which math operator to use,
// and what scalar or fixed threshold should be applied.
public class JsonMathOp
{
	public string strID { get; set; }
	public string strCond { get; set; }
	public int nMathOp { get; set; }
	public float fMathVal { get; set; }
	public override string ToString()
	{
		return this.strCond;
	}
}
