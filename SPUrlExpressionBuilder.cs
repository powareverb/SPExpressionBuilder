using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Compilation;
using Microsoft.SharePoint;
using System.CodeDom;
using Phoenix.SharePointHelpers;

// Thanks to: http://buyevich.blogspot.com/2010/10/spurl-for-wss-30-or-sharepoint.html

namespace Phoenix.SPExpressionBuilder
{
    public class SPUrlExpressionBuilder : ExpressionBuilder
    {
        static SPTraceLogger _log = new SPTraceLogger();
        static uint _id = 912122314;
        static string _owner = "SPExpressionBuilder";

        public override CodeExpression GetCodeExpression
(System.Web.UI.BoundPropertyEntry entry, object parsedData, ExpressionBuilderContext context)
        {
            _log.Write(_id, SPTraceLogger.TraceSeverity.InformationEvent, "GetCodeExpression", "Info", "Called");

            CodeTypeReferenceExpression thisType = new CodeTypeReferenceExpression(base.GetType());

            CodePrimitiveExpression expression = new CodePrimitiveExpression(entry.Expression.Trim().ToString());

            string evaluationMethod = "GetKeyValue";

            return new CodeMethodInvokeExpression(thisType, evaluationMethod, new CodeExpression[] { expression });
        }

        public override bool SupportsEvaluate
        {
            get
            {
                return true;
            }
        }

        public static object GetKeyValue(string expression)
        {
            _log.Write(_id, SPTraceLogger.TraceSeverity.InformationEvent, "GetKeyValue", "Info", "Called");

            SPWeb web = SPContext.Current.Web;

            string key = "~SiteCollection";
            if (expression.IndexOf(key, StringComparison.InvariantCultureIgnoreCase) == 0)
                return web.Site.Url + expression.Substring(key.Length);

            key = "~Site";
            if (expression.IndexOf(key, StringComparison.InvariantCultureIgnoreCase) == 0)
                return web.Url + expression.Substring(key.Length);

            return expression;
        }
    }
}
