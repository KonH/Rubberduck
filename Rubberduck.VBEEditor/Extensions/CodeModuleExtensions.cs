using Rubberduck.VBEditor.DisposableWrappers;
using Rubberduck.VBEditor.DisposableWrappers.VBA;

namespace Rubberduck.VBEditor.Extensions
{
    /// <summary>
    /// VBE CodeModule extension methods.
    /// </summary>
    public static class CodeModuleExtensions
    {
        /// <summary>
        /// Gets an array of strings where each element is a line of code in the Module,
        /// with line numbers stripped and any other pre-processing that needs to be done.
        /// </summary>
        public static string[] GetSanitizedCode(this CodeModule module)
        {
            var lines = module.CountOfLines;
            if (lines == 0)
            {
                return new string[] { };
            }

            var code = module.GetLines(1, lines).Replace("\r", string.Empty).Split('\n');

            StripLineNumbers(code);
            return code;
        }

        private static void StripLineNumbers(string[] lines)
        {
            var continuing = false;
            for(var line = 0; line < lines.Length; line++)
            {
                var code = lines[line];
                int? lineNumber;
                if (!continuing && HasNumberedLine(code, out lineNumber))
                {
                    var lineNumberLength = lineNumber.ToString().Length;
                    if (lines[line].Length > lineNumberLength)
                    {
                        // replace line number with as many spaces as characters taken, to avoid shifting the tokens
                        lines[line] = new string(' ', lineNumberLength) + code.Substring(lineNumber.ToString().Length + 1);
                    }
                }

                continuing = code.EndsWith("_");
            }
        }

        private static bool HasNumberedLine(string codeLine, out int? lineNumber)
        {
            lineNumber = null;

            if (string.IsNullOrWhiteSpace(codeLine.Trim()))
            {
                return false;
            }

            int line;
            var firstToken = codeLine.TrimStart().Split(' ')[0];
            if (int.TryParse(firstToken, out line))
            {
                lineNumber = line;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns all of the code in a Module as a string.
        /// </summary>
        public static string Lines(this CodeModule module)
        {
            if (module.CountOfLines == 0)
            {
                return string.Empty;
            }

            return module.GetLines(1, module.CountOfLines);
        }

        /// <summary>
        /// Deletes all lines from the CodeModule
        /// </summary>
        public static void Clear(this CodeModule module)
        {
            module.DeleteLines(1, module.CountOfLines);
        }

        /// <summary>
        /// Returns the lines containing the selection.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="selection"></param>
        /// <returns></returns>
        public static string GetLines(this CodeModule module, Selection selection)
        {
            return module.GetLines(selection.StartLine, selection.LineCount);
        }

        /// <summary>
        /// Deletes the lines containing the selection.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="selection"></param>
        public static void DeleteLines(this CodeModule module, Selection selection)
        {
            module.DeleteLines(selection.StartLine, selection.LineCount);
        }
        public static QualifiedSelection? GetSelection(this CodeModule module)
        {
            var pane = module.CodePane;
            {
                if (pane.IsWrappingNullReference) { return null; }
                return new QualifiedSelection(new QualifiedModuleName(module.Parent), pane.GetSelection());
            }
        }
    }
}
