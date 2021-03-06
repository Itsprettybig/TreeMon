﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TreeMon.Data;
using TreeMon.Models.App;
using System.Collections.Generic;
using TreeMon.Managers.Membership;

namespace TreeMon.Web.Tests._templates
{
    //NOTE: this class is not complete or implelmented. 
    //This is for future reference.
    //
    [TestClass]
    public class KeyManager_Tests
    {
        private string connectionKey = "MSSQL_TEST";

        [TestMethod]
        public void KeyManager_GenerateKey()
        {
            KeyManager m = new KeyManager(new TreeMonDbContext(connectionKey));

            Assert.IsTrue(m.GenerateKey("APIKEY", 15).Length == 15 );
            Assert.IsTrue(m.GenerateKey("USERKEY", 10).Length == 10);
            Assert.IsTrue(string.IsNullOrWhiteSpace(m.GenerateKey("USERKEY", 0) ));
        }

      
    }
}
