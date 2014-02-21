using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Tiff;

public partial class _Default : System.Web.UI.Page
{
        
    int hminRaw = Convert.ToInt32(Resources.AppSettings.uploadheight);
    int imgUploadPreview = Convert.ToInt32(Resources.AppSettings.uploadpreviewh);
    int maxSize = Convert.ToInt32(Resources.AppSettings.maxuploadsize);
    int hminCropped = Convert.ToInt32(Resources.AppSettings.passheight);
    int wminCropped = Convert.ToInt32(Resources.AppSettings.passwidth);
    int prevw = Convert.ToInt32(Resources.AppSettings.previewwidth);
    int prevh = Convert.ToInt32(Resources.AppSettings.previewheight);    
    PhotoUtils myphotoutils = new PhotoUtils();

    protected void Page_Load(Object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            preview.Width =  new Unit(prevw);
            preview.Height =  new Unit(prevh);
            previewdiv.Attributes["style"] = previewdiv.Attributes["style"].Replace("120", prevw.ToString()).Replace("160", prevh.ToString());
        }
    }

    /// <summary>
    /// Handle image upload
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void UploadPhoto(Object sender, EventArgs e)
    {
        errorLiteral.Text = "";
        String path  = Server.MapPath("~/uploaded_images/raw/");        
        int fileSize = FileUpload1.PostedFile.ContentLength;
        String testfilename = "image_upload_test.jpg";
        String resultstr = "";        
        if (FileUpload1.HasFile)
        {
            String opresult = myphotoutils.UploadImage(FileUpload1.PostedFile, testfilename, path, maxSize, hminRaw);
            resultstr = opresult;
            if (opresult == "OK: File uploaded!")
            {
                imagenameLiteral.Text = testfilename;
                ViewState["imagename"] = testfilename;
                Image1.ImageUrl = "~/uploaded_images/raw/" + testfilename ;
                Image1.Height = new Unit(imgUploadPreview);
                Image1.Width = myphotoutils.CalculateResizedWidth((String)ViewState["imagename"], path, imgUploadPreview);
                preview.ImageUrl = "~/uploaded_images/raw/" + testfilename;
                
                int jsWidth = myphotoutils.CalculateResizedWidth(ViewState["imagename"].ToString(), path, hminRaw);
                
                UpdatePreviewJsWH(jsWidth,hminRaw, path, testfilename);
                croppedpreviewLiteral.Visible = true;
            }
            else
            {
                resultstr = opresult;
                errorLiteral.Text = resultstr;
                croppedpreviewLiteral.Visible = false;
            }
        }
        else
        {
            resultstr = "ERROR: Cannot upload photos without valid image file.";
            errorLiteral.Text = resultstr;
        }
    }

    /// <summary>
    /// Hanlde image cropping
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void btnCrop_Click(Object sender, EventArgs e)
    {
        String path = Server.MapPath("~/uploaded_images/");
        String sourcefolder = "raw";
        String targetfolder = "cropped";
        if (Image1.ImageUrl.Contains("cropped"))
        {
            sourcefolder = "cropped";            
        }
        String myimagename = ViewState["imagename"].ToString();
        string opres = myphotoutils.CropImage(myimagename, sourcefolder, targetfolder, path, (int)Double.Parse(X1value.Value), (int)Double.Parse(Y1value.Value), (int)Double.Parse(Wvalue.Value), (int)Double.Parse(Hvalue.Value));
        if (opres.StartsWith("OK"))
        {
            Image1.ImageUrl = "~/uploaded_images/cropped/" + myimagename;
            Image1.Height = new Unit(imgUploadPreview);
            Image1.Width = myphotoutils.CalculateResizedWidth(myimagename, Server.MapPath("~/uploaded_images/cropped/"), imgUploadPreview);
            preview.ImageUrl = "~/uploaded_images/cropped/" + myimagename;
            // update JS paramaters for refresh function
            UpdatePreviewJsWH(wminCropped, hminCropped, path.Replace("raw", "cropped"), myimagename);
        }
        else
        {
            errorLiteral.Text = opres;
        }
    }

    /// <summary>
    /// Handle javascript markup based on the dimensions of the image being cropped
    /// </summary>
    /// <param name="neww"></param>
    /// <param name="newh"></param>
    /// <param name="folderpath"></param>
    /// <param name="filename"></param>
    void UpdatePreviewJsWH(int neww, int newh, String folderpath, String filename)
    {
        // change width based on source image
        int startselect = jcropLiteral.Text.IndexOf("width: Math.round");
        int startvalue = jcropLiteral.Text.IndexOf("(", startselect);
        int endvalue = jcropLiteral.Text.IndexOf(")", startvalue);
        String selectvalue = jcropLiteral.Text.Substring(startvalue, endvalue - startvalue + 1);
        String newvalue = "(rx*" + neww.ToString() + ")";
        jcropLiteral.Text = jcropLiteral.Text.Replace(selectvalue, newvalue);
        // change height based on source image
        startselect = jcropLiteral.Text.IndexOf("height: Math.round");
        startvalue = jcropLiteral.Text.IndexOf("(", startselect);
        endvalue = jcropLiteral.Text.IndexOf(")", startvalue);
        selectvalue = jcropLiteral.Text.Substring(startvalue, endvalue - startvalue + 1);
        newvalue = "(ry*" + newh.ToString() + ")";
        jcropLiteral.Text = jcropLiteral.Text.Replace(selectvalue, newvalue);
        // Configure Explicit Resizing 
        startselect = jcropLiteral.Text.IndexOf("trueSize:");
        startvalue = jcropLiteral.Text.IndexOf("[", startselect);
        endvalue = jcropLiteral.Text.IndexOf("]", startvalue);
        selectvalue = jcropLiteral.Text.Substring(startvalue, endvalue - startvalue + 1);
        newvalue = "[" + neww.ToString() + ", " + newh.ToString() + "]";
        jcropLiteral.Text = jcropLiteral.Text.Replace(selectvalue, newvalue);  
        // determine fixed image ratio from Appsettings
        String ratio  = Resources.AppSettings.passwidth + "/" + Resources.AppSettings.passheight;
        startselect = jcropLiteral.Text.IndexOf("aspectRatio:");
        startvalue = jcropLiteral.Text.IndexOf(" ", startselect);
        endvalue = jcropLiteral.Text.IndexOf(",", startvalue);
        selectvalue = jcropLiteral.Text.Substring(startvalue, endvalue - startvalue + 1);
        newvalue = " " + ratio + ",";
        jcropLiteral.Text = jcropLiteral.Text.Replace(selectvalue, newvalue);
        //configure default values for rx     
        startselect = jcropLiteral.Text.IndexOf("var rx");
        startvalue = jcropLiteral.Text.IndexOf("=", startselect);
        endvalue = jcropLiteral.Text.IndexOf(";", startvalue);
        selectvalue = jcropLiteral.Text.Substring(startvalue, endvalue - startvalue + 1);
        newvalue = "= " + Resources.AppSettings.previewwidth + " / c.w;";
        jcropLiteral.Text = jcropLiteral.Text.Replace(selectvalue, newvalue);
        //configure default values for ry 
        startselect = jcropLiteral.Text.IndexOf("var ry ");
        startvalue = jcropLiteral.Text.IndexOf("=", startselect);
        endvalue = jcropLiteral.Text.IndexOf(";", startvalue);
        selectvalue = jcropLiteral.Text.Substring(startvalue, endvalue - startvalue + 1);
        newvalue = "= " + Resources.AppSettings.previewheight + " / c.h;";
        jcropLiteral.Text = jcropLiteral.Text.Replace(selectvalue, newvalue);        
    }

}