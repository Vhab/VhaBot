using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;

namespace AoLib.Utils
{
    [XmlRoot("organization")]
    public class OrganizationResult
    {
        [XmlElement("id", Type = typeof(Int32))]
        public Int32 ID;
        [XmlElement("name")]
        public string Name;
        [XmlElement("side")]
        public string Faction;
        [XmlElement("last_updated")]
        public string LastUpdated;
        [XmlElement("members")]
        public OrganizationMembers Members;

        public OrganizationMember Leader
        {
            get
            {
                foreach (OrganizationMember mem in this.Members.Items)
                    if (mem.RankID == 0)
                        return mem;
                return null;
            }
        }
    }
    public class OrganizationMembers
    {
        [XmlElement("member")]
        public OrganizationMember[] Items;

        public OrganizationMember GetMember(string nickname)
        {
            foreach (OrganizationMember member in this.Items)
                if (member.Nickname.ToLower() == nickname.ToLower())
                    return member;
            return null;
        }
    }

    public class OrganizationMember
    {
        [XmlElement("firstname")]
        public string Firstname;
        [XmlElement("nickname")]
        public string Nickname;
        [XmlElement("lastname")]
        public string Lastname;
        [XmlElement("rank", Type = typeof(Int32))]
        public Int32 RankID;
        [XmlElement("rank_name")]
        public string Rank;
        [XmlElement("level", Type = typeof(Int32))]
        public Int32 Level;
        [XmlElement("profession")]
        public string Profession;
        [XmlElement("gender")]
        public string Gender;
        [XmlElement("breed")]
        public string Breed;
        [XmlElement("defender_rank")]
        public string DefenderRank;
        [XmlElement("defender_rank_id", Type = typeof(Int32))]
        public Int32 DefenderLevel;
        [XmlElement("photo_url")]
        public string PictureUrl;
        [XmlElement("smallphoto_url")]
        public string SmallPictureUrl;

        public WhoisResult ToWhoisResult(OrganizationResult organization)
        {
            WhoisResult result = new WhoisResult();
            result.Name = new WhoisResult_Name();
            result.Name.Firstname = this.Firstname;
            result.Name.Nickname = this.Nickname;
            result.Name.Lastname = this.Lastname;

            result.Stats = new WhoisResult_Stats();
            result.Stats.Level = this.Level;
            result.Stats.Breed = this.Breed;
            result.Stats.Gender = this.Gender;
            result.Stats.Faction = organization.Faction;
            result.Stats.Profession = this.Profession;
            result.Stats.Title = null;
            result.Stats.DefenderRank = this.DefenderRank;
            result.Stats.DefenderLevel = this.DefenderLevel;

            result.Organization = new WhoisResult_Organization();
            result.Organization.ID = organization.ID;
            result.Organization.Name = organization.Name;
            result.Organization.Rank = this.Rank;
            result.Organization.RankID = this.RankID;

            result.PictureURL = this.PictureUrl;
            result.SmallPictureURL = this.SmallPictureUrl;
            result.LastUpdated = organization.LastUpdated;

            return result;
        }

        public void FromWhoisResult(WhoisResult whois)
        {
            if (whois == null)
                return;

            if (whois.Name != null)
            {
                this.Firstname = whois.Name.Firstname;
                this.Nickname = whois.Name.Nickname;
                this.Lastname = whois.Name.Lastname;
            }
            if (whois.Stats != null)
            {
                this.Level = whois.Stats.Level;
                this.Breed = whois.Stats.Breed;
                this.Gender = whois.Stats.Gender;
                this.Profession = whois.Stats.Profession;
                this.DefenderRank = whois.Stats.DefenderRank;
                this.DefenderLevel = whois.Stats.DefenderLevel;
            }
            if (whois.Organization != null)
            {
                this.Rank = whois.Organization.Rank;
                this.RankID = whois.Organization.RankID;
            }
            this.PictureUrl = whois.PictureURL;
            this.SmallPictureUrl = whois.SmallPictureURL;
        }
    }

    [XmlRoot("organization")]
    public class OrganizationCache
    {
        [XmlElement("id", Type = typeof(Int32))]
        public Int32 ID;
        [XmlElement("name")]
        public string Name;
        [XmlElement("side")]
        public string Faction;
        [XmlElement("last_updated")]
        public string LastUpdated;
        [XmlElement("member")]
        public string[] Members;

        public void FromOrganizationResult(OrganizationResult result)
        {
            this.ID = result.ID;
            this.Name = result.Name;
            this.Faction = result.Faction;
            this.LastUpdated = result.LastUpdated;
            this.Members = new string[result.Members.Items.Length];
            int i = 0;
            foreach (OrganizationMember member in result.Members.Items)
            {
                this.Members[i] = member.Nickname;
                i++;
            }
        }

        public OrganizationResult ToOrganizationResult(Net.Server server)
        {
            OrganizationResult result = new OrganizationResult();
            result.ID = this.ID;
            result.Name = this.Name;
            result.Faction = this.Faction;
            result.LastUpdated = this.LastUpdated;
            result.Members = new OrganizationMembers();
            result.Members.Items = new OrganizationMember[this.Members.Length];
            for (int i = 0; i < this.Members.Length; i++)
            {
                result.Members.Items[i] = new OrganizationMember();
                result.Members.Items[i].FromWhoisResult(XML.GetWhois(this.Members[i], server, true, true, true));
            }
            return result;
        }
    }
}
