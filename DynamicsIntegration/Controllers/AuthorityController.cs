﻿// =====================================================================
//  This file is part of the Microsoft Dynamics CRM SDK code samples.
//
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  This source code is intended only as a supplement to Microsoft
//  Development Tools and/or on-line documentation.  See these other
//  materials for detailed information regarding Microsoft code samples.
//
//  THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//  PARTICULAR PURPOSE.
// =====================================================================

//<snippetAuthenticateWithNoHelp>
using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Collections;
using System.Reflection;

// These namespaces are found in the Microsoft.Xrm.Sdk.dll assembly
// located in the SDK\bin folder of the SDK download.
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Discovery;
using Microsoft.Crm.Sdk.Messages;

namespace DynamicsIntegration.Controllers
{
    /// <summary>
    /// Demonstrate how to do basic authentication using IServiceManagement and SecurityTokenResponse.
    /// </summary>
    class AuthorityController
    {
        #region Class Level Members
        // To get discovery service address and organization unique name, 
        // Sign in to your CRM org and click Settings, Customization, Developer Resources.
        // On Developer Resource page, find the discovery service address under Service Endpoints and organization unique name under Your Organization Information.
        //private String _discoveryServiceAddress = "https://dev.crm.dynamics.com/XRMServices/2011/Discovery.svc";
        private String _discoveryServiceAddress = " https://disco.crm4.dynamics.com/XRMServices/2011/Discovery.svc";
        //private String _organizationUniqueName = "OrganizationUniqueName";
        //private String _organizationUniqueName = "org66aa9e14";
        // Provide your user name and password.
        //private String _userName = "username@mydomain.com";
        public static String _userName;// = "stefan.degeer@ungapped.com";
        //private String _password = "password";
        public static String _password;//  = "Q1w2e3r4";

        // Provide domain name for the On-Premises org.
        //private String _domain = "mydomain";
        public static String _domain;// = "ungapped.com";

        private static OrganizationServiceProxy organizationServiceProxy = null;

        #endregion Class Level Members

        public bool login()
        {
            try
            {
                organizationServiceProxy = getOrganizationServiceProxy();
            }
            catch(Exception)
            {
                organizationServiceProxy = null;
                return false;
            }

            return true;
        }

        public EntityCollection getAllLists()
        {
            EntityCollection results = new EntityCollection();
            QueryExpression query = new QueryExpression { EntityName = "list", ColumnSet = new ColumnSet("listname", "membercount", "listid", "modifiedon") };

            query.AddOrder("modifiedon", OrderType.Descending);
            results = organizationServiceProxy.RetrieveMultiple(query);
               
            return results;
        }

        public ArrayList getContactsInList(string id)
        {
            ArrayList contacts = new ArrayList();
            Guid listid;
            EntityCollection results = new EntityCollection();
            QueryExpression query = new QueryExpression { EntityName = "listmember", ColumnSet = new ColumnSet("listid", "entityid") };

            try
            {
                listid = new Guid(id);
            }
            catch(FormatException)
            {
                return contacts;
            }

            results = organizationServiceProxy.RetrieveMultiple(query);
            
            foreach (ListMember member in results.Entities)
            {
                if (member.ListId.Id == listid)
                {
                    Contact contact = organizationServiceProxy.Retrieve("contact", member.EntityId.Id, new ColumnSet(new string[] { "firstname", "lastname", "emailaddress1" })).ToEntity<Contact>();
                    contacts.Add(contact);
                }

            }

            return contacts;
        }

        public OrganizationServiceProxy getOrganizationServiceProxy()
        {
            //<snippetAuthenticateWithNoHelp1>
            IServiceManagement<IDiscoveryService> serviceManagement =
                        ServiceConfigurationFactory.CreateManagement<IDiscoveryService>(
                        new Uri(_discoveryServiceAddress));
            AuthenticationProviderType endpointType = serviceManagement.AuthenticationType;

            // Set the credentials.
            AuthenticationCredentials authCredentials = GetCredentials(serviceManagement, endpointType);

            String organizationUri = String.Empty;
            // Get the discovery service proxy.
            using (DiscoveryServiceProxy discoveryProxy =
                GetProxy<IDiscoveryService, DiscoveryServiceProxy>(serviceManagement, authCredentials))
            {
                // Obtain organization information from the Discovery service. 
                if (discoveryProxy != null)
                {
                    // Obtain information about the organizations that the system user belongs to.
                    OrganizationDetailCollection orgs = DiscoverOrganizations(discoveryProxy);

                    string _organizationUniqueName = orgs.ToArray()[0].UniqueName;

                    // Obtains the Web address (Uri) of the target organization.
                   organizationUri = FindOrganization(_organizationUniqueName,
                        orgs.ToArray()).Endpoints[EndpointType.OrganizationService];
                       
                }
            }
            //</snippetAuthenticateWithNoHelp1>

            if (!String.IsNullOrWhiteSpace(organizationUri))
            {
                //<snippetAuthenticateWithNoHelp3>
                IServiceManagement<IOrganizationService> orgServiceManagement =
                    ServiceConfigurationFactory.CreateManagement<IOrganizationService>(
                    new Uri(organizationUri));

                // Set the credentials.
                AuthenticationCredentials credentials = GetCredentials(orgServiceManagement, endpointType);

                // Get the organization service proxy.
                using (organizationServiceProxy =
                    GetProxy<IOrganizationService, OrganizationServiceProxy>(orgServiceManagement, credentials))
                {
                    // This statement is required to enable early-bound type support.
                    organizationServiceProxy.EnableProxyTypes();

                }
            }

            return organizationServiceProxy;
        }


        //<snippetAuthenticateWithNoHelp2>
        /// <summary>
        /// Obtain the AuthenticationCredentials based on AuthenticationProviderType.
        /// </summary>
        /// <param name="service">A service management object.</param>
        /// <param name="endpointType">An AuthenticationProviderType of the CRM environment.</param>
        /// <returns>Get filled credentials.</returns>
        private AuthenticationCredentials GetCredentials<TService>(IServiceManagement<TService> service, AuthenticationProviderType endpointType)
        {
            AuthenticationCredentials authCredentials = new AuthenticationCredentials();

            switch (endpointType)
            {
                case AuthenticationProviderType.ActiveDirectory:
                    authCredentials.ClientCredentials.Windows.ClientCredential =
                        new System.Net.NetworkCredential(_userName,
                            _password,
                            _domain);
                    break;
                case AuthenticationProviderType.LiveId:
                    authCredentials.ClientCredentials.UserName.UserName = _userName;
                    authCredentials.ClientCredentials.UserName.Password = _password;
                    authCredentials.SupportingCredentials = new AuthenticationCredentials();
                    authCredentials.SupportingCredentials.ClientCredentials = 
                        Microsoft.Crm.Services.Utility.DeviceIdManager.LoadOrRegisterDevice();
                    break;
                default: // For Federated and OnlineFederated environments.                    
                    authCredentials.ClientCredentials.UserName.UserName = _userName;
                    authCredentials.ClientCredentials.UserName.Password = _password;
                    // For OnlineFederated single-sign on, you could just use current UserPrincipalName instead of passing user name and password.
                    // authCredentials.UserPrincipalName = UserPrincipal.Current.UserPrincipalName;  // Windows Kerberos

                    // The service is configured for User Id authentication, but the user might provide Microsoft
                    // account credentials. If so, the supporting credentials must contain the device credentials.
                    if (endpointType == AuthenticationProviderType.OnlineFederation)
                    {
                        IdentityProvider provider = service.GetIdentityProvider(authCredentials.ClientCredentials.UserName.UserName);
                        if (provider != null && provider.IdentityProviderType == IdentityProviderType.LiveId)
                        {
                            authCredentials.SupportingCredentials = new AuthenticationCredentials();
                            authCredentials.SupportingCredentials.ClientCredentials =
                                Microsoft.Crm.Services.Utility.DeviceIdManager.LoadOrRegisterDevice();
                        }
                    }

                    break;
            }

            return authCredentials;
        }
        //</snippetAuthenticateWithNoHelp2>

        /// <summary>
        /// Discovers the organizations that the calling user belongs to.
        /// </summary>
        /// <param name="service">A Discovery service proxy instance.</param>
        /// <returns>Array containing detailed information on each organization that 
        /// the user belongs to.</returns>
        public OrganizationDetailCollection DiscoverOrganizations(
            IDiscoveryService service)
        {
            if (service == null) throw new ArgumentNullException("service");
            RetrieveOrganizationsRequest orgRequest = new RetrieveOrganizationsRequest();
            RetrieveOrganizationsResponse orgResponse =
                (RetrieveOrganizationsResponse)service.Execute(orgRequest);

            return orgResponse.Details;
        }

        /// <summary>
        /// Finds a specific organization detail in the array of organization details
        /// returned from the Discovery service.
        /// </summary>
        /// <param name="orgUniqueName">The unique name of the organization to find.</param>
        /// <param name="orgDetails">Array of organization detail object returned from the discovery service.</param>
        /// <returns>Organization details or null if the organization was not found.</returns>
        /// <seealso cref="DiscoveryOrganizations"/>
        public OrganizationDetail FindOrganization(string orgUniqueName,
            OrganizationDetail[] orgDetails)
        {
            if (String.IsNullOrWhiteSpace(orgUniqueName))
                throw new ArgumentNullException("orgUniqueName");
            if (orgDetails == null)
                throw new ArgumentNullException("orgDetails");
            OrganizationDetail orgDetail = null;

            foreach (OrganizationDetail detail in orgDetails)
            {
                if (String.Compare(detail.UniqueName, orgUniqueName,
                    StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    orgDetail = detail;
                    break;
                }
            }
            return orgDetail;
        }

        /// <summary>
        /// Generic method to obtain discovery/organization service proxy instance.
        /// </summary>
        /// <typeparam name="TService">
        /// Set IDiscoveryService or IOrganizationService type to request respective service proxy instance.
        /// </typeparam>
        /// <typeparam name="TProxy">
        /// Set the return type to either DiscoveryServiceProxy or OrganizationServiceProxy type based on TService type.
        /// </typeparam>
        /// <param name="serviceManagement">An instance of IServiceManagement</param>
        /// <param name="authCredentials">The user's Microsoft Dynamics CRM logon credentials.</param>
        /// <returns></returns>
        /// <snippetAuthenticateWithNoHelp4>
        private TProxy GetProxy<TService, TProxy>(
            IServiceManagement<TService> serviceManagement,
            AuthenticationCredentials authCredentials)
            where TService : class
            where TProxy : ServiceProxy<TService>
        {
            Type classType = typeof(TProxy);

            if (serviceManagement.AuthenticationType !=
                AuthenticationProviderType.ActiveDirectory)
            {
                AuthenticationCredentials tokenCredentials =
                    serviceManagement.Authenticate(authCredentials);
                // Obtain discovery/organization service proxy for Federated, LiveId and OnlineFederated environments. 
                // Instantiate a new class of type using the 2 parameter constructor of type IServiceManagement and SecurityTokenResponse.
                return (TProxy)classType
                    .GetConstructor(new Type[] { typeof(IServiceManagement<TService>), typeof(SecurityTokenResponse) })
                    .Invoke(new object[] { serviceManagement, tokenCredentials.SecurityTokenResponse });
            }

            // Obtain discovery/organization service proxy for ActiveDirectory environment.
            // Instantiate a new class of type using the 2 parameter constructor of type IServiceManagement and ClientCredentials.
            return (TProxy)classType
                .GetConstructor(new Type[] { typeof(IServiceManagement<TService>), typeof(ClientCredentials) })
                .Invoke(new object[] { serviceManagement, authCredentials.ClientCredentials });
        }
        /// </snippetAuthenticateWithNoHelp4
    }
}
//</snippetAuthenticateWithNoHelp>