using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Drawing;

namespace WinForms_RTSP_Player.Utilities
{
    /// <summary>
    /// Extension methods for OpenCV Mat to Bitmap conversion
    /// </summary>
    public static class MatExtensions
    {
        /// <summary>
        /// Convert OpenCV Mat to System.Drawing.Bitmap (thread-safe)
        /// </summary>
        public static Bitmap? ToBitmap(this Mat mat)
        {
            if (mat == null || mat.Empty())
                return null;

            try
            {
                return BitmapConverter.ToBitmap(mat);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Resize Mat to fit target size while maintaining aspect ratio
        /// </summary>
        public static Mat ResizeToFit(this Mat mat, int targetWidth, int targetHeight)
        {
            if (mat == null || mat.Empty())
                return mat;

            double aspectRatio = (double)mat.Width / mat.Height;
            int newWidth, newHeight;

            if (mat.Width > mat.Height)
            {
                newWidth = targetWidth;
                newHeight = (int)(targetWidth / aspectRatio);
            }
            else
            {
                newHeight = targetHeight;
                newWidth = (int)(targetHeight * aspectRatio);
            }

            Mat resized = new Mat();
            Cv2.Resize(mat, resized, new OpenCvSharp.Size(newWidth, newHeight));
            return resized;
        }
    }
}
