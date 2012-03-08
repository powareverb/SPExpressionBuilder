using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Phoenix.WebConfig;
using Microsoft.SharePoint.Administration;

namespace Phoenix.SPExpressionBuilder
{
//    <configuration>
//<system.web>
//<compilation>
//<expressionBuilders>
//<add expressionPrefix="WSSUrl"
//type="{your Namespace}.WSSUrlExpressionBuilder,{Assembly Name}"/>
//</expressionBuilders>
//</compilation>
//</system.web>
//</configuration>
    //_modifications.Add(CreateModification("Microsoft.Dynamics",
    //        "configuration", "Microsoft.Dynamics",
    //        SPWebConfigModification.SPWebConfigModificationType.EnsureSection, _owner));

    public class SPExpressionBuilderConfigModifications : WebConfigModificationHandler
    {
        private string _owner = "SPExpressionBuilder";

        public SPExpressionBuilderConfigModifications()
        {
            _modifications.Add(CreateModification("compilation",
            "configuration/system.web", "compilation",
            SPWebConfigModification.SPWebConfigModificationType.EnsureSection, _owner));

            _modifications.Add(CreateModification("expressionBuilders",
            "configuration/system.web/compilation", "expressionBuilders",
            SPWebConfigModification.SPWebConfigModificationType.EnsureSection, _owner));

            _modifications.Add(CreateModification("add[@expressionPrefix='SPUrl']",
                "configuration/system.web/compilation/expressionBuilders",
                @"<add expressionPrefix='SPUrl' type='Phoenix.SPExpressionBuilder.SPUrlExpressionBuilder,Phoenix.SPExpressionBuilder, Version=1.0.0.0, Culture=neutral, PublicKeyToken=3bf50d67de4343b0' />",
                SPWebConfigModification.SPWebConfigModificationType.EnsureChildNode, _owner));
        }
    }
}
