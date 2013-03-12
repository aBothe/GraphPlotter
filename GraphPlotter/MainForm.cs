using GraphPlotter.Plotting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Xwt;

namespace GraphPlotter
{
	class MainForm : Window
	{
		#region Properties
		public const string WindowTitle = "GraphPlotter";

		public const string SessionFileExtension = ".gpsess";
		public readonly FileDialogFilter SessionFileFilter = new FileDialogFilter("GraphPlotter Session (*.gpsess)", "*.gpsess");

		string sessFile;
		public string SessionFile
		{
			get { return sessFile; }
			set
			{
				sessFile = value;
				if (!string.IsNullOrEmpty(sessFile))
					Title = WindowTitle+ " [" + sessFile + "]";
				else
					Title = WindowTitle;
			}
		}
		readonly PlotCanvas plot = new PlotCanvas();


		#endregion

		#region Init/Constructor
		public MainForm()
			: base(WindowTitle)
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

			var b = new MenuItem("Load session...");
			b.Clicked += (sender, ea) => Open();
			ss.Items.Add(b);

			b = new MenuItem("Save session");
			b.Clicked += (sender, ea) => Save();
			ss.Items.Add(b);

			b = new MenuItem("Save session as...");
			b.Clicked += (sender, ea) => SaveAs();
			ss.Items.Add(b);

			ss.Items.Add(new SeparatorMenuItem());

			b = new MenuItem("Export as png");
			b.Clicked += (sender, ea) =>
			{
				var dlg = new SaveFileDialog("Export plot as .png file");
				dlg.Filters.Add(new FileDialogFilter("Picture network graphic", ".png"));
				dlg.InitialFileName = "Plot.png";
				dlg.ActiveFilter = dlg.Filters[dlg.Filters.Count - 1];
				if(dlg.Run(this))
					plot.RenderIntoPng(dlg.FileName);
			};
			ss.Items.Add(b);

			ss.Items.Add(new SeparatorMenuItem());

			b = new MenuItem("Exit");
			b.Clicked += (sender, ea) => Application.Exit();
			ss.Items.Add(b);




			s = new MenuItem("View");
			m.Items.Add(s);

			ss = new Menu();
			s.SubMenu = ss;

			b = new CheckBoxMenuItem("Settings overlay") { Checked = true };
			b.Clicked += (sender, ea) => plot.SettingsOverlayVisible = (sender as CheckBoxMenuItem).Checked;
			ss.Items.Add(b);

			b = new CheckBoxMenuItem("Function editing overlay") { Checked = true };
			b.Clicked += (sender, ea) => plot.FunctionOverlayVisible = (sender as CheckBoxMenuItem).Checked;
			ss.Items.Add(b);

			b = new CheckBoxMenuItem("Grid lines") { Checked = true };
			b.Clicked += (sender, ea) => { plot.Options.gridThickness = (sender as CheckBoxMenuItem).Checked ? 1 : 0; plot.Redraw(); };
			ss.Items.Add(b);


			s = new MenuItem("Tools");
			m.Items.Add(s);

			ss = new Menu();
			s.SubMenu = ss;

			b = new MenuItem("Clear functions");
			b.Clicked += (sender, ea) => 
				plot.Options.Functions.Clear();
			ss.Items.Add(b);

			b = new MenuItem("Center origin");
			b.Clicked += (sender, ea) => 
				plot.CenterBaseLocation();
			ss.Items.Add(b);

			b = new MenuItem("Reset scaling");
			b.Clicked += (sender, ea) => plot.Options.RestoreDefaultScaling();
			ss.Items.Add(b);

			ss.Items.Add(new SeparatorMenuItem());

			b = new MenuItem("Reset settings");
			b.Clicked += (sender, ea) => plot.Options.LoadDefaultSettings();
			ss.Items.Add(b);
		}

		void BuildGui()
		{
			Height = 600;
			Width = 900;
			Content = plot;
		}

		protected override bool OnCloseRequested()
		{
			var cmd = MessageDialog.AskQuestion("You try to leave GraphPlotter","Do you want to save the session?", 0, Command.Yes, Command.No, Command.Cancel);

			if (cmd == Command.Cancel)
				return true;

			if (cmd == Command.Yes)
				Save();

			return base.OnCloseRequested();
		}
		#endregion

		#region Settings
		public bool Open()
		{
			var dlg = new OpenFileDialog("Load session...");
			dlg.Filters.Add(SessionFileFilter);
			dlg.ActiveFilter = dlg.Filters[dlg.Filters.Count - 1];

			if (!string.IsNullOrEmpty(SessionFile))
			{
				dlg.CurrentFolder = Path.GetDirectoryName(SessionFile);
				dlg.InitialFileName = Path.GetFileName(SessionFile);
			}

			if (!dlg.Run(this))
				return false;

			try
			{
				using (var x = XmlReader.Create(dlg.FileName))
					plot.Options.LoadSettingsFromXml(x);
				SessionFile = dlg.FileName;
				return true;
			}
			catch (Exception ex)
			{
				MessageDialog.ShowError("Error during loading file", ex.Message);
			}
			return false;
		}

		public bool Save()
		{
			if (string.IsNullOrEmpty(SessionFile))
				return SaveAs();

			try
			{
				using (var x = XmlWriter.Create(SessionFile))
				{
					x.WriteStartDocument();
					plot.Options.SaveToXml(x);
					x.WriteEndDocument();
				}
				return true;
			}
			catch (Exception ex)
			{
				MessageDialog.ShowError(ex.Message);
			}
			return false;
		}

		public bool SaveAs()
		{
			var dlg = new SaveFileDialog("Save session as...");
			dlg.Filters.Add(SessionFileFilter);
			dlg.ActiveFilter = dlg.Filters[dlg.Filters.Count - 1];

			dlg.CurrentFolder = Path.GetDirectoryName(SessionFile);
			dlg.InitialFileName = Path.GetFileName(SessionFile) ?? "session.gpsess";
			
			if (!dlg.Run(this))
				return false;

			SessionFile = dlg.FileName;
			Save();

			return true;
		}
		#endregion
	}
}
