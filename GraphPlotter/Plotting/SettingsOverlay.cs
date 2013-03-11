using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xwt;

namespace GraphPlotter.Plotting
{
	class SettingsOverlay : Widget
	{
		public readonly PlotCanvas Plot;
		TextEntry text_BaseX = new TextEntry(), text_BaseY = new TextEntry();
		TextEntry text_ScaleX = new TextEntry(), text_ScaleY = new TextEntry();
		TextEntry text_TickDensX = new TextEntry(), text_TickDensY = new TextEntry(), text_CalcDensity = new TextEntry();

		public SettingsOverlay(PlotCanvas plot)
		{
			Plot = plot;
			plot.Options.PropertyChanged += Options_PropertyChanged;

			NaturalWidth = 130;

			var font = text_BaseX.Font.WithPointSize(8);
			var labelFont = font.WithPointSize(9).WithWeight(Xwt.Drawing.FontWeight.Semibold);

			
			var mainBox = new VBox();
			Content = mainBox;

			mainBox.PackStart(new Label("Base location (X/Y)") { Font = labelFont });
			text_BaseX.Font = font; text_BaseX.Text = plot.Options.BaseLocation_X.ToString();
			text_BaseY.Font = font; text_BaseY.Text = plot.Options.BaseLocation_Y.ToString();
			var hb = new HBox();
			mainBox.PackStart(hb, BoxMode.Fill);
			hb.PackStart(text_BaseX);
			hb.PackStart(text_BaseY);
			var b = new Button("Set") { Font = labelFont };
			b.Clicked += (s, e) => {
				double d;
				if (Double.TryParse(text_BaseX.Text, out d))
					plot.Options.BaseLocation_X = d;
				if (Double.TryParse(text_BaseY.Text, out d))
					plot.Options.BaseLocation_Y = d;
				plot.Redraw();
			};
			hb.PackStart(b);


			mainBox.PackStart(new Label("Scaling (X/Y)") { Font = labelFont });
			text_ScaleX.Font = font; text_ScaleX.Text = plot.Options.Scale_X.ToString();
			text_ScaleY.Font = font; text_ScaleY.Text = plot.Options.Scale_Y.ToString();
			hb = new HBox();
			mainBox.PackStart(hb, BoxMode.Fill);
			hb.PackStart(text_ScaleX);
			hb.PackStart(text_ScaleY);
			b = new Button("Set") { Font = labelFont };
			b.Clicked += (s, e) =>
			{
				double d;
				if (Double.TryParse(text_ScaleX.Text, out d))
					plot.Options.Scale_X = d;
				if (Double.TryParse(text_ScaleY.Text, out d))
					plot.Options.Scale_Y= d;
				plot.Redraw();
			};
			hb.PackStart(b);


			mainBox.PackStart(new Label("Tick density (X/Y)") { Font = labelFont });
			text_TickDensX.Font = font; text_TickDensX.Text = plot.Options.TickDensity_XAxis.ToString();
			text_TickDensY.Font = font; text_TickDensY.Text = plot.Options.TickDensity_YAxis.ToString();
			hb = new HBox();
			mainBox.PackStart(hb, BoxMode.Fill);
			hb.PackStart(text_TickDensX);
			hb.PackStart(text_TickDensY);
			b = new Button("Set") { Font = labelFont };
			b.Clicked += (s, e) =>
			{
				double d;
				if (Double.TryParse(text_TickDensX.Text, out d))
					plot.Options.TickDensity_XAxis = d;
				if (Double.TryParse(text_TickDensY.Text, out d))
					plot.Options.TickDensity_YAxis = d;
				plot.Redraw();
			};
			hb.PackStart(b);


			mainBox.PackStart(new Label("Calculation density (Pixel)") { Font = labelFont });
			text_CalcDensity.Font = font; text_CalcDensity.Text = plot.Options.CalculationDensity.ToString();
			text_CalcDensity.MarginLeft = 5;
			hb = new HBox();
			mainBox.PackStart(hb, BoxMode.Fill);
			hb.PackStart(text_CalcDensity);
			b = new Button("Set") { Font = labelFont };
			b.Clicked += (s, e) =>
			{
				uint d;
				if (UInt32.TryParse(text_CalcDensity.Text, out d) && d >= 1)
				{
					plot.Options.CalculationDensity = (int)d;
					plot.Redraw();
				}
			};
			hb.PackStart(b);
		}

		void Options_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			var o = sender as PlotCanvasOptions;
			switch (e.PropertyName)
			{
				case "BaseLocation_X":
					text_BaseX.Text = Math.Round(o.BaseLocation_X,3).ToString();
					break;
				case "BaseLocation_Y":
					text_BaseY.Text = Math.Round(o.BaseLocation_Y, 3).ToString();
					break;
				case "Scale_X":
					text_ScaleX.Text = Math.Round(o.Scale_X, 3).ToString();
					break;
				case "Scale_Y":
					text_ScaleY.Text = Math.Round(o.Scale_Y, 3).ToString();
					break;
				case "TickDensity_X":
					text_TickDensX.Text = Math.Round(o.TickDensity_XAxis, 3).ToString();
					break;
				case "TickDensity_Y":
					text_TickDensY.Text = Math.Round(o.TickDensity_YAxis, 3).ToString();
					break;
				case "CalculationDensity":
					text_CalcDensity.Text = o.CalculationDensity.ToString();
					break;
			}
		}
	}
}
