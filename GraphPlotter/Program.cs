using System;
using Xwt;

namespace GraphPlotter
{
	class Program
	{
		[STAThread]
		static void Main()
		{
			Application.Initialize();
			var main = new MainForm();
			main.Show();
			Application.Run();
			main.Dispose();
		}
	}
}
