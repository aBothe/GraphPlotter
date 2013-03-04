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
		Plotting.PlotCanvas plot;

		#endregion

		#region Init/Constructor
		public MainForm()
			: base("GraphPlotter")
		{
			BuildMenu();
			BuildGui();
		}

		void BuildMenu()
		{
			var m = new Menu();



			MainMenu = m;
		}

		void BuildGui()
		{
			Height = 600;
			Width = 900;

			var vb = new VBox();
			this.Content = vb;

			plot = new Plotting.PlotCanvas();
			vb.PackStart(plot, BoxMode.FillAndExpand);


			var lowerRow = new HBox();
			vb.PackEnd(lowerRow, BoxMode.FillAndExpand, 2);


			var settingsBox = new VBox();
			lowerRow.PackStart(settingsBox, BoxMode.FillAndExpand);

			settingsBox.PackEnd(new Label("Boundaries"));

			var exportButton = new Button("Export to png");
			settingsBox.PackEnd(exportButton);
			exportButton.Clicked += exportButton_Clicked;
		}

		void exportButton_Clicked(object sender, EventArgs e)
		{
			plot.RenderIntoPng("dump.png");
		}
		#endregion
	}
}
