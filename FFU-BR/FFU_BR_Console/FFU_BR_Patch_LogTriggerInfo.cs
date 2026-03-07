using System;
public partial class patch_ConsoleResolver : ConsoleResolver {
    private static string GetRulesInfoDev(CondTrigger refTrigger) {
        refTrigger.strFailReason = null;
        Type refType = refTrigger.GetType();
        return (refType.GetProperty("RulesInfoDev")?.GetValue(refTrigger, null) as string) ?? refTrigger.RulesInfo;
    }
    private static string GetRulesInfoTxt(CondTrigger refTrigger) {
        refTrigger.strFailReason = null;
        Type refType = refTrigger.GetType();
        return refTrigger.RulesInfo;
    }
    private static bool KeywordCondTrigInfo(ref string strInput) {
        string[] strTriggerVals = strInput.Split(' ');
        if (strTriggerVals.Length < 3) {
            strInput += "\nMissing command arguments.";
            return false;
        }
        string strTriggerKey = strTriggerVals[1];
        if (!DataHandler.dictCTs.ContainsKey(strTriggerKey)) {
            strInput += "\nCondition trigger not found.";
            return false;
        }
        int.TryParse(strTriggerVals[2], out int printMode);
        CondTrigger refTrigger = DataHandler.dictCTs[strTriggerKey].Clone();
        switch (printMode) {
            case 0: {
                strInput += $"\nCondition Trigger '{strTriggerKey}' Rules: {GetRulesInfoDev(refTrigger)}";
                break;
            }
            case 1: {
                strInput += $"\nCondition Trigger '{strTriggerKey}' Rules: {GetRulesInfoTxt(refTrigger)}";
                break;
            }
            default: {
                strInput += $"\nInvalid rule info rendering option."; 
                return false;
            }
        }
        return true;
    }
}