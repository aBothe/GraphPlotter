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
		public Rectangle AxisBoundaries = new Rectangle(-Math.PI, -1, Math.PI*2, 2);

		public double CalculationDensity = 4;
		public double TickDensity_XAxis = 0.2;
		public double TickDensity_YAxis = 0.2;

		public Color axisColor = Colors.Black;
		public Color gridColor = Colors.LightGray;
		public double gridThickness = 0.3;
		public double axisThickness = 1;
		#endregion

		public PlotCanvas()
		{
			Graphs.Add(Function.Parse("cos(x*pi*2)", "f"));
			Graphs.Add(Function.Parse("x^^2", "g"));
		}

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
			var y_multiplier = dirtyRect.Height / AxisBoundaries.Height;
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

			ctxt.Translate(0, -axis_y - (axis_y % TickDensity_XAxis));
		}

		void DrawYAxis(Context ctxt, Rectangle dirtyRect)
		{
			var axis_x = (-AxisBoundaries.X * dirtyRect.Width) / AxisBoundaries.Width;

			if (axis_x < 0 || axis_x > dirtyRect.Width)
				return;

			ctxt.SetColor(Colors.Black);
			ctxt.SetLineWidth(0.5);

			ctxt.MoveTo(axis_x, 0);
			ctxt.LineTo(axis_x, dirtyRect.Height);
			ctxt.Stroke();
		}

		void DrawGraphs(Context ctxt, Rectangle dirtyRect)
		{
			var x_min = AxisBoundaries.X;
			var x_max = AxisBoundaries.X + AxisBoundaries.Width + CalculationDensity;
			var x_delta = (AxisBoundaries.Width * CalculationDensity) / dirtyRect.Width;

			var y_multiplier = -(dirtyRect.Height / AxisBoundaries.Height);
			var y = 0d;
			var y_min = AxisBoundaries.Y;
			var y_max = AxisBoundaries.Y + AxisBoundaries.Height;

			ctxt.Translate(0, dirtyRect.Height / 2);
			foreach (var f in Graphs)
			{
				y = 0d;
				ctxt.SetColor(f.GraphColor);
				ctxt.MoveTo(0, 0);
				var px = -CalculationDensity;
				for (var x = x_min; x <= x_max; x += x_delta)
				{
					px += CalculationDensity;

					try
					{
						y = f.Calculate(x);

						if (y < y_min)
						{
							y = y_min;
						}
						else if (y > y_max)
							y = y_max;
						else
						{
							ctxt.LineTo(px, y * y_multiplier);
							continue;
						}
					}
					catch (DivideByZeroException ex)
					{
					}

					ctxt.MoveTo(px, y * y_multiplier);
				}

				ctxt.Stroke();
			}

			ctxt.Translate(0, -dirtyRect.Height / 2);
		}
	}
}
