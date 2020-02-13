using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SMFGC {
    class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 
        static MySqlConnection conn = new MySqlConnection(pVariables.sConn);

        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Login());
        }

        public static void sysLog(int dev_id, string uid, string process, string message, int alert) {
            //Error   16
            //The message box contains a symbol consisting of white X in a circle with a red background.

            //Information     64
            //The message box contains a symbol consisting of a lowercase letter i in a circle.

            //None    0
            //The message box contains no symbols.

            //Question    32
            //The message box contains a symbol consisting of a question mark in a circle. The question mark message icon is no longer recommended because it does not clearly represent a specific type of message and because the phrasing of a message as a question could apply to any message type.In addition, users can confuse the question mark symbol with a help information symbol.Therefore, do not use this question mark symbol in your message boxes.The system continues to support its inclusion only for backward compatibility.

            //Warning     48
            //The message box contains a symbol consisting of an exclamation point in a triangle with a yellow background.


            if (conn != null && conn.State == ConnectionState.Open) conn.Close();

            MySqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = pVariables.qLogger;
            cmd.Parameters.Add("@p1", MySqlDbType.Int32).Value = dev_id;
            cmd.Parameters.Add("@p2", MySqlDbType.VarChar).Value = uid;
            cmd.Parameters.Add("@p3", MySqlDbType.VarChar).Value = process;
            cmd.Parameters.Add("@p4", MySqlDbType.VarChar).Value = alert;
            cmd.Parameters.Add("@p5", MySqlDbType.VarChar).Value = message;
            conn.Open();
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        public static Bitmap ResizeImage(Image image, int width, int height) {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage)) {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes()) {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public static byte[] imageToByteArray(System.Drawing.Image imageIn) {
            MemoryStream ms = new MemoryStream();
            imageIn.Save(ms, ImageFormat.Jpeg);
            return ms.ToArray();
        }

        public static Image byteArrayToImage(byte[] byteArrayIn) {
            MemoryStream ms = new MemoryStream(byteArrayIn);
            Image returnImage = Image.FromStream(ms);
            return returnImage;
        }
    }
}
