﻿// Copyright (c) 2017 TreeMon.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://treemon.org/docs/license.txt  for full license details.
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TreeMon.Data;
using TreeMon.Data.Logging;
using TreeMon.Models.Datasets;
using TreeMon.Utilites.Extensions;

/// <summary>
/// For implementation see
/// See 
///     ReportsController
///     web page..
/// </summary>
namespace TreeMon.Managers.DataSets
{
    public class DatasetManager : BaseManager
    {
        private SystemLogger _logger = null;

        public DatasetManager(string connectionKey, string sessionKey) : base(connectionKey, sessionKey)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionKey), "DatasetManager CONTEXT IS NULL!");
               
            this._connectionKey = connectionKey;
                _logger = new SystemLogger(_connectionKey);
        }

        public List<DataPoint> GetData(string type, string field, List<DataScreen> screens)
        {
            List<DataPoint> dataset = new List<DataPoint>();
            DataQuery dq = BuildQuery(type, screens);
            if (dq == null || dq.SQL.Contains("ERROR:"))
            {
                Debug.Assert(false, "Error building reports getdata query." + dq.SQL);
                dataset.Add(new DataPoint() {
                    Value = "Error building the query.",
                    ValueType = "ERROR"
                    });
                _logger.InsertError("Error building query." + Environment.NewLine + dq.SQL, "DatasetManager", "GetData:" + type + Environment.NewLine + field);
                return dataset;
            }
            IDataReader reader = null;

            try
            {
                using (var context = new TreeMonDbContext(this._connectionKey))
                {
                    reader = context.Execute(dq.SQL, dq.Parameters);
             
                    if (reader == null || reader.FieldCount == 0)
                        return dataset;

                    while (reader.Read())
                    {
                        dataset.Add(new DataPoint()
                        {
                            Value = reader[field].ToString(),
                            ValueType = reader[field].GetType().Name
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
                dataset.Add(new DataPoint()
                {
                    Value = "Error getting report data.",
                    ValueType = "ERROR"
                });
                _logger.InsertError(ex.Message + Environment.NewLine + dq.SQL, "DatasetManager", "GetData:" + type + ":" + field);
            }
            finally
            {
                if (reader != null) { reader.Close(); }
                //if (conn != null) conn.Close();
            }
            return dataset;
        }


        /// <summary>
        /// returns a query based on the screens
        /// Not using parameters. kept throwing “Invalid type owner for DynamicMethod” error
        /// </summary>
        /// <param name="screens"></param>
        /// <returns></returns>
        protected DataQuery BuildQuery<T>( List<DataScreen> screens) where T : class
        {
            DataQuery dq = new DataQuery();
            string table = new TreeMonDbContext(this._connectionKey).GetTableName<T>();
            if (string.IsNullOrEmpty(table))
            {   dq.SQL = "ERROR: Table name could not be parsed.";
                return dq;
            }

            dq.SQL = "SELECT * FROM " + table;

            if (screens.Count() == 0)
                return dq;

            dq.SQL += GetWhereClause(screens);
         
            dq.Parameters = null;
            
            return dq;
        }

        protected DataQuery BuildQuery( string typeName , List<DataScreen> screens ) 
        {
            DataQuery dq = new DataQuery();

            string table = new TreeMonDbContext(this._connectionKey).GetTableName(typeName);

            if (string.IsNullOrEmpty(table))
            {
                dq.SQL = "ERROR: Table name could not be found.";
                return dq;
            }

            //Set default sql query
            dq.SQL = "SELECT * FROM " + table;
            dq.Parameters = null;

            if (screens == null || screens.Count() == 0)
                return dq;

            //Check if we do a distinct query.
            //
            DataScreen distinctDataScreen = screens.FirstOrDefault(w => w.Command?.ToLower() == "distinct");

            if(distinctDataScreen == null)
                dq.SQL += GetWhereClause(screens);
            else
                dq.SQL = "SELECT DISTINCT( " + distinctDataScreen.Field + " ) FROM " + table  + GetWhereClause(screens);


            return dq;
        }

        private string GetWhereClause(List<DataScreen> screens)
        {
            if (screens == null || screens.Count == 0)
                return string.Empty;

            screens = screens.Where(fw => fw.Type == "sql").ToList();
            if (screens == null)
                return string.Empty;


            StringBuilder SQL = new StringBuilder( " WHERE " );

            int screenIndex = 1;

            foreach (DataScreen screen in screens)
            {
                if (screen.Type != "sql" || screen.Command?.ToLower() == "distinct" )
                    continue;

                #region range query
                if(screen.Command.EqualsIgnoreCase("BETWEEN"))
                {
                    if (screen.Order == 0)
                        SQL.Append(  screen.Field + " BETWEEN '" + screen.Value +  "' " + screen.Junction + " ");

                    if (screen.Order == 1)
                    {
                        SQL.Append(" '" + screen.Value + "'");
                        if ( screenIndex != screens.Count )
                            SQL.Append(" " + screen.Junction + " ");
                    }
                    screenIndex++;
                    continue;
                }
                #endregion

                if (screenIndex == screens.Count)
                    SQL.Append(screen.Field + " " + screen.Command + " '" + screen.Value + "'");
                else
                    SQL.Append(screen.Field + " " + screen.Command + " '" + screen.Value + "' " + screen.Junction + " ");

                SQL = SQL.Replace("'(", "(").Replace(")'", ")");
                //dapper requires this format
                //SELECT * FROM " + this.TableName + " WHERE name = @accountName AND password = @accountPassword
                //var parameters2 = new { accountName = screen.Value, accountPassword = screen.Value };

                screenIndex++;
            }

            if (SQL.Equals( " WHERE "))
                return "";//no where statement compiled

            return SQL.ToString();
        }
    }
}
