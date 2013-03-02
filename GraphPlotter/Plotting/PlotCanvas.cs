using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xwt;
using Xwt.Drawing;

namespace GraphPlotter.Plotting
{
	class PlotCanvas : Canvas
	{
		#region Properties
		public readonly List<Function> Graphs = new List<Function>();

		/// <summary>
		/// The mathematical point to take for the upper left corner.
		/// </summary>
		public Point BaseLocation;
		public double Scale_X = 1;
		public double Scale_Y = 1;

		public static double DotsPerCentimeter = 96 / 2.54; // ISSUE: Assume 96 as actual (dots per inch) number - It's windows standard

		double DeltaX { get { return Scale_X * CalculationDensity * DotsPerCentimeter; } }
		double YMultiplier { get { return Scale_Y * DotsPerCentimeter; } }

		public int CalculationDensity = 1;
		public double TickDensity_XAxis = 0.2;
		public double TickDensity_YAxis = 0.2;

		public Color axisColor = Colors.Black;
		public Color gridColor = Colors.LightGray;
		public double gridThickness = 0.3;
		public double axisThickness = 1;
		#endregion

		public PlotCanvas()
		{
			Graphs.Add(Function.Parse("cos(x)", "f"));
			Graphs.Add(Function.Parse("sin(x)", "g"));

			BaseLocation = new Point(0, 1);
		}

		#region Drawing
		protected override void OnDraw(Context ctxt, Rectangle dirtyRect)
		{
			ctxt.Save();
			
			DrawXAxis(ctxt, dirtyRect);
			DrawYAxis(ctxt, dirtyRect);

			DrawGraphs(ctxt, dirtyRect);
			
			ctxt.Restore();
			
			base.OnDraw(ctxt, dirtyRect);
		}

		void DrawXAxis(Context ctxt, Rectangle dirtyRect)
		{
			/*var y_multiplier = dirtyRect.Height / AxisBoundaries.Height;
			var y_max = AxisBoundaries.Y + AxisBoundaries.Height;
			var axis_y = -AxisBoundaries.Y * y_multiplier;
			ctxt.Translate(0, axis_y + (axis_y % TickDensity_XAxis));

			ctxt.SetColor(gridColor);
			ctxt.SetLineWidth(gridThickness);

			for (var y = AxisBoundaries.Y; y <= y_max; y += TickDensity_XAxis)
			{
				// We've reached the 0 axis
				if (y >= 0 && y< TickDensity_XAxis)
				{
					ctxt.SetColor(axisColor);
					ctxt.SetLineWidth(axisThickness);

					ctxt.MoveTo(0, 0);
					ctxt.LineTo(dirtyRect.Width, 0);
					ctxt.Stroke();

					ctxt.SetColor(gridColor);
					ctxt.SetLineWidth(gridThickness);
				}
				else
				{
					ctxt.MoveTo(0, y * y_multiplier);
					ctxt.LineTo(dirtyRect.Width, y * y_multiplier);
					ctxt.Stroke();
				}
			}

			ctxt.Translate(0, -axis_y - (axis_y % TickDensity_XAxis));*/
		}

		void DrawYAxis(Context ctxt, Rectangle dirtyRect)
		{
			/*var axis_x = (-AxisBoundaries.X * dirtyRect.Width) / AxisBoundaries.Width;

			if (axis_x < 0 || axis_x > dirtyRect.Width)
				return;

			ctxt.SetColor(Colors.Black);
			ctxt.SetLineWidth(0.5);

			ctxt.MoveTo(axis_x, 0);
			ctxt.LineTo(axis_x, dirtyRect.Height);
			ctxt.Stroke();*/
		}

		double GetMinimalX()
		{
			return BaseLocation.X;
		}

		double GetMinimalY(double windowHeight)
		{
			return BaseLocation.Y - (windowHeight / Scale_Y);
		}

		double GetMaximalX(double windowWidth)
		{
			return BaseLocation.X + (windowWidth / (Scale_X * CalculationDensity));
		}

		double GetMaximalY()
		{
			return BaseLocation.Y;
		}

		void DrawGraphs(Context ctxt, Rectangle dirtyRect)
		{
			var x_max = GetMaximalX(dirtyRect.Width);
			var x_delta = DeltaX;

			var y_multiplier = -YMultiplier;
			var y = 0d;
			var y_min = GetMinimalY(dirtyRect.Height);
			var y_max = GetMaximalY();

			foreach (var f in Graphs)
			{
				y = 0d;
				var x = GetMinimalX();
				ctxt.SetColor(f.GraphColor);
				ctxt.MoveTo(0, 0);
				for (var px = 0 ; px <= dirtyRect.Width; px += CalculationDensity)
				{
					try
					{
						y = f.Calculate(x);
						x += x_delta;

						if (y < y_min)
						{
							y = y_min;
						}
						else if (y > y_max)
							y = y_max;
						else
						{
							ctxt.LineTo(px, (y- y_max) * y_multiplier);
							continue;
						}
					}
					catch (DivideByZeroException ex)
					{
						x += x_delta;
					}

					ctxt.MoveTo(px, (y - y_max) * y_multiplier);
				}

				ctxt.Stroke();
			}
		}
		#endregion

		#region Scaling

		#endregion
	}
}
