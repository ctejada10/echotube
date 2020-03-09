using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EchoTube.Devices
{
    #region Static Class 'SettingsFileHandler'
    internal static class SettingsFileHandler
    {
        #region Static Fields
        public static readonly NumberFormatInfo Provider = new NumberFormatInfo
        {
            NumberDecimalSeparator = "."
        };
        #endregion

        #region Static Loading
        internal static EchoTubeSettings LoadSettings(string fileName)
        {
            EchoTubeSettings settings = new EchoTubeSettings();

            FileStream strm = new FileStream(fileName, FileMode.Open);
            StreamReader reader = new StreamReader(strm);

            // read the first three lines
            reader.ReadLine();
            reader.ReadLine();
            reader.ReadLine();

            // read the values
            settings.StartCutOff = Convert.ToInt32(reader.ReadLine().Split(
                new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries)[1]);
            settings.EndCutOff = Convert.ToInt32(reader.ReadLine().Split(
                new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries)[1]);
            settings.Alpha = Convert.ToSingle(reader.ReadLine().Split(
                new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries)[1], Provider);
            settings.UpperTouchThreshold = Convert.ToInt32(reader.ReadLine().Split(
                new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries)[1]);
            settings.LowerTouchThreshold = Convert.ToInt32(reader.ReadLine().Split(
                new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries)[1]);
            settings.SingleTouch = Convert.ToInt32(reader.ReadLine().Split(
                new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries)[1]) == 1;

            reader.Close();
            reader.Dispose();
            reader = null;

            strm.Close();
            strm.Dispose();
            strm = null;

            return settings;
        }
        #endregion

        #region Static Saving
        internal static void SaveSettings(string fileName, EchoTubeSettings settings)
        {
            FileStream strm = new FileStream(fileName, FileMode.Create);
            StreamWriter writer = new StreamWriter(strm);

            writer.WriteLine("EchoTube Settings");
            writer.WriteLine("Created: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
            writer.WriteLine("------------------------------------------------------------");
            writer.WriteLine("StartCutOff=" + settings.StartCutOff);
            writer.WriteLine("EndCutOff=" + settings.EndCutOff);
            writer.WriteLine("Alpha=" + settings.Alpha.ToString(Provider));
            writer.WriteLine("UpperThreshold=" + settings.UpperTouchThreshold);
            writer.WriteLine("LowerThreshold=" + settings.LowerTouchThreshold);
            writer.WriteLine("TouchPoints=" + (settings.SingleTouch ? 1 : 2));

            writer.Close();
            writer.Dispose();
            writer = null;

            strm.Close();
            strm.Dispose();
            strm = null;
        }
        #endregion
    }
    #endregion
}
