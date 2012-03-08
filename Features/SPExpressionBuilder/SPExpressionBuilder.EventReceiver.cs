using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Security;
using Microsoft.SharePoint.Administration;
using Phoenix.SharePointHelpers;
using System.Linq;

namespace Phoenix.SPExpressionBuilder.Features.Feature1
{
    /// <summary>
    /// This class handles events raised during feature activation, deactivation, installation, uninstallation, and upgrade.
    /// </summary>
    /// <remarks>
    /// The GUID attached to this class may be used during packaging and should not be modified.
    /// </remarks>


    [Guid("288851e8-aa48-4c6a-bb69-578c0a77fb76")]
    public class Feature1EventReceiver : SPFeatureReceiver
    {
        SPTraceLogger _log = new SPTraceLogger();
        uint _id = 912122314;
        string _owner = "SPExpressionBuilder";

        public override void FeatureActivated(SPFeatureReceiverProperties properties)
        {
            _log.Write(_id, SPTraceLogger.TraceSeverity.InformationEvent, "Feature1EventReceiver", "Info", "FeatureActivated");

            SPWebApplication rootApp = properties.Feature.Parent as SPWebApplication;
            SPExpressionBuilderConfigModifications express = new SPExpressionBuilderConfigModifications();
            express.SetConfigModifications(rootApp, _owner);
        }

        public override void FeatureDeactivating(SPFeatureReceiverProperties properties)
        {
            _log.Write(_id, SPTraceLogger.TraceSeverity.InformationEvent, "Feature1EventReceiver", "Info", "FeatureDeactivating");

            SPWebApplication rootApp = properties.Feature.Parent as SPWebApplication;
            SPExpressionBuilderConfigModifications express = new SPExpressionBuilderConfigModifications();
            express.RemoveConfigModifications(rootApp, _owner);
        }

        public override void FeatureInstalled(SPFeatureReceiverProperties properties)
        {
            //throw new Exception("The method or operation is not implemented.");
        }

        public override void FeatureUninstalling(SPFeatureReceiverProperties properties)
        {
            _log.Write(_id, SPTraceLogger.TraceSeverity.InformationEvent, "Feature1EventReceiver", "Info", "FeatureUninstalling");

            SPWebService webService = SPWebService.ContentService;

            foreach (SPWebApplication webApp in webService.WebApplications)
            {
                DeactivateFeatures(webApp);
                RemoveConfig(webApp);
            }
        }

        // We've already deactivated features, this is a last ditch to remove mods if feature deactivation hasn't covered them
        private void RemoveConfig(SPWebApplication webApp)
        {
            SPExpressionBuilderConfigModifications express = new SPExpressionBuilderConfigModifications();
            express.ForceRemoveConfigModifications(webApp, _owner);
        }

        private void DeactivateFeatures(SPWebApplication webApp)
        {
            // Deactivate if exists
            var feat = webApp.Features.SingleOrDefault(p => p.DefinitionId == new Guid("87d34d06-0f3a-4752-bb37-b3913935a360"));
            if (feat != null)
            {
                string message99 = string.Format("Removing feature on WebApp: {0}", webApp.Name);
                _log.Write(_id, SPTraceLogger.TraceSeverity.InformationEvent, "Feature1EventReceiver", "Info", message99);

                webApp.Features.Remove(feat.DefinitionId);
            }
            else
            {
                _log.Write(_id, SPTraceLogger.TraceSeverity.InformationEvent, "Feature1EventReceiver", "Info", "Feature not found");
            }
        }
    }
}
