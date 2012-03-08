using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Administration;
using Phoenix.SharePointHelpers;

namespace Phoenix.WebConfig
{
    public class WebConfigModificationHandler
    {
        protected List<SPWebConfigModification> _modifications = new List<SPWebConfigModification>();
        SPTraceLogger _log = new SPTraceLogger();
        uint _id = 912122314;

        public SPWebConfigModification CreateModification(string Name, string XPath, string Value, SPWebConfigModification.SPWebConfigModificationType Type, string Owner)
        {
            SPWebConfigModification modification = new SPWebConfigModification(Name, XPath);
            modification.Owner = Owner;
            modification.Sequence = 0;
            modification.Type = Type;
            modification.Value = Value;
            return modification;
        }

        public virtual void SetConfigModifications(SPWebApplication WebApp, string Owner)
        {
            if (WebApp == null)
                throw new ArgumentException("WebApp is null, perhaps your feature is not scoped at WebAPp level?", "WebApp");
            if (string.IsNullOrEmpty(Owner))
                throw new ArgumentException("Owner is null", "Owner");

            // Start by removing all mods
            this.ForceRemoveConfigModifications(WebApp, Owner);

            {
                string message = string.Format("Adding Mods to WebApp: {0}", WebApp.Name);
                _log.Write(_id, SPTraceLogger.TraceSeverity.InformationEvent, "WebConfigModificationHandler", "Info", message);

            }

            foreach (var mod in _modifications)
            {
                string message = string.Format("Adding Mod: {0}", mod.Path);
                _log.Write(_id, SPTraceLogger.TraceSeverity.InformationEvent, "WebConfigModificationHandler", "Info", message);
                SPWebService.ContentService.WebApplications[WebApp.Id].WebConfigModifications.Add(mod);
                //WebApp.WebConfigModifications.Add(mod);
            }

            _log.Write(_id, SPTraceLogger.TraceSeverity.InformationEvent, "WebConfigModificationHandler", "Info", "Mods added, updating");
            SPWebService.ContentService.WebApplications[WebApp.Id].Update();
            _log.Write(_id, SPTraceLogger.TraceSeverity.InformationEvent, "WebConfigModificationHandler", "Info", "Mods added, applying update");
            SPWebService.ContentService.WebApplications[WebApp.Id].WebService.ApplyWebConfigModifications();
        }

        public virtual void RemoveConfigModifications(SPWebApplication WebApp, string Owner)
        {
            if (WebApp == null)
                throw new ArgumentException("WebApp is null, perhaps your feature is not scoped at WebApp level?", "WebApp");
            if (string.IsNullOrEmpty(Owner))
                throw new ArgumentException("Owner is null", "Owner");

            foreach (var mod in _modifications)
            {
                string message = string.Format("Removing Mod: {0}", mod.Path);
                _log.Write(_id, SPTraceLogger.TraceSeverity.InformationEvent, "WebConfigModificationHandler", "Info", message);

                WebApp.WebConfigModifications.Remove(mod);
            }

            //WebApp.Update();
            //WebApp.Farm.Services.GetValue<SPWebService>().ApplyWebConfigModifications();

            _log.Write(_id, SPTraceLogger.TraceSeverity.InformationEvent, "WebConfigModificationHandler", "Info", "Mods deleted, updating");
            SPWebService.ContentService.WebApplications[WebApp.Id].Update();
            _log.Write(_id, SPTraceLogger.TraceSeverity.InformationEvent, "WebConfigModificationHandler", "Info", "Mods deleted, applying update");
            SPWebService.ContentService.WebApplications[WebApp.Id].WebService.ApplyWebConfigModifications();

            ForceRemoveConfigModifications(WebApp, Owner);
        }

        // We make absolutely sure no mods with this owner exist
        public void ForceRemoveConfigModifications(SPWebApplication WebApp, string Owner)
        {
            List<SPWebConfigModification> removeMods = new List<SPWebConfigModification>();
            foreach (var mod in WebApp.WebConfigModifications)
            {
                if (mod.Owner == Owner)
                {
                    removeMods.Add(mod);
                }
            }

            foreach (var mod in removeMods)
            {
                string message = string.Format("Removing additional owner based mod: {0}", mod.Path);
                _log.Write(_id, SPTraceLogger.TraceSeverity.InformationEvent, "WebConfigModificationHandler", "Info", message);

                if (mod.Type != SPWebConfigModification.SPWebConfigModificationType.EnsureSection ||
                    (mod.Type == SPWebConfigModification.SPWebConfigModificationType.EnsureSection && !mod.Path.ToLower().Contains("system")))
                    WebApp.WebConfigModifications.Remove(mod);
                else
                {
                    string message2 = string.Format("Mod: {0} Type: {1} was not removed", mod.Path, mod.Type.ToString());
                    _log.Write(_id, SPTraceLogger.TraceSeverity.InformationEvent, "WebConfigModificationHandler", "Info", message2);
                }
            }

            _log.Write(_id, SPTraceLogger.TraceSeverity.InformationEvent, "WebConfigModificationHandler", "Info", "Mods deleted, updating");
            SPWebService.ContentService.WebApplications[WebApp.Id].Update();
            _log.Write(_id, SPTraceLogger.TraceSeverity.InformationEvent, "WebConfigModificationHandler", "Info", "Mods deleted, applying update");
            SPWebService.ContentService.WebApplications[WebApp.Id].WebService.ApplyWebConfigModifications();
        }

    }
}
