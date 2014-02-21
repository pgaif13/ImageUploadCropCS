using System;
using System.Web;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace Tiff
{
    /// <summary>
    /// Functions to manipulate uploaded images (resizing / cropping)
    /// </summary>
    /// 

    public class PhotoUtils
    {

        /// <summary>
        /// Function that manages the process of uploading an image file
        /// </summary>
        /// <param name="postedfile"></param>
        /// <param name="targetfilename"></param>
        /// <param name="folder"></param>
        /// <param name="maxsize"></param>
        /// <param name="hmindimension"></param>
        /// <returns></returns>
        public String UploadImage(HttpPostedFile postedfile, String targetfilename, String folder, int maxsize = 262144, int hmindimension = 0)
        {
            String result = "";
            // if hmindimension=0 use default settings
            if (hmindimension == 0)
            {
                hmindimension = Convert.ToInt32(Resources.AppSettings.uploadheight);
            }                     
            Boolean fileOK = false;
            int fileSize = postedfile.ContentLength;            
            String maxk = ((int)((double)maxsize / 1024)).ToString();
            if  (fileSize > 0 & targetfilename.Length > 0)
            {
                String fileExtension = System.IO.Path.GetExtension(postedfile.FileName).ToLower();
                String[] allowedExtensions = { ".jpg", ".jpeg" };
                for (int i = 0; i <= allowedExtensions.Length - 1; i++)
                {
                    if (allowedExtensions[i].Equals(fileExtension))
                    {
                        fileOK = true;
                    }
                }
                if (fileOK)
                {
                    if (fileSize < maxsize)
                    {
                        try
                        {
                            result = ResizeImageUpload(postedfile.InputStream, targetfilename, folder, hmindimension);
                        }
                        catch(Exception ex)
                        {
                            result = "ERROR: File could not be uploaded <br>" + ex.Message;
                        }
                    }
                    else
                    {
                        result = "ERROR: File is larger than " + maxk + "K. Please upload a smaller image";
                    }
                }
                else
                {
                    result = "ERROR: Cannot accept files of this type.";
                }
            }
            else
            {
                result = "ERROR: Cannot upload photos without valid targetfilename.";
            }
            return result;
        }

        /// <summary>
        /// Function that manages resizing the uploaded image files to a specific target size for standarization
        /// it scales the images based on values in AppSettings file
        /// </summary>
        /// <param name="inputfilestream"></param>
        /// <param name="finalfilename"></param>
        /// <param name="folderpath"></param>
        /// <param name="hmindimension"></param>
        /// <returns></returns>
        public String ResizeImageUpload(Stream inputfilestream, String finalfilename, String folderpath, int hmindimension)
        {
            String result = "";
            // enforce final uploaded images to have a fixed height            
            int newStillWidth, newStillHeight;
            int ori1;
            Image originalimg;
            try
            {
                originalimg = System.Drawing.Image.FromStream(inputfilestream);
                if (originalimg.Width > originalimg.Height)
                {
                    // landscape rules
                    ori1 = originalimg.Height;
                    newStillHeight = hmindimension;
                    newStillWidth = (int)((double)originalimg.Width * hmindimension / ori1);
                }
                else
                {
                    // portrait rules
                     ori1 = originalimg.Width;
                    newStillHeight = hmindimension;
                    newStillWidth = (int)((double)newStillHeight * originalimg.Width / originalimg.Height);
                }
                Bitmap still = new Bitmap(newStillWidth, newStillHeight);
                Graphics gr_dest_still = Graphics.FromImage(still);
                SolidBrush sb = new SolidBrush(System.Drawing.Color.White);
                gr_dest_still.FillRectangle(sb, 0, 0, still.Width, still.Height);
                gr_dest_still.DrawImage(originalimg, 0, 0, still.Width, still.Height);
                try
                {
                    ImageCodecInfo codecencoder = GetEncoder("image/jpeg");
                    int quality = 90;
                    EncoderParameters encodeparams = new EncoderParameters(1);
                    EncoderParameter qualityparam = new EncoderParameter(Encoder.Quality, quality);
                    encodeparams.Param[0] = qualityparam;
                    still.SetResolution(96, 96);
                    if (!folderpath.EndsWith("\\")){
                        folderpath += "\\";
                    }
                    still.Save(folderpath  + finalfilename, codecencoder, encodeparams);
                    result = "OK: File uploaded!";
                }
                catch(Exception ex)
                {
                    result = "ERROR: there was a problem saving the image. " + ex.Message;
                }
                if (still!=null)
                {
                    still.Dispose();                    
                }    
            }
            catch(Exception ex)
            {
                result = "ERROR: that was not an image we could process. " + ex.Message;
            }
            return result;
        }

        /// <summary>
        /// Function that handles cropping the images after uploading
        /// images are cropped/scaled to dimensions specified in the AppSettings file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="sourcefolder"></param>
        /// <param name="targetfolder"></param>
        /// <param name="imgfolder"></param>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="W"></param>
        /// <param name="H"></param>
        /// <returns></returns>
        public String CropImage(String filename, String sourcefolder, String targetfolder, String imgfolder, int X, int Y, int W, int H)
        {
            String result = "";            
            // enforce final cropped images to have fixed dimensions
            int croppedfinalw, croppedfinalh;
            croppedfinalh = Convert.ToInt32(Resources.AppSettings.passheight);
            croppedfinalw = Convert.ToInt32(Resources.AppSettings.passwidth);
            try
            {
                if (!imgfolder.EndsWith("\\"))
                {
                    imgfolder += "\\";
                }
                String sourcepath = imgfolder + sourcefolder + "\\";
                Bitmap image1 = (Bitmap)Image.FromFile(sourcepath + filename, true);
                Rectangle rect = new Rectangle(X, Y, W, H);
                Bitmap cropped = image1.Clone(rect, image1.PixelFormat);
                // dispose original image in case we need to overwrite it below
                if (image1 != null)
                {
                    image1.Dispose();                    
                }    
                Bitmap finalcropped= new Bitmap(croppedfinalw, croppedfinalh);
                Graphics gr_finalcropped  = Graphics.FromImage(finalcropped);
                SolidBrush sb = new SolidBrush(System.Drawing.Color.White);
                gr_finalcropped.FillRectangle(sb, 0, 0, finalcropped.Width, finalcropped.Height);
                gr_finalcropped.DrawImage(cropped, 0, 0, finalcropped.Width, finalcropped.Height);
                try
                {
                    ImageCodecInfo codecencoder  = GetEncoder("image/jpeg");
                    int quality = 92;
                    EncoderParameters encodeparams  = new EncoderParameters(1);
                    EncoderParameter qualityparam = new EncoderParameter(Encoder.Quality, quality);
                    encodeparams.Param[0] = qualityparam;
                    finalcropped.SetResolution(240, 240);
                    sourcepath = sourcepath.Replace(sourcefolder, targetfolder);
                    finalcropped.Save(sourcepath + filename, codecencoder, encodeparams);
                    result = "OK - File cropped";
                }
                catch(Exception ex)
                {
                    result = "ERROR: there was a problem saving the image. " + ex.Message;
                }
                if (cropped != null)
                {
                    cropped.Dispose();                    
                }
                if (finalcropped != null)
                {
                    finalcropped.Dispose();                    
                }
            }
            catch(Exception ex)
            {
                result = "ERROR: that was not an image we could process. " + ex.Message;
            }
            return result;
        }

        public ImageCodecInfo GetEncoder(String mimetype)
        {
            ImageCodecInfo result = null;
            foreach (ImageCodecInfo codec in ImageCodecInfo.GetImageEncoders())
            {
                if (codec.MimeType == mimetype)
                {
                    result = codec;
                }
            }
            return result;
        }

        public int CalculateResizedWidth(String filename, String folderpath, int newh)
        {
            int result = 0;
            if (!folderpath.EndsWith("\\"))
            {
                folderpath += "\\";
            }
            String fullpath = folderpath + filename;
            Bitmap image1 = (Bitmap)Image.FromFile(fullpath, true);
            if (image1 != null)
            {
                result = (int)((double)newh * image1.Width / image1.Height);
                image1.Dispose();
            }
            return result;
        }

    }   
    
}


