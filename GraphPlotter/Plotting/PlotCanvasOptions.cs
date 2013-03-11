﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml;
using Xwt;
using Xwt.Drawing;

namespace GraphPlotter.Plotting
{
	class PlotCanvasOptions : INotifyPropertyChanged
	{
		#region Properties
		public readonly PlotCanvas Plot;

		public readonly ObservableCollection<Function> Functions = new ObservableCollection<Function>();
		public const int MaximumFunctionCount = 5;

		#region Private
		double baseX, baseY;
		double scaleX, scaleY, movingDelta, scalingDelta;
		int calculationDensity;
		double tickDensX, tickDensY;
		#endregion
		
		/// <summary>
		/// The mathematical point to take for the upper left corner.
		/// </summary>
		public Point BaseLocation
		{
			get { return new Point(baseX,baseY); }
		}
		public double BaseLocation_X { get { return baseX; } set { baseX = value; propChanged("BaseLocation_X"); } }
		public double BaseLocation_Y { get { return baseY; } set { baseY = value; propChanged("BaseLocation_Y"); } }
		public double Scale_X { get { return scaleX; } set { scaleX = value; propChanged("Scale_X"); } }
		public double Scale_Y { get { return scaleY; } set { scaleY = value; propChanged("Scale_Y"); } }

		
		public const double Scale_Min = 0.4;

		public double MovingDelta { get { return movingDelta; } set { movingDelta = value; propChanged("MovingDelta"); } }
		public double ScalingDelta { get { return scalingDelta; } set { scalingDelta = value; propChanged("ScalingDelta"); } }

		public const double DotsPerCentimeter = 40; // ISSUE: Assume 96 as actual (dots per inch) number - It's windows standard

		public double DeltaX { get { return CalculationDensity / (DotsPerCentimeter * Scale_X); } }
		public double YMultiplier { get { return DotsPerCentimeter * Scale_Y; } }

		public int CalculationDensity { get { return calculationDensity; } set { calculationDensity = value; propChanged("CalculationDensity"); } }
		public double TickDensity_XAxis { get { return tickDensX; } set { tickDensX = value; propChanged("TickDensity_XAxis"); } }
		public double TickDensity_YAxis { get { return tickDensY; } set { tickDensY = value; propChanged("TickDensity_YAxis"); } }

		public readonly TextLayout tickLabelFont;
		public Color axisColor;
		public Color gridColor;
		public double gridThickness;
		public double axisThickness;

		public event PropertyChangedEventHandler PropertyChanged;
		void propChanged(string n)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(n));
		}
		#endregion

		#region Constructors/Init
		public PlotCanvasOptions(PlotCanvas plot)
		{
			tickLabelFont = new TextLayout(plot);
			Plot = plot;

			LoadDefaultSettings();
		}

		#endregion

		public void RestoreDefaultScaling()
		{
			Scale_X = 4;
			Scale_Y = 4;
		}

		public void LoadDefaultSettings()
		{
			BaseLocation_X = 0;
			BaseLocation_Y = 0;

			RestoreDefaultScaling();

			MovingDelta = 1;
			ScalingDelta = 0.1;

			CalculationDensity = 2;

			TickDensity_XAxis = 0.2;
			TickDensity_YAxis = 0.2;

			axisColor = Colors.Black;
			axisThickness = 1;

			gridColor = Color.FromBytes(0xee, 0xee, 0xee);
			gridThickness = 1;

			tickLabelFont.Font = tickLabelFont.Font.WithPointSize(7);
		}

		public void LoadSettingsFromXml(XmlReader x)
		{
			while (x.Read())
			{
				switch (x.LocalName)
				{
					case "BaseLocation":
						if (x.MoveToAttribute("x"))
							BaseLocation_X = x.ReadContentAsDouble();
						if (x.MoveToAttribute("y"))
							BaseLocation_X = x.ReadContentAsDouble();
						if (x.MoveToAttribute("delta"))
							MovingDelta = x.ReadContentAsDouble();
						break;
					case "Scaling":
						if (x.MoveToAttribute("x"))
							Scale_X = Math.Min(x.ReadContentAsDouble(), Scale_Min);
						if (x.MoveToAttribute("y"))
							Scale_Y = Math.Min(x.ReadContentAsDouble(), Scale_Min);
						if (x.MoveToAttribute("delta"))
							ScalingDelta = x.ReadContentAsDouble();
						break;
					case "CalculationDensity":
						if (x.MoveToAttribute("value"))
							CalculationDensity = Math.Min(1, x.ReadContentAsInt());
						break;
					case "AxisTickDensty":
						if (x.MoveToAttribute("x"))
							TickDensity_XAxis = Math.Min(x.ReadContentAsDouble(), 0.001);
						if (x.MoveToAttribute("y"))
							TickDensity_YAxis = Math.Min(x.ReadContentAsDouble(), 0.001);
						break;

					case "Functions":
						Plot.BeginUpdateGraphs();
						Functions.Clear();
						var subTree = x.ReadSubtree();
						while (subTree.Read())
						{
							if (subTree.LocalName == "Function")
							{
								var f = Function.LoadFrom(subTree.ReadSubtree());
								if (f != null)
									Functions.Add(f);
							}
						}
						Plot.FinishUpdateGraphs();
						break;
				}
			}
		}

		public void SaveToXml(XmlWriter x)
		{
			x.WriteStartElement("BaseLocation");
			x.WriteAttributeString("x", baseX.ToString());
			x.WriteAttributeString("y", baseY.ToString());
			x.WriteAttributeString("delta", MovingDelta.ToString());
			x.WriteEndElement();

			x.WriteStartElement("Scaling");
			x.WriteAttributeString("x", Scale_X.ToString());
			x.WriteAttributeString("y", Scale_Y.ToString());
			x.WriteAttributeString("delta", ScalingDelta.ToString());
			x.WriteEndElement();

			x.WriteStartElement("CalculationDensity");
			x.WriteAttributeString("value", CalculationDensity.ToString());
			x.WriteEndElement();

			x.WriteStartElement("AxisTickDensty");
			x.WriteAttributeString("x", TickDensity_XAxis.ToString());
			x.WriteAttributeString("y", TickDensity_YAxis.ToString());
			x.WriteEndElement();

			if (Functions.Count > 0)
			{
				x.WriteStartElement("Functions");
				foreach (var f in Functions)
				{
					x.WriteStartElement("Function");
					f.SaveTo(x);
					x.WriteEndElement();
				}
				x.WriteEndElement();
			}
		}
	}
}