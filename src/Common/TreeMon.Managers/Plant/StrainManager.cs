﻿// Copyright (c) 2017 TreeMon.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://treemon.org/docs/license.txt  for full license details.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TreeMon.Data;
using TreeMon.Data.Logging.Models;
using TreeMon.Managers.Interfaces;
using TreeMon.Models;
using TreeMon.Models.App;
using TreeMon.Models.General;
using TreeMon.Models.Plant;
using TreeMon.Models.Store;
using TreeMon.Utilites.Extensions;

namespace TreeMon.Managers.Plant
{
    public class StrainManager : BaseManager, ICrud
    {
        public StrainManager(string connectionKey, string sessionKey) : base(connectionKey, sessionKey)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionKey), "StrainManager CONTEXT IS NULL!");


            this._connectionKey = connectionKey;
        }

        public Strain AddStrainFromProduct(Product p)
        {
            if (p == null)
                return null;
            using (var context = new TreeMonDbContext(this._connectionKey))
            {
                Category c = context.GetAll<Category>().FirstOrDefault(w => w.UUID == p.CategoryUUID);
                if (c == null || c.UsesStrains == false)
                    return null;
            }
            //since the user can type a new strain in the uuid field, search the db to make sure
            //         
            Strain s = FindStrain(p.StrainUUID, p.StrainUUID, p.AccountUUID);

            if (s != null)
                return s;

            string variety = DetectVariety(p.CategoryUUID);

            //If it wasn't found and because the ui allows adding via text/combobox.
            // we assign the uuid to the name.

            Strain tmpStrain = new Strain()
            {
                AccountUUID = p.AccountUUID,
                Active = true,
                CreatedBy = p.CreatedBy,
                DateCreated = DateTime.UtcNow,
                Deleted = false,
                Name = p.StrainUUID,
                CategoryUUID = variety,
                UUIDType = "Strain"
            };

            ServiceResult res = this.Insert(tmpStrain);

            if (res.Code == 200)
                return (Strain)res.Result;

            return null;
        }

        //only a few product categories have a variety name in it.
        //
        protected string DetectVariety(string productCategory)
        {
            if (string.IsNullOrWhiteSpace(productCategory))
                return productCategory;

            string temp = productCategory.ToUpper();
            if (temp.Contains("HYBRID"))
                return "Hybrid";

            if (temp.Contains("SATIVA"))
                return "Sativa";

            if (temp.Contains("INDICA"))
                return "Indica";

            return "";
        }

        public ServiceResult Delete(INode s, bool purge = false)
        {
            ServiceResult res = ServiceResponse.OK();

            if (s == null)
                return ServiceResponse.Error("No record sent.");

            if (!this.DataAccessAuthorized(s, "DELETE", false)) return ServiceResponse.Error("You are not authorized this action.");

            using (var context = new TreeMonDbContext(this._connectionKey))
            {
                if (purge)
                {
                    if (context.Delete<Strain>((Strain)s) == 0)
                        return ServiceResponse.Error(s.Name + " failed to delete. ");
                }

                //get the strain from the table with all the data so when its updated it still contains the same data.
                s = this.GetBy(s.UUID);
                if (s == null)
                    ServiceResponse.Error("Strain not found.");
                s.Deleted = true;
                if (context.Update<Strain>((Strain)s) == 0)
                    return ServiceResponse.Error(s.Name + " failed to delete. ");
            }
            return res;
        }

        public INode Get( string name)
        {
            //if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            if (string.IsNullOrWhiteSpace(name))
                return null;
            using (var context = new TreeMonDbContext(this._connectionKey))
            {
                return context.GetAll<Strain>().FirstOrDefault(sw => sw.Name.EqualsIgnoreCase(name));
            }
        }

        public List<Strain> GetStrains(string accountUUID, bool deleted = false, bool includeSystemAccount = false)
        {

            //if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");

            using (var context = new TreeMonDbContext(this._connectionKey))
            {
                if (includeSystemAccount)
                {
                    return context.GetAll<Strain>().Where(sw => (sw.AccountUUID == accountUUID || sw.AccountUUID == SystemFlag.Default.Account) && sw.Deleted == deleted).GroupBy(x => x.Name).Select(group => group.First()).OrderBy(ob => ob.Name).ToList();
                }
                return context.GetAll<Strain>().Where(sw => (sw.AccountUUID == accountUUID) && sw.Deleted == deleted).OrderBy(ob => ob.Name).ToList();
            }
        }


        public INode GetBy(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return null;
            using (var context = new TreeMonDbContext(this._connectionKey))
            {
                return context.GetAll<Strain>().FirstOrDefault(sw => sw.UUID == uuid);
            }
        }

        public INode GetByGetBySyncKey(string syncKey)
        {
            if (string.IsNullOrWhiteSpace(syncKey))
                return null;
            using (var context = new TreeMonDbContext(this._connectionKey))
            {
                return context.GetAll<Strain>().FirstOrDefault(sw => sw.SyncKey == syncKey);
            }
        }


        private Strain FindStrain(string uuid, string name, string AccountUUID, bool includeDefaultAccount = true)
        {
            Strain res = null;

            if (string.IsNullOrWhiteSpace(uuid) && string.IsNullOrWhiteSpace(name) == false)
                res = (Strain)this.Get(name);
            else if (string.IsNullOrWhiteSpace(uuid) == false && string.IsNullOrWhiteSpace(name))
                res = (Strain)this.GetBy(uuid);
            else
            {
                using (var context = new TreeMonDbContext(this._connectionKey))
                {
                    res = context.GetAll<Strain>().FirstOrDefault(w => w.UUID == uuid || (w.Name?.EqualsIgnoreCase(name) ?? false));
                }
            }

            if (res == null)
                return res;

            if (includeDefaultAccount && (res.AccountUUID == AccountUUID))
                return res;

            if (res.AccountUUID == AccountUUID)
                return res;

            return null;

        }

        public ServiceResult Insert(INode n, bool validateFirst = true)
        {
            if (!this.DataAccessAuthorized(n, "POST", false)) return ServiceResponse.Error("You are not authorized this action.");

            n.Initialize(this._requestingUser.UUID, this._requestingUser.AccountUUID, this._requestingUser.RoleWeight);

            var s = (Strain)n;

            using (var context = new TreeMonDbContext(this._connectionKey))
            {
                if (validateFirst)
                {
                    Strain dbU = context.GetAll<Strain>().FirstOrDefault(wu => (wu.Name?.EqualsIgnoreCase(s.Name) ?? false) && wu.AccountUUID == s.AccountUUID);

                    if (dbU != null)
                        return ServiceResponse.Error("Strain already exists.");
                }
            
                if (context.Insert<Strain>((Strain)s))
                    return ServiceResponse.OK("", s);
            }
            return ServiceResponse.Error("An error occurred inserting strain " + s.Name);
        }

        public ServiceResult Update(INode s)
        {
            if (s == null)
                return ServiceResponse.Error("Invalid Strain data.");

            if (!this.DataAccessAuthorized(s, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

            using (var context = new TreeMonDbContext(this._connectionKey))
            {
                if (context.Update<Strain>((Strain)s) > 0)
                    return ServiceResponse.OK();
            }
            return ServiceResponse.Error("System error, Strain was not updated.");
        }

    }
}
