using System;
using System.Text.RegularExpressions;

namespace LMS.Helpers
{
    public static class ProgramNameHelper
    {
        public static string AbbreviateProgramName(string programName)
        {
            if (string.IsNullOrWhiteSpace(programName))
                return programName;

            programName = programName.Trim();

            programName = Regex.Replace(programName, @"^Bachelor\s+of\s+Science\s+in\s+", "BS in ", RegexOptions.IgnoreCase);
            programName = Regex.Replace(programName, @"^Bachelor\s+of\s+Arts\s+in\s+", "BA in ", RegexOptions.IgnoreCase);
            programName = Regex.Replace(programName, @"^Bachelor\s+of\s+Science\s+", "BS ", RegexOptions.IgnoreCase);
            programName = Regex.Replace(programName, @"^Bachelor\s+of\s+Arts\s+", "BA ", RegexOptions.IgnoreCase);

            return programName;
        }

        public static string GetDisplayName(string programName, string programCode = null)
        {
            var abbrev = AbbreviateProgramName(programName);
            if (!string.IsNullOrWhiteSpace(programCode))
                return $"{programCode} - {abbrev}";
            return abbrev;
        }
    }
}
