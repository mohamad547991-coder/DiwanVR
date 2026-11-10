using System;
using System.Text.RegularExpressions;

namespace DiwanVR.Editor
{
    // Whitelist one known editor search failure and the explicit no-headset
    // result. Missing scripts, disposal failures and other XR errors fail.
    internal static class ValidationLogPolicy
    {
        public static bool IsExpectedEditorError(string message,string stack,bool desktopWithoutHeadset)
        {
            string text=(message??"")+"\n"+(stack??"");
            if(text.Contains("DiwanVR."))return false;
            var xrErrors=Regex.Matches(text,@"XR_ERROR_[A-Z_]+");
            if(xrErrors.Count>0)
            {
                if(!desktopWithoutHeadset)return false;
                foreach(Match error in xrErrors)
                    if(error.Value!="XR_ERROR_FORM_FACTOR_UNAVAILABLE")return false;
                return true;
            }
            return message!=null && message.StartsWith("ArgumentOutOfRangeException: Index was out of range",StringComparison.Ordinal)
                && (stack??"").Contains("UnityEditor.Search.SearchDatabase")
                && !(stack??"").Contains("Assets/");
        }
    }
}
