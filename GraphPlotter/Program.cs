using System;
using Xwt;

namespace GraphPlotter
{
	class Program
	{
		[STAThread]
		static void Main()
		{
			Application.Initialize(ToolkitType.Wpf);
			var main = new MainForm();
			Application.Run();
			main.Dispose();
		}
	}
}
