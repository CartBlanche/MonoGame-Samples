using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework.Net;

namespace Gorillas
{
	/// <summary>
	/// A small log of what happened on the network (who joined or left, why a session ended, what
	/// the Net library reported), written to Gorillas-net.log in the temp folder and to the
	/// debugger / console. A packaged game has no console, so without this there is nothing to look
	/// at when a game can't find or join a session. The file starts afresh on every launch.
	/// </summary>
	public static class NetLog
	{
		static readonly object sync = new object ();

		/// <summary>Where the log is written.</summary>
		public static readonly string Path = System.IO.Path.Combine (
			System.IO.Path.GetTempPath (), "Gorillas-net.log");

		static NetLog ()
		{
			try {
				File.WriteAllText (Path, "Gorillas network log, started " + DateTime.Now + Environment.NewLine);
			} catch (Exception) {
				// a log that cannot be written must never stop the game
			}
		}

		public static void Write (string message)
		{
			string line = DateTime.Now.ToString ("HH:mm:ss.fff") + "  " + message;
			Debug.WriteLine ("[NET] " + message);
			Console.WriteLine ("[NET] " + message);   // logcat on Android

			try {
				lock (sync)
					File.AppendAllText (Path, line + Environment.NewLine);
			} catch (Exception) {
				// see above
			}
		}
	}

	/// <summary>
	/// Sends the Net library's own log (discovery, joins, timeouts, who was removed and why) to
	/// <see cref="NetLog"/>. Set it once at start-up: NetworkLogging.Logger = new NetLogger ().
	/// </summary>
	public class NetLogger : INetworkLogger
	{
		// Chatty lines the library repeats every second or so while hosting; they would bury the rest.
		static readonly string[] noise = { "Sent broadcast", "advertisement to localhost", "Sending heartbeat" };

		public void LogInfo (string message)
		{
			foreach (string chatter in noise)
				if (message.Contains (chatter))
					return;
			NetLog.Write (message);
		}

		public void LogWarning (string message)
		{
			NetLog.Write ("WARNING " + message);
		}

		public void LogError (string message, Exception ex = null)
		{
			NetLog.Write ("ERROR " + message + (ex != null ? Environment.NewLine + ex : ""));
		}
	}
}
