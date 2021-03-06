﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Users;
using NBrightCore.common;
using NBrightCore.render;
using NBrightDNN;

namespace Nevoweb.DNN.NBrightBuy.Components.Clients
{
    public static class ProductFunctions
    {
        #region "Client Admin Methods"

        public static string ProcessCommand(string paramCmd,HttpContext context)
        {
            var strOut = "CLIENT - ERROR!! - No Security rights for current user!";
            if (NBrightBuyUtils.CheckManagerRights())
            {
                var ajaxInfo = NBrightBuyUtils.GetAjaxFields(context);
                var userId = ajaxInfo.GetXmlPropertyInt("genxml/hidden/userid");

                switch (paramCmd)
                {
                    case "product_admin_getlist":
                        strOut = ProductFunctions.ProductAdminList(context);
                        break;
                    case "product_admin_getdetail":
                        strOut = ProductFunctions.ProductAdminDetail(context);
                        break;
                    case "product_admin_save":
                        strOut = ProductFunctions.ProductAdminSave(context);
                        break;
                    case "product_admin_selectlist":
                        strOut = ProductFunctions.ProductAdminList(context);
                        break;
                    case "product_moveproductadmin":
                        strOut = ProductFunctions.MoveProductAdmin(context);
                        break;
                        
                }
            }
            return strOut;
        }

        public static String ProductAdminDetail(HttpContext context)
        {
            try
            {
                if (NBrightBuyUtils.CheckManagerRights())
                {
                    var settings = NBrightBuyUtils.GetAjaxDictionary(context);
                    var strOut = "";
                    var selecteditemid = settings["selecteditemid"];
                    if (Utils.IsNumeric(selecteditemid))
                    {

                        if (!settings.ContainsKey("themefolder")) settings.Add("themefolder", "");
                        if (!settings.ContainsKey("razortemplate")) settings.Add("razortemplate", "");
                        if (!settings.ContainsKey("portalid")) settings.Add("portalid", PortalSettings.Current.PortalId.ToString("")); // aways make sure we have portalid in settings
                        if (!settings.ContainsKey("selecteditemid")) settings.Add("selecteditemid", "");

                        var themeFolder = settings["themefolder"];

                        var razortemplate = settings["razortemplate"];
                        var portalId = Convert.ToInt32(settings["portalid"]);

                        var passSettings = settings;
                        foreach (var s in StoreSettings.Current.Settings()) // copy store setting, otherwise we get a byRef assignement
                        {
                            if (passSettings.ContainsKey(s.Key))
                                passSettings[s.Key] = s.Value;
                            else
                                passSettings.Add(s.Key, s.Value);
                        }

                        if (!Utils.IsNumeric(selecteditemid)) return "";

                        if (themeFolder == "")
                        {
                            themeFolder = StoreSettings.Current.ThemeFolder;
                            if (settings.ContainsKey("themefolder")) themeFolder = settings["themefolder"];
                        }

                        var objCtrl = new NBrightBuyController();
                        var info = objCtrl.Get(Convert.ToInt32(selecteditemid), "PRD", Utils.GetCurrentCulture());

                        strOut = NBrightBuyUtils.RazorTemplRender(razortemplate, 0, "", info, "/DesktopModules/NBright/NBrightBuy", themeFolder, Utils.GetCurrentCulture(), passSettings);
                    }
                    return strOut;
                }
                return "";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        public static String ProductAdminSave(HttpContext context)
        {
            try
            {
                if (NBrightBuyUtils.CheckManagerRights())
                {
                    var ajaxInfo = NBrightBuyUtils.GetAjaxFields(context);
                    var userId = ajaxInfo.GetXmlPropertyInt("genxml/hidden/userid");
                    if (userId > 0)
                    {
                        var clientData = new ClientData(PortalSettings.Current.PortalId, userId);
                        if (clientData.Exists)
                        {
                            clientData.Update(ajaxInfo);
                            clientData.Save();
                            return "";
                        }
                    }
                }
                return "";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }


        }

        public static String ProductAdminList(HttpContext context, bool paging = true)
        {
            var settings = NBrightBuyUtils.GetAjaxDictionary(context);
            return ProductAdminList(settings, paging);
        }

        public static String ProductAdminList(Dictionary<string,string> settings, bool paging = true)
        {

            try
            {
                if (NBrightBuyUtils.CheckManagerRights())
                {
                    if (UserController.Instance.GetCurrentUserInfo().UserID <= 0) return "";

                    var strOut = "";

                    if (!settings.ContainsKey("themefolder")) settings.Add("themefolder", "");
                    if (!settings.ContainsKey("razortemplate")) settings.Add("razortemplate", "");
                    if (!settings.ContainsKey("header")) settings.Add("header", "");
                    if (!settings.ContainsKey("body")) settings.Add("body", "");
                    if (!settings.ContainsKey("footer")) settings.Add("footer", "");
                    if (!settings.ContainsKey("filter")) settings.Add("filter", "");
                    if (!settings.ContainsKey("orderby")) settings.Add("orderby", "");
                    if (!settings.ContainsKey("returnlimit")) settings.Add("returnlimit", "0");
                    if (!settings.ContainsKey("pagenumber")) settings.Add("pagenumber", "0");
                    if (!settings.ContainsKey("pagesize")) settings.Add("pagesize", "0");
                    if (!settings.ContainsKey("searchtext")) settings.Add("searchtext", "");
                    if (!settings.ContainsKey("searchcategory")) settings.Add("searchcategory", "");
                    if (!settings.ContainsKey("cascade")) settings.Add("cascade", "False");

                    if (!settings.ContainsKey("portalid")) settings.Add("portalid", PortalSettings.Current.PortalId.ToString("")); // aways make sure we have portalid in settings

                    // select a specific entity data type for the product (used by plugins)
                    if (!settings.ContainsKey("entitytypecode")) settings.Add("entitytypecode", "PRD");
                    if (!settings.ContainsKey("entitytypecodelang")) settings.Add("entitytypecodelang", "PRDLANG");
                    var entitytypecodelang = settings["entitytypecodelang"];
                    var entitytypecode = settings["entitytypecode"];

                    var themeFolder = settings["themefolder"];
                    var header = settings["header"];
                    var body = settings["body"];
                    var footer = settings["footer"];
                    var filter = settings["filter"];
                    var orderby = settings["orderby"];
                    var returnLimit = Convert.ToInt32(settings["returnlimit"]);
                    var pageNumber = Convert.ToInt32(settings["pagenumber"]);
                    var pageSize = Convert.ToInt32(settings["pagesize"]);
                    var cascade = Convert.ToBoolean(settings["cascade"]);
                    var razortemplate = settings["razortemplate"];
                    var portalId = Convert.ToInt32(settings["portalid"]);

                    var searchText = settings["searchtext"];
                    var searchCategory = settings["searchcategory"];

                    if (searchText != "") filter += " and (NB3.[ProductName] like '%" + searchText + "%' or NB3.[ProductRef] like '%" + searchText + "%' or NB3.[Summary] like '%" + searchText + "%' ) ";

                    if (Utils.IsNumeric(searchCategory))
                    {
                        if (orderby == "{bycategoryproduct}") orderby += searchCategory;
                        var objQual = DotNetNuke.Data.DataProvider.Instance().ObjectQualifier;
                        var dbOwner = DotNetNuke.Data.DataProvider.Instance().DatabaseOwner;
                        if (!cascade)
                            filter += " and NB1.[ItemId] in (select parentitemid from " + dbOwner + "[" + objQual + "NBrightBuy] where typecode = 'CATXREF' and XrefItemId = " + searchCategory + ") ";
                        else
                            filter += " and NB1.[ItemId] in (select parentitemid from " + dbOwner + "[" + objQual + "NBrightBuy] where (typecode = 'CATXREF' and XrefItemId = " + searchCategory + ") or (typecode = 'CATCASCADE' and XrefItemId = " + searchCategory + ")) ";
                    }
                    else
                    {
                        if (orderby == "{bycategoryproduct}") orderby = " order by NB3.productname ";
                    }

                    // logic for client list of products
                    if (NBrightBuyUtils.IsClientOnly())
                    {
                        filter += " and NB1.ItemId in (select ParentItemId from dbo.[NBrightBuy] as NBclient where NBclient.TypeCode = 'USERPRDXREF' and NBclient.UserId = " + UserController.Instance.GetCurrentUserInfo().UserID.ToString("") + ") ";
                    }

                    var recordCount = 0;

                    if (themeFolder == "")
                    {
                        themeFolder = StoreSettings.Current.ThemeFolder;
                        if (settings.ContainsKey("themefolder")) themeFolder = settings["themefolder"];
                    }


                    var objCtrl = new NBrightBuyController();

                    if (paging) // get record count for paging
                    {
                        if (pageNumber == 0) pageNumber = 1;
                        if (pageSize == 0) pageSize = 20;

                        // get only entity type required
                        recordCount = objCtrl.GetListCount(PortalSettings.Current.PortalId, -1, entitytypecode, filter, entitytypecodelang, Utils.GetCurrentCulture());

                    }

                    // get selected entitytypecode.
                    var list = objCtrl.GetDataList(PortalSettings.Current.PortalId, -1, entitytypecode, entitytypecodelang, Utils.GetCurrentCulture(), filter, orderby, StoreSettings.Current.DebugMode, "", returnLimit, pageNumber, pageSize, recordCount);

                    var passSettings = settings;
                    foreach (var s in StoreSettings.Current.Settings()) // copy store setting, otherwise we get a byRef assignement
                    {
                        if (passSettings.ContainsKey(s.Key))
                            passSettings[s.Key] = s.Value;
                        else
                            passSettings.Add(s.Key, s.Value);
                    }

                    strOut = NBrightBuyUtils.RazorTemplRenderList(razortemplate, 0, "", list, "/DesktopModules/NBright/NBrightBuy", themeFolder, Utils.GetCurrentCulture(), passSettings);

                    // add paging if needed
                    if (paging && (recordCount > pageSize))
                    {
                        var pg = new NBrightCore.controls.PagingCtrl();
                        strOut += pg.RenderPager(recordCount, pageSize, pageNumber);
                    }

                    return strOut;

                }
                return "";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        public static String MoveProductAdmin(HttpContext context)
        {
            try
            {

                //get uploaded params
                var ajaxInfo = NBrightBuyUtils.GetAjaxFields(context);
                var moveproductid = ajaxInfo.GetXmlPropertyInt("moveproductid");
                var movetoproductid = ajaxInfo.GetXmlPropertyInt("movetoproductid");
                var searchcategory = ajaxInfo.GetXmlPropertyInt("searchcategory");
                if (searchcategory > 0 && movetoproductid > 0 && moveproductid > 0)
                {
                    var objCtrl = new NBrightBuyController();
                    objCtrl.GetListCustom(PortalSettings.Current.PortalId, -1, "NBrightBuy_MoveProductinCateogry", 0, "", searchcategory + ";" + moveproductid + ";" + movetoproductid);
                }

                DataCache.ClearCache();

                return ProductFunctions.ProductAdminList(context);

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        #endregion


    }
}
