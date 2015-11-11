﻿using System;
using System.Diagnostics;
using System.Collections;
using System.Web.Http;
using System.Web.Http.Cors;

using DynamicsIntegration.Models;
using DynamicsIntegration.Services;
using System.ServiceModel.Security;

namespace DynamicsIntegration.Controllers
{

    [RoutePrefix("Dynamics")]
    [EnableCors(origins: "http://172.17.123.104:8888", headers: "*", methods: "*")]
    public class CrmApiController : ApiController
    {
        CrmService crmService;
        CrmApiHelper helper;

        [Route("Validate")]
        [HttpPost]
        public IHttpActionResult ValidateCredentials([FromUri]DynamicsCredentials credentials)
        {
            try
            {
                crmService = new CrmService(credentials);
                ArrayList orgNames = crmService.getOrganizations();
                return Ok(orgNames);
            }
            catch (Exception ex) when (ex is MessageSecurityException || ex is ArgumentNullException)
            {
                return Unauthorized();
            }
        }

        [Route("Organizations/{orgName}/MarketLists")]
        [HttpPost]
        public IHttpActionResult GetListsWithAllAttributes(string orgName, [FromUri]DynamicsCredentials credentials, [FromUri] bool translate = false, [FromUri] bool allValues = true)
        {
            try
            {
                crmService = new CrmService(credentials);
                crmService.setOrgName(orgName);
                helper = new CrmApiHelper(crmService);
            }
            catch (Exception ex) when (ex is MessageSecurityException || ex is ArgumentNullException)
            {
                return Unauthorized();
            }

            var lists = crmService.getAllLists(allValues);
            var responsObject = helper.getValuesFromLists(lists, translate, allValues);

            return Ok(responsObject);
        }

        [Route("Organizations/{orgName}/MarketLists/{listId}/Contacts")]
        [HttpPost]
        public IHttpActionResult GetContactsWithAttributes(string orgName, string listId, [FromUri]DynamicsCredentials credentials, [FromUri] int top = 0, [FromUri] bool translate = true, [FromUri] bool allValues = true)
        {
            Debug.WriteLine("contacts anrop " + DateTime.Now.ToString());
            try
            {
                crmService = new CrmService(credentials);
                crmService.setOrgName(orgName);
                helper = new CrmApiHelper(crmService);
                Debug.WriteLine("contacts anrop efter try" + DateTime.Now.ToString());
            }
            catch(Exception ex) when (ex is MessageSecurityException || ex is ArgumentNullException)
            {
                return Unauthorized();
            }

            try
            {
                Debug.WriteLine("contacts anrop innan resp obj " + DateTime.Now.ToString());
                var contacts = crmService.getContactsInList(listId, allValues, top);
                var responsObject = helper.getValuesFromContacts(contacts, translate, allValues);
                Debug.WriteLine("contacts anrop efter resp obj" + DateTime.Now.ToString());

                return Ok(responsObject);
            }
            catch(FormatException)
            {
                return BadRequest();
            }
        }

        [Route("Organizations/{orgId}/Contacts/{contactId}/Donotbulkemail")]
        [HttpPut]
        public IHttpActionResult UpdateBulkEmailForContact(string orgName, string contactId, [FromUri]DynamicsCredentials credentials)
        {
            try
            {
                crmService = new CrmService(credentials);
                crmService.setOrgName(orgName);
            }
            catch (Exception ex) when (ex is MessageSecurityException || ex is ArgumentNullException)
            {
                return Unauthorized();
            }

            try
            {
                crmService.changeBulkEmail(contactId);
                return Ok();
            }
            catch (FormatException)
            {
                return BadRequest();
            }
        }

        [Route("Contacts/donotbulkemail")]
        [HttpPut]
        public IHttpActionResult testMultipleContacts([FromUri]DynamicsCredentials credentials)
        {
            try
            {
                crmService = new CrmService(credentials);
            }
            catch (Exception)
            {
                return Unauthorized();
            }

            return Ok();
        }

        private Contact createCont(Guid guid)
        {
            Contact contact = new Contact();
            contact.ContactId = guid;
            return contact;
        }

    }
}