﻿using System.Collections.Generic;
using Antlr4.Runtime;
using Rubberduck.Parsing.Symbols;
using Rubberduck.Parsing.VBA;
using Rubberduck.VBEditor;

namespace Rubberduck.Inspections
{
    public class ObsoleteCommentSyntaxInspectionResult : InspectionResultBase
    {
        private readonly IEnumerable<CodeInspectionQuickFix> _quickFixes;

        public ObsoleteCommentSyntaxInspectionResult(IInspection inspection, CommentNode comment) 
            : base(inspection, comment)
        {
            _quickFixes = new CodeInspectionQuickFix[]
            {
                new ReplaceObsoleteCommentMarkerQuickFix(Context, QualifiedSelection, comment),
                new RemoveCommentQuickFix(Context, QualifiedSelection, comment), 
                new IgnoreOnceQuickFix(Context, QualifiedSelection, Inspection.AnnotationName), 
            };
        }

        public override IEnumerable<CodeInspectionQuickFix> QuickFixes { get { return _quickFixes; } }

        public override string Description
        {
            get { return Inspection.Description; }
        }
    }

    public class RemoveCommentQuickFix : CodeInspectionQuickFix
    {
        private readonly CommentNode _comment;

        public RemoveCommentQuickFix(ParserRuleContext context, QualifiedSelection selection, CommentNode comment)
            : base(context, selection, InspectionsUI.RemoveCommentQuickFix)
        {
            _comment = comment;
        }

        public override void Fix()
        {
            var module = Selection.QualifiedName.Component.CodeModule;
            {
                if (module.IsWrappingNullReference)
                {
                    return;
                }

                var content = module.GetLines(Selection.Selection.StartLine, Selection.Selection.LineCount);

                int markerPosition;
                if (!content.HasComment(out markerPosition))
                {
                    return;
                }

                var code = string.Empty;
                if (markerPosition > 0)
                {
                    code = content.Substring(0, markerPosition).TrimEnd();
                }

                if (_comment.QualifiedSelection.Selection.LineCount > 1)
                {
                    module.DeleteLines(_comment.QualifiedSelection.Selection.StartLine, _comment.QualifiedSelection.Selection.LineCount);
                }

                module.ReplaceLine(_comment.QualifiedSelection.Selection.StartLine, code);
            }
        }
    }
}
