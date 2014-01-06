using System;
using Xwt;

namespace GraphPlotter
{
	class Program
	{
		[STAThread]
		static void Main()
		{
			Application.Initialize(ToolkitType.Gtk);
			var main = new MainForm();
			Application.Run();
			main.Dispose();
		}
	}
}
