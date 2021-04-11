using Decal.Adapter;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace QuestHelper
{
	public static class Util
	{
		public static void LogError(Exception ex)
		{
			try
			{
				using (StreamWriter writer = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\Asheron's Call\" + Globals.PluginName + " errors.txt", true))
				{
					writer.WriteLine("============================================================================");
					writer.WriteLine(DateTime.Now.ToString());
					writer.WriteLine("Error: " + ex.Message);
					writer.WriteLine("Source: " + ex.Source);
					writer.WriteLine("Stack: " + ex.StackTrace);
					if (ex.InnerException != null)
					{
						writer.WriteLine("Inner: " + ex.InnerException.Message);
						writer.WriteLine("Inner Stack: " + ex.InnerException.StackTrace);
					}
					writer.WriteLine("============================================================================");
					writer.WriteLine("");
					writer.Close();
				}
			}
			catch
			{
			}
		}

		public static void WriteToChat(string message)
		{
			try
			{
				Globals.Host.Actions.AddChatText("<{" + Globals.PluginName + "}>: " + message, 3);
			}
			catch (Exception ex) { LogError(ex); }
		}

		public static string GetFriendlyTimeDifference(int seconds)
		{
			var ts = TimeSpan.FromSeconds(seconds);
			return GetFriendlyTimeDifference(ts);
		}

		public static string GetFriendlyTimeDifference(TimeSpan difference)
		{
			string output = "";
			int parts = 0;

			if (difference.Days > 0) parts = 4;
			else if (difference.Hours > 0) parts = 3;
			else if (difference.Minutes > 0) parts = 2;
			else if (difference.Seconds > 0) parts = 1;

			if (parts >= 4) output += $"{difference.Days}d ";
			if (parts >= 3) output += $"{difference.Hours}h ";
			if (parts >= 2) output += $"{difference.Minutes:D2}m ";
			if (parts >= 1) output += $"{difference.Seconds:D2}s ";

			if (output.Length == 0)
				return "Ready";
			return output.Trim();
		}

		public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
		{
			DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			dtDateTime = dtDateTime.AddSeconds(unixTimeStamp);
			return dtDateTime;
		}

        [DllImport("Decal.dll")]
        static extern private int DispatchOnChatCommand(ref IntPtr str, [MarshalAs(UnmanagedType.U4)] int target);

        private static bool Decal_DispatchOnChatCommand(string cmd)
        {
            IntPtr bstr = Marshal.StringToBSTR(cmd);

            try
            {
                bool eaten = (DispatchOnChatCommand(ref bstr, 1) & 0x1) > 0;

                return eaten;
            }
            finally
            {
                Marshal.FreeBSTR(bstr);
            }
        }

        /// <summary>
        /// This will first attempt to send the messages to all plugins. If no plugins set e.Eat to true on the message, it will then simply call InvokeChatParser.
        /// </summary>
        /// <param name="cmd"></param>
        public static void DispatchChatToBoxWithPluginIntercept(string cmd)
        {
            if (!Decal_DispatchOnChatCommand(cmd))
                CoreManager.Current.Actions.InvokeChatParser(cmd);
        }
	}
}
