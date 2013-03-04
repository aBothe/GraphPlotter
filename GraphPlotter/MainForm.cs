using GraphPlotter.Plotting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xwt;

namespace GraphPlotter
{
	class MainForm : Window
	{
		#region Properties
		readonly PlotCanvas plot = new PlotCanvas();

		#endregion

		#region Init/Constructor
		public MainForm()
			: base("GraphPlotter")
		{
			BuildMenu();
			BuildGui();

			Show();
			plot.SetFocus();
		}

		void BuildMenu()
		{
			var m = new Menu();
			MainMenu = m;

			var s = new MenuItem("Program");
			m.Items.Add(s);

			var ss = new Menu();
			s.SubMenu = ss;

			var b = new MenuItem("Export as png");
			b.Clicked += (sender, ea) => 
				plot.RenderIntoPng("dump.png"); //TODO
			ss.Items.Add(b);

			ss.Items.Add(new SeparatorMenuItem());

			b = new MenuItem("Exit");
			b.Clicked += (sender, ea) => Application.Exit();
			ss.Items.Add(b);



			s = new MenuItem("Tools");
			m.Items.Add(s);

			ss = new Menu();
			s.SubMenu = ss;

			b = new MenuItem("Clear functions");
			b.Clicked += (sender, ea) => 
				plot.Graphs = null;
			ss.Items.Add(b);

			b = new MenuItem("Center origin");
			b.Clicked += (sender, ea) => 
				plot.CenterBaseLocation();
			ss.Items.Add(b);
		}

		void BuildGui()
		{
			Height = 600;
			Width = 900;
			Content = plot;
		}

		#endregion
	}
}
