﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BaseCommon.DBModuleTable.DBModule.Table;
using GovernmentInfoStudio.ActionManager;
using System.IO;
using System.Text.RegularExpressions;
using LinqToExcel;
using Aspose.Words;
using System.Threading;

namespace GovernmentInfoStudio
{
    public partial class FrmMain :  DevExpress.XtraEditors.XtraForm
    {
        public FrmMain()
        {
            InitializeComponent();
        }

        List<TblDepartment> departList = new List<TblDepartment>();
        List<TblAdministrativeCategory> categoryList = new List<TblAdministrativeCategory>();
        List<TreeMainData> treeDataList = new List<TreeMainData>();

        void InitControl()
        {
            c_grcMain_view_DepartmentID.FieldName = "DepartmentID";
            c_grcMain_view_DepartmentName.FieldName = "DepartmentName";
            c_grcMain_view_DepartmentFullName.FieldName = "DepartFullName";
            c_grcMain_view_DepartmentProcess.FieldName = "DepartmentProcess";

            c_grcMain.DataSource = departList;

            c_trlMain.ParentFieldName = "TreeDataCode";
            c_trlMain.KeyFieldName = "TreeDataID";

            c_trlMain_DepartmentName.FieldName = "DepartmentName";
            c_trlMain_CategoryName.FieldName = "CategoryName";
            c_trlMain_AuthorityMatteryName.FieldName = "AuthorityMatteryName";
            c_trlMain_AuthorityDetailName.FieldName = "AuthorityDetailName";
            c_trlMain_AuthorityFullName.FieldName = "CategoryFileName";
            c_trlMain_AuthorityMatteryCode.FieldName = "AuthorityMatteryCode";

            c_trlMain.DataSource = treeDataList;
        }

        void LoadData() 
        {
            string errMsg = string.Empty;
            DepartmentMng.GetList(ref departList, ref errMsg);
            c_grcMain.DataSource = departList;

            DepartmentMng.GetList(ref categoryList, ref errMsg);
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            InitControl();
            LoadData();
        }

        private void buttonEdit1_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择文件路径";
            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            buttonEdit2.Text = dialog.SelectedPath;
        }

        List<TblDepartment> ReadDepaet(string path)
        {
            var DepartmentList = new List<TblDepartment>();

            try
            {

                var dictory = Directory.GetDirectories(path);

                var regex1 = new Regex(@"\d+[.]{0,}(.*?)[-,－,—,_,（,(]");

                string departName = string.Empty;

                #region     读取部门

                foreach (var item in dictory)
                {
                    departName = item.Replace(path, "").Replace(@"\", "");

                    if (regex1.IsMatch(departName))
                    {
                        departName = regex1.Match(departName).Groups[1].Value;
                    }
                    else
                    {
                        //未匹配到部门
                    }

                    TblDepartment depart = new TblDepartment();
                    
                    depart.DepartmentName = departName;
                    depart.DepartmentSortID = depart.DepartmentID;
                    depart.DepartFullName = item;

                    DepartmentList.Add(depart);
                }
                #endregion
            }
            catch (Exception exception)
            {

            }

            return DepartmentList;
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(buttonEdit2.Text))
            {
                return;
            }

            //读取部门
            var dataList = ReadDepaet(buttonEdit2.Text);

            progressBar1.Maximum = dataList.Count;

            foreach (var item in dataList)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback((obj) =>
                {
                    var departItme = obj as TblDepartment;

                    if (departItme == null)
                    {
                        return;
                    }

                    var depart = departList.Find(c => c.DepartmentName == departItme.DepartmentName);

                    if (depart == null)
                    {
                        DepartmentMng.Insert(departItme);
                        departList.Add(departItme);
                    }
                    else
                    {
                        depart.DepartFullName = departItme.DepartFullName;
                    }

                    this.Invoke(new Action(() =>
                    {
                        c_grcMain.RefreshDataSource();

                        progressBar1.Value++;

                        //if (progressBar1.Value >= progressBar1.Maximum)
                        //{
                        //    progressBar1.Value = 0;
                        //}
                    }));
                }), item);
            }
        }

        TblDepartment focusedRowDepartment = new TblDepartment();

        private void repositoryItemButtonEdit1_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            treeDataList = new List<TreeMainData>();
            c_trlMain.DataSource = treeDataList;

            focusedRowDepartment = (TblDepartment)c_grcMain_View.GetFocusedRow();

            //读取部门分类
            var category = ReadAdministrativeCategory(focusedRowDepartment);

            var departCategory = new List<TblDepartment_AdministrativeCategory>();

            string errMsg = string.Empty;

            DepartmentMng.GetList(focusedRowDepartment, ref departCategory, ref errMsg);

            int treeID = 0;

            var departCateBach = new List<TblDepartment_AdministrativeCategory>();

            foreach (var item in category)
            {
                var depart = categoryList.Find(c => c.AdministrativeCategoryName == item.AdministrativeCategoryName);

                if (depart == null)
                {
                    DepartmentMng.Insert(item);
                    categoryList.Add(item);
                }

                var departCateTemp = departCategory.Find(c => c.DepartmentID == focusedRowDepartment.DepartmentID && c.AdministrativeCategoryID == depart.AdministrativeCategoryID);

                if (departCateTemp == null)
                {
                    departCateBach.Add(new TblDepartment_AdministrativeCategory()
                    {
                        DepartmentID = focusedRowDepartment.DepartmentID,
                        AdministrativeCategoryID = depart.AdministrativeCategoryID
                    });
                }

                var treeData = new TreeMainData();

                treeData.TreeDataID = treeID;
                treeData.TreeDataCode = treeID;
                treeData.Department = focusedRowDepartment;
                treeData.DepartmentName = focusedRowDepartment.DepartmentName;
                treeData.Category = depart;
                treeData.CategoryName = depart.AdministrativeCategoryName;
                treeData.CategoryFileName = item.CategoryFileName;
                treeData.AuthorityFullName = item.CategoryFullName;

                treeDataList.Add(treeData);

                c_trlMain.RefreshDataSource();

                treeID++;
            }

            if (departCateBach.Count > 0)
            {
                DepartmentMng.InsertBath(departCateBach);
            }
        }

        List<TblAdministrativeCategory> ReadAdministrativeCategory(TblDepartment depart)
        {
            var category = new List<TblAdministrativeCategory>();

            try
            {
                var dictory = Directory.GetDirectories(depart.DepartFullName);

                var regex2 = new Regex(@"\d+[.]{0,}(.*?)[-,－,—,_,（,(]");

                string categoryName = string.Empty;

                #region 读取分类

                foreach (var item in dictory)
                {
                    FileInfo file = new FileInfo(item);

                    categoryName = file.Name;

                    if (regex2.IsMatch(categoryName))
                    {
                        categoryName = regex2.Match(categoryName).Groups[1].Value;
                    }
                    else
                    {
                        //未匹配到的分类
                    }

                    if (string.IsNullOrEmpty(categoryName))
                    {
                        continue;
                    }

                    TblAdministrativeCategory admincategory = new TblAdministrativeCategory();

                    admincategory.AdministrativeCategoryName = categoryName;
                    admincategory.Department = depart;
                    admincategory.CategoryFullName = file.FullName;
                    admincategory.CategoryFileName = file.Name;
  
                    category.Add(admincategory);
                }

                #endregion
            }
            catch (Exception exception)
            {

            }

            return category;
        }

        private void repositoryItemButtonEdit2_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {

        }

        private void c_trlMain_DoubleClick(object sender, EventArgs e)
        {
            TreeMainData focusedRow = (TreeMainData)c_trlMain.GetDataRecordByNode(c_trlMain.FocusedNode);

            if (focusedRow.TreeDataID == focusedRow.TreeDataCode &&
                focusedRow.AuthorityMatteryDetail == null)
            {
                var Mattery = ReadAuthorityMattery(focusedRow.Department.DepartmentID, focusedRow.Category.AdministrativeCategorySortID, focusedRow.AuthorityFullName);

                if (Mattery.AuthorityMatteryDetailList.Count <= 0)
                {
                    return;
                }

                focusedRow.AuthorityMatteryDetail = Mattery.AuthorityMatteryDetailList[0];
                focusedRow.AuthorityMatteryCode = Mattery.AuthorityMatteryDetailList[0].AuthorityCode;
                focusedRow.AuthorityMatteryName = Mattery.AuthorityMatteryName;
                focusedRow.AuthorityDetailName = (Mattery.AuthorityMatteryDetailList.Count <= 1 ? "无" : Mattery.AuthorityMatteryDetailList.Count.ToString()) + "子项";

                if (Mattery.AuthorityMatteryDetailList.Count > 1)
                {
                    foreach (var item in Mattery.AuthorityMatteryDetailList)
                    {
                        var treeData = new TreeMainData();

                        treeData.TreeDataID = treeDataList.Count + 1;
                        treeData.TreeDataCode = focusedRow.TreeDataID;
                        treeData.Department = focusedRow.Department;
                        treeData.DepartmentName = focusedRow.DepartmentName;
                        treeData.Category = focusedRow.Category;
                        treeData.CategoryName = focusedRow.Category.AdministrativeCategoryName;
                        treeData.CategoryFileName = item.MatteryPath;
                        treeData.AuthorityMatteryCode = item.AuthorityCode;
                        treeData.AuthorityMatteryName = item.AuthorityName;
                        treeData.AuthorityMatteryDetail = item;
                        treeData.AuthorityDetailName = "无子项";

                        treeDataList.Add(treeData);

                        c_trlMain.RefreshDataSource();
                    }
                }
            }

            ShowAuthorityMattery(focusedRow.AuthorityMatteryDetail);

            c_trlMain.RefreshDataSource();
        }

        void ShowAuthorityMattery(TblAuthorityMatteryDetail detailValue) 
        {
            try
            {
                if (detailValue == null)
                {
                    return;
                }

                if (detailValue.AuthorityDetailList!=null)
                {
                    memoEdit1.Text = "";
                    foreach (var item in detailValue.AuthorityDetailList)
                    {
                        memoEdit1.Text += item.AuthorityMatteryTitle + ":" + item.AuthorityMatteryContent + "\r\n";
                    }
                }

                if (detailValue.AuthorityMatteryFlow != null)
                {
                    pictureBox1.Image = Image.FromFile(detailValue.AuthorityMatteryFlow.FlowImagePath);
                }
                else
                {
                    pictureBox1.Image = null;
                }
            }
            catch (Exception exception)
            {
                
            }
        }

        TblAuthorityMattery ReadAuthorityMattery(int DepartmentID, int CategoryID, string CategoryFullName)
        {
            var authoryMatt = new TblAuthorityMattery();

            try
            {
                FileInfo fileInfo = new FileInfo(CategoryFullName);

                string title = fileInfo.Name;

                var titleRegex = new Regex("[—,-]{1,}(.*)");

                if (titleRegex.IsMatch(title))
                {
                    title = titleRegex.Match(title).Groups[1].Value;
                }
                else
                {

                }

                authoryMatt.AuthorityMatteryName = title;
                authoryMatt.DepartmentID = DepartmentID;
                authoryMatt.AdministrativeCategoryID = CategoryID;

                var excels = Directory.GetFiles(CategoryFullName, "*.xls");

                authoryMatt.AuthorityMatteryDetailList = new List<TblAuthorityMatteryDetail>();

                #region 子项

                foreach (var item in excels)
                {
                    var excel = new ExcelQueryFactory(item);

                    var excelRows = excel.WorksheetNoHeader(0);

                    var rowCount = excelRows.Count();

                    if (rowCount <= 0)
                    {
                        continue;
                    }

                    fileInfo = new FileInfo(item);

                    var rows = excelRows.ToList();

                    var detail = new TblAuthorityMatteryDetail();
                    detail.AuthorityDetailList = new List<TblAuthorityDetail>();
                   
                    #region 子项明细

                    foreach (var itemrow in rows)
                    {
                        if (itemrow.Count() < 2)
                        {
                            continue;
                        }

                        var authdetail = new TblAuthorityDetail();

                        authdetail.AuthorityMatteryTitle = itemrow[0].Value.ToString().Trim();

                        if (rowCount == 1)
                        {
                            detail.AuthorityDetailList.Add(authdetail);
                            continue;
                        }

                        authdetail.AuthorityMatteryContent = itemrow[1].Value.ToString();

                        if (string.IsNullOrEmpty(authdetail.AuthorityMatteryTitle) && string.IsNullOrEmpty(authdetail.AuthorityMatteryContent))
                        {
                            continue;
                        }

                        detail.AuthorityDetailList.Add(authdetail);
                    }

                    #endregion

                    if (detail.AuthorityDetailList.FindIndex(c => c.AuthorityMatteryTitle == "职权编码") > 0)
                    {
                        detail.AuthorityCode = detail.AuthorityDetailList.Find(c => c.AuthorityMatteryTitle == "职权编码").AuthorityMatteryContent.Trim();
                    }
                    
                    detail.AuthorityName = fileInfo.Name.Replace(".xls", "");
                    detail.MatteryPath = fileInfo.FullName;

                    #region 子项流程图

                    string imagePath = string.Empty;
                    detail.AuthorityMatteryFlow = new TblAuthorityMatteryFlow();

                    var docPath = fileInfo.FullName.Replace(".xls", ".doc");

                    try
                    {
                        #region 存在相同文件名Word

                        GetDoc:

                        if (File.Exists(docPath))
                        {
                            imagePath = docPath.Replace("doc", "jpg");

                            if (File.Exists(imagePath))
                            {
                                File.Delete(imagePath);
                            }
                          
                            new Aspose.Words.License().SetLicense(new MemoryStream(Convert.FromBase64String(Key)));

                            Document doc = new Document(docPath);

                            using (MemoryStream stream = new MemoryStream())
                            {
                                doc.Save(stream, SaveFormat.Jpeg);

                                detail.AuthorityMatteryFlow.AuthorityMatteryFlowImage = stream.GetBuffer();

                                using (System.Drawing.Image image = Bitmap.FromStream(stream)) // 原始图
                                {
                                    detail.AuthorityMatteryFlow.AuthorityFlowImage = image;

                                    using (Bitmap image2 = new Bitmap(image))
                                    {
                                        image2.Save(imagePath);
                                    }

                                    detail.AuthorityMatteryFlow.FlowImagePath = imagePath;
                                }
                            }
                        }

                        #endregion

                        #region
                        else
                        {
                            DirectoryInfo directoryInfo = new DirectoryInfo(fileInfo.Directory.FullName);

                            var docs = directoryInfo.GetFiles("*.doc");

                            if (docs.Length>0)
                            {
                                docPath = docs[0].FullName;
                                goto GetDoc;
                            }
                        }
                        #endregion
                    }
                    catch (Exception exception)
                    {
                        detail.AuthorityMatteryFlow.FlowImagePath = imagePath;
                    }

                    detail.MatteryFlowPath = docPath;

                    #endregion
                    
                    authoryMatt.AuthorityMatteryDetailList.Add(detail);
                }

                #endregion
            }
            catch (Exception exception)
            {

            }

            return authoryMatt;
        }

        private void simpleButton4_Click(object sender, EventArgs e)
        {
            FrmDBConnect frmDb = new FrmDBConnect();
            frmDb.ShowDialog();
        }

        #region key

        public const string Key =
            "PExpY2Vuc2U+DQogIDxEYXRhPg0KICAgIDxMaWNlbnNlZFRvPkFzcG9zZSBTY290bGFuZCB" +
            "UZWFtPC9MaWNlbnNlZFRvPg0KICAgIDxFbWFpbFRvPmJpbGx5Lmx1bmRpZUBhc3Bvc2UuY2" +
            "9tPC9FbWFpbFRvPg0KICAgIDxMaWNlbnNlVHlwZT5EZXZlbG9wZXIgT0VNPC9MaWNlbnNlV" +
            "HlwZT4NCiAgICA8TGljZW5zZU5vdGU+TGltaXRlZCB0byAxIGRldmVsb3BlciwgdW5saW1p" +
            "dGVkIHBoeXNpY2FsIGxvY2F0aW9uczwvTGljZW5zZU5vdGU+DQogICAgPE9yZGVySUQ+MTQ" +
            "wNDA4MDUyMzI0PC9PcmRlcklEPg0KICAgIDxVc2VySUQ+OTQyMzY8L1VzZXJJRD4NCiAgIC" +
            "A8T0VNPlRoaXMgaXMgYSByZWRpc3RyaWJ1dGFibGUgbGljZW5zZTwvT0VNPg0KICAgIDxQc" +
            "m9kdWN0cz4NCiAgICAgIDxQcm9kdWN0PkFzcG9zZS5Ub3RhbCBmb3IgLk5FVDwvUHJvZHVj" +
            "dD4NCiAgICA8L1Byb2R1Y3RzPg0KICAgIDxFZGl0aW9uVHlwZT5FbnRlcnByaXNlPC9FZGl" +
            "0aW9uVHlwZT4NCiAgICA8U2VyaWFsTnVtYmVyPjlhNTk1NDdjLTQxZjAtNDI4Yi1iYTcyLT" +
            "djNDM2OGYxNTFkNzwvU2VyaWFsTnVtYmVyPg0KICAgIDxTdWJzY3JpcHRpb25FeHBpcnk+M" +
            "jAxNTEyMzE8L1N1YnNjcmlwdGlvbkV4cGlyeT4NCiAgICA8TGljZW5zZVZlcnNpb24+My4w" +
            "PC9MaWNlbnNlVmVyc2lvbj4NCiAgICA8TGljZW5zZUluc3RydWN0aW9ucz5odHRwOi8vd3d" +
            "3LmFzcG9zZS5jb20vY29ycG9yYXRlL3B1cmNoYXNlL2xpY2Vuc2UtaW5zdHJ1Y3Rpb25zLm" +
            "FzcHg8L0xpY2Vuc2VJbnN0cnVjdGlvbnM+DQogIDwvRGF0YT4NCiAgPFNpZ25hdHVyZT5GT" +
            "zNQSHNibGdEdDhGNTlzTVQxbDFhbXlpOXFrMlY2RThkUWtJUDdMZFRKU3hEaWJORUZ1MXpP" +
            "aW5RYnFGZkt2L3J1dHR2Y3hvUk9rYzF0VWUwRHRPNmNQMVpmNkowVmVtZ1NZOGkvTFpFQ1R" +
            "Hc3pScUpWUVJaME1vVm5CaHVQQUprNWVsaTdmaFZjRjhoV2QzRTRYUTNMemZtSkN1YWoyTk" +
            "V0ZVJpNUhyZmc9PC9TaWduYXR1cmU+DQo8L0xpY2Vuc2U+";

        #endregion

        private class TreeMainData
        {
            public int TreeDataID { get; set; }

            public int TreeDataCode { get; set; }

            public TblDepartment Department { get; set; }

            public string DepartmentName { get; set; }

            public TblAdministrativeCategory Category { get; set; }

            public string CategoryName { get; set; }

            public string AuthorityMatteryCode { get; set; }

            public string AuthorityMatteryName { get; set; }

            public string AuthorityFullName { get; set; }

            public string CategoryFileName { get; set; }

            public string AuthorityDetailName { get; set; }

            public bool IsLoad { get; set; }
            public TblAuthorityMatteryDetail AuthorityMatteryDetail { get; set; }
        }

        private void labelControl3_Click(object sender, EventArgs e)
        {

        }

    }
}