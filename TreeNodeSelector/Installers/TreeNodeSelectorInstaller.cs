using CMS.Base;
using CMS.Core;
using CMS.DataEngine;
using CMS.FormEngine;
using CMS.Modules;
using CMS.SiteProvider;
using System;
using System.Linq;
using XperienceCommunity.TreeNodeSelectorFormControl.Constants;
using XperienceCommunity.TreeNodeSelectorFormControl.Core;

namespace XperienceCommunity.TreeNodeSelectorFormControl.Installers
{
    internal class TreeNodeSelectorInstaller : ITreeNodeSelectorInstaller
    {
        private readonly IEventLogService _eventLogService;
        private readonly IResourceInfoProvider _resourceInfoProvider;
        private readonly IResourceSiteInfoProvider _resourceSiteInfoProvider;
        private readonly ISiteInfoProvider _siteInfoProvider;
        private readonly IQueryInfoProvider _queryInfoProvider;


        /// <remarks>
        /// This class also uses the following hard-coded, concrete dependencies:
        ///     FormUserControlInfoProvider
        ///     QueryInfoProvider
        /// </remarks>
        public TreeNodeSelectorInstaller(IEventLogService eventLogService,
                                           IResourceInfoProvider resourceInfoProvider,
                                           IResourceSiteInfoProvider resourceSiteInfoProvider,
                                           ISiteInfoProvider siteInfoProvider,
                                           IQueryInfoProvider queryInfoProvider)
        {
            _eventLogService = eventLogService;
            _resourceInfoProvider = resourceInfoProvider;
            _resourceSiteInfoProvider = resourceSiteInfoProvider;
            _siteInfoProvider = siteInfoProvider;
            _queryInfoProvider = queryInfoProvider;
        }

        public void Install()
        {
            try
            {
                var resourceInfo = InstallResourceInfo();
                AssignModuleToSites(resourceInfo);
            }
            catch(Exception ex)
            {
                _eventLogService.LogException(nameof(TreeNodeSelectorInstaller),
                                              "ERROR",
                                              ex);
            }
        }

        private void AssignModuleToSites(ResourceInfo resourceInfo)
        {
            using (new CMSActionContext
            {
                LogSynchronization = false,
                ContinuousIntegrationAllowObjectSerialization = false
            })
            {

                var unassignedSites = _siteInfoProvider
                                    .Get()
                                    .WhereNotIn("SiteID",
                                    _resourceSiteInfoProvider
                                        .Get()
                                        .Column("SiteID")
                                        .WhereEquals("ResourceID", resourceInfo.ResourceID)
                                    )
                                    .Select(siteInfo => siteInfo.SiteID)
                                    .ToList();

                unassignedSites.ForEach(siteId => _resourceSiteInfoProvider
                                                    .Add(resourceInfo.ResourceID, siteId));
                var unassignedSiteCount = unassignedSites.Count;
                if (unassignedSiteCount > 0)
                {
                    LogInformation("ASSIGNED", $"Assigned the module '{ResourceConstants.ResourceDisplayName}' to {unassignedSiteCount} sites.");
                }
            }
        }

        private ResourceInfo InstallResourceInfo()
        {
            using (new CMSActionContext
            {
                LogSynchronization = false,
                ContinuousIntegrationAllowObjectSerialization = false
            })
            {
                var resourceInfo = _resourceInfoProvider.Get(ResourceConstants.ResourceName);
                if (InstalledModuleIsCurrent(resourceInfo))
                {
                    LogInformation("CURRENT", $"The '{ResourceConstants.ResourceDisplayName}' module is already installed and current.");
                    return resourceInfo;
                }
                LogInformation("START", $"{(resourceInfo == null ? "Installing" : "Updating")} the module '{ResourceConstants.ResourceDisplayName}'.");
                if (resourceInfo == null)
                {
                    resourceInfo = new ResourceInfo();
                }

                resourceInfo.ResourceDisplayName = ResourceConstants.ResourceDisplayName;
                resourceInfo.ResourceName = ResourceConstants.ResourceName;
                resourceInfo.ResourceDescription = ResourceConstants.ResourceDescription;
                resourceInfo.ResourceAuthor = ResourceConstants.ResourceAuthor;
                resourceInfo.ResourceIsInDevelopment = ResourceConstants.ResourceIsInDevelopment;
                // Setting ResourceInstallationState to 'installed' will cause Kentico to uninstall related objects if it
                // finds a module meta file in ~\App_Data\CMSModules\CMSInstallation\Packages\Installed
                resourceInfo.ResourceInstallationState = ResourceConstants.ResourceInstallationState;
                _resourceInfoProvider.Set(resourceInfo);
                InstallUserControlObject(resourceInfo); 
                InstallQueryObject();
                StoreInstalledVersion(resourceInfo);
                LogInformation("COMPLETE", $"{(resourceInfo == null ? "Install" : "Update")} of the module '{ResourceConstants.ResourceDisplayName}' version {resourceInfo.ResourceVersion} is complete.");
                return resourceInfo;
            }
        }

        /// <summary>
        /// Store the version number of the installed module. This should
        /// be done after the ResourceInfo and UiElementInfo are successfully
        /// updated and saved.
        /// </summary>
        /// <param name="resourceInfo"></param>
        private void StoreInstalledVersion(ResourceInfo resourceInfo)
        {
            using (new CMSActionContext
            {
                LogSynchronization = false,
                ContinuousIntegrationAllowObjectSerialization = false
            })
            {
                string newVersion = GetModuleVersionFromAssembly();
                resourceInfo.ResourceInstalledVersion = newVersion;
                resourceInfo.ResourceVersion = newVersion;
                _resourceInfoProvider.Set(resourceInfo);
            }
        }

        private void InstallQueryObject()
        {
            using (new CMSActionContext
            {
                LogSynchronization = false,
                ContinuousIntegrationAllowObjectSerialization = false
            })
            {
                // NOTE: The static method GetQueryInfo has significant optimizations for this
                // query that are not provided by the injected implementation, including using
                // a hashtable for quick in-memory lookup of a QueryInfo by its fully-qualified
                // name.
                // To use the injectable implementation, I would need an IDataClassInfoProvider
                // to get the page type containing the query -- not available. Boo.
                var queryInfo = QueryInfoProvider.GetQueryInfo(QueryConstants.FullyQualifiedQueryName, false)
                                ??
                                new QueryInfo();
                queryInfo.QueryName = QueryConstants.QueryName;
                queryInfo.QueryType = QueryTypeEnum.SQLQuery;
                queryInfo.QueryRequiresTransaction = false;
                queryInfo.QueryIsCustom = true;
                queryInfo.QueryIsLocked = true;
                queryInfo.QueryText = QueryConstants.QueryText;
                if (string.IsNullOrEmpty(queryInfo.QueryClassName))
                {
                    var dataClassInfo = DataClassInfoProvider.GetDataClassInfo(QueryConstants.ClassName);
                    if(dataClassInfo == null)
                    {
                        throw new DataClassNotFoundException($"The query required by the control, '{FormUserControlConstants.ControlDisplayName}', cannot be installed because the page type, '{QueryConstants.ClassName}', is missing.", QueryConstants.ClassName);
                    }
                    queryInfo.ClassID = dataClassInfo.ClassID;
                }
                _queryInfoProvider.Set(queryInfo);
            }
        }

        private void InstallUserControlObject(ResourceInfo resourceInfo)
        {
            using (new CMSActionContext
            {
                LogSynchronization = false,
                ContinuousIntegrationAllowObjectSerialization = false
            })
            {
                var formUserControlInfo = FormUserControlInfoProvider.GetFormUserControlInfo(FormUserControlConstants.ControlCodeName)
                                          ??
                                          new FormUserControlInfo();
                formUserControlInfo.UserControlDisplayName = FormUserControlConstants.ControlDisplayName;
                formUserControlInfo.UserControlCodeName = FormUserControlConstants.ControlCodeName;
                formUserControlInfo.UserControlDescription = FormUserControlConstants.ControlDescription;
                formUserControlInfo.UserControlFileName  = FormUserControlConstants.ControlFileName;
                formUserControlInfo.UserControlResourceID = resourceInfo.ResourceID;
                formUserControlInfo.UserControlForLongText = true;
                formUserControlInfo.UserControlForText = true;
                formUserControlInfo.UserControlForGUID = true;
                formUserControlInfo.UserControlShowInDocumentTypes = true;
                formUserControlInfo.UserControlShowInCustomTables = true;
                formUserControlInfo.UserControlForInteger = false;
                formUserControlInfo.UserControlForDecimal = false;
                formUserControlInfo.UserControlForDateTime = false;
                formUserControlInfo.UserControlForBoolean = false;
                formUserControlInfo.UserControlForFile = false;
                formUserControlInfo.UserControlForDocAttachments = false;
                formUserControlInfo.UserControlForBinary = false;
                formUserControlInfo.UserControlForDocRelationships = false;
                // If someone enables UserControlForGuid/Text or UserControlShowInReports/WebParts/SystemTables,
                // this installer will not override it. There maybe use cases that work.
                formUserControlInfo.UserControlParameters = FormUserControlConstants.ControlParameters;
                FormUserControlInfoProvider.SetFormUserControlInfo(formUserControlInfo);
            }
        }


        private bool InstalledModuleIsCurrent(ResourceInfo resourceInfo)
        {
            return (resourceInfo != null) &&
                   (GetModuleVersionFromAssembly() == resourceInfo.ResourceInstalledVersion);
        }

        /// <summary>
        /// Create a Module version number from the assembly version.
        /// The module version must be in 3 parts (e.g. 1.0.13).
        /// </summary>
        /// <returns></returns>
        private string GetModuleVersionFromAssembly()
        {
            return this.GetType().Assembly.GetName().Version.ToString(3);
        }

        private void LogInformation(string eventCode, string eventMessage)
        {
            _eventLogService.LogEvent(EventTypeEnum.Information,
                                      nameof(TreeNodeSelectorInstaller),
                                      eventCode,
                                      eventMessage);
        }
    }
}
