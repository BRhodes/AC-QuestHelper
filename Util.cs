using System;
using System.IO;

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

			if (difference.Days > 0) output += $"{difference.Days}d ";
			if (difference.Hours > 0) output += $"{difference.Hours}h ";
			if (difference.Minutes > 0) output += $"{difference.Minutes:D2}m ";
			if (difference.Seconds > 0) output += $"{difference.Seconds:D2}s ";

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
	}
}
