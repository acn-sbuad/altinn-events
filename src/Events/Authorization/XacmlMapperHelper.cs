﻿using System.Collections.Generic;
using System.Security.Claims;

using Altinn.Authorization.ABAC.Xacml.JsonProfile;
using Altinn.Common.PEP.Helpers;

namespace Altinn.Platform.Events.Authorization
{
    /// <summary>
    /// Shared methods for mapping to xacml
    /// </summary>
    public static class XacmlMapperHelper
    {
        private const string DefaultIssuer = "Altinn";

        private const string UserPrefix = "/user/";
        private const string OrgPrefix = "/org/";
        private const string PartyPrefix = "/party/";

        private const string ClaimUserId = "urn:altinn:userid";
        private const string ClaimPartyID = "urn:altinn:partyid";
        private const string ClaimOrg = "urn:altinn:org";

        /// <summary>
        /// Generates subject attribute list 
        /// </summary>
        /// <returns>The XacmlJsonCategory or the subject</returns>
        public static XacmlJsonCategory CreateSubjectAttributes(string subject)
        {
            XacmlJsonCategory category = new()
            {
                Attribute = new List<XacmlJsonAttribute>()
            };

            if (subject.StartsWith(UserPrefix))
            {
                string value = subject.Replace(UserPrefix, string.Empty);
                category.Attribute.Add(DecisionHelper.CreateXacmlJsonAttribute(ClaimUserId, value, ClaimValueTypes.String, DefaultIssuer));
            }
            else if (subject.StartsWith(OrgPrefix))
            {
                string value = subject.Replace(OrgPrefix, string.Empty);
                category.Attribute.Add(DecisionHelper.CreateXacmlJsonAttribute(ClaimOrg, value, ClaimValueTypes.String, DefaultIssuer));
            }
            else if (subject.StartsWith(PartyPrefix))
            {
                string value = subject.Replace(PartyPrefix, string.Empty);
                category.Attribute.Add(DecisionHelper.CreateXacmlJsonAttribute(ClaimPartyID, value, ClaimValueTypes.Integer, DefaultIssuer));
            }

            return category;
        }
    }
}
